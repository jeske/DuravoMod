// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

// ╔════════════════════════════════════════════════════════════════════════════════════════════╗
// ║                            FUTURE IMPROVEMENTS / TODOs                                      ║
// ╚════════════════════════════════════════════════════════════════════════════════════════════╝
//
// TODO(1): FOLLOW DISTANCE - Consider stopping path-following once we reach the minion's natural
//          follow distance instead of going all the way to the player. Each aiStyle has different
//          "close enough" thresholds:
//          - aiStyle 62 (Hornet, Imp, UFO, Cell): ~70-100 pixels from player
//          - aiStyle 26/67 (Pygmy, Spider, Pirate): ~50-80 pixels (ground formation position)
//          - Flying minions hover ~60 pixels above player
//          See: _DOCS/TERRARIA_REFERENCE/MinionStateExtraction.md for full follow distance table
//          and MinionFollowDistances.GetIdlePosition() for per-aiStyle target positions.
//
// TODO(2): AISTYLE 156 (Sanguine Bat, Terraprisma) - These are ALWAYS-PHASING minions that use
//          arc/orbital patterns around the player. They have tileCollide=false in SetDefaults
//          and should NEVER trigger QoL pathfinding. Currently excluded via AlwaysPhases check
//          in MinionStateExtractor, but TEST to ensure they don't trigger "bouncing to center
//          of player" if they run into an obstacle during their arc movement. The arc pattern
//          means they intentionally move AWAY from player sometimes, which could falsely trigger
//          "blocked" detection if there's an L-path obstacle during arc motion.
//          Verify: minionState.AlwaysPhases should be TRUE for all aiStyle 156 minions.
//
// TODO(3): There is a better way to do our check for a minion getting stuck while returning. 
//          (a) if minion is returning and our fast L-path fails..
//          (b) track best-progress towards player on BOTH X/Y (we currently on track dominant axis)
//          (c) if we don't see progress on EITHER X/Y for 0.5s, then we know we're stuck.
//          (d) trigger the A* pathfinding for a better route... if we find no route, phase
//
// ═════════════════════════════════════════════════════════════════════════════════════════════

namespace DuravoQOLMod.TetheredMinions
{
    /// <summary>
    /// GlobalProjectile that handles minion anti-cheese and QoL pathing.
    ///
    /// Two main features:
    /// 1. CHEESE PREVENTION: When player places a block, if minion is nearby with no LOS
    ///    and no valid A* path, immediately phase the minion back to player.
    /// 2. QOL PATHING: When minion's velocity is pointing toward player but blocked,
    ///    compute A* path and follow it (or phase if no path exists).
    /// </summary>
    public class TetheredMinionProjectile : GlobalProjectile
    {
        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                        CONFIG ACCESSORS                            ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Get debug pathfinding setting from mod config</summary>
        private static bool DebugTethering => ModContent.GetInstance<DuravoQOLModConfig>()?.Debug?.DebugMinionPathfinding ?? false;

        /// <summary>Get smart pathfinding enabled setting from mod config</summary>
        private static bool EnableQoLFollowPathing => ModContent.GetInstance<DuravoQOLModConfig>()?.MinionSmartPathfinding ?? true;

        /// <summary>Get isolated return (anti-cheese) enabled setting from mod config</summary>
        private static bool EnableIsolatedReturn => ModContent.GetInstance<DuravoQOLModConfig>()?.MinionIsolatedReturn ?? true;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                        TUNABLE CONSTANTS                           ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Max distance in tiles for cheese check / QoL pathing to apply</summary>
        private const float MaxDistanceForCheckTiles = 35f;

        /// <summary>Max A* path length in tiles (prevents expensive long searches)</summary>
        private const int MaxAStarPathLengthTiles = 35;

        /// <summary>Cooldown in ticks before rechecking A* after cheese prevention triggers</summary>
        private const int CheeseCheckCooldownTicks = 120; // 2 seconds

        /// <summary>Cooldown in ticks before rechecking QoL pathing</summary>
        private const int QoLPathingCheckCooldownTicks = 30; // 0.5 seconds

        /// <summary>Maximum speed at which minion flies toward player when phasing</summary>
        private const float PhasingMaxFlySpeed = 15f;

        /// <summary>Distance at which minion starts slowing down (pixels)</summary>
        private const float PhasingSlowdownDistance = 150f;

        /// <summary>Distance at which minion is considered "arrived" (pixels)</summary>
        private const float PhasingArrivalDistance = 40f;

        /// <summary>Distance at which minion is considered to have reached a path waypoint (pixels)</summary>
        private const float WaypointArrivalDistance = 24f;

        /// <summary>Speed when following A* path (pixels per tick)</summary>
        private const float PathFollowSpeed = 4f;

        /// <summary>Minimum distance from player (pixels) before QoL pathing kicks in</summary>
        private const float MinDistanceForQoLPathing = 80f;

        /// <summary>How long "phasing" state lasts before we force teleport (ticks, 60 = 1 second)</summary>
        private const int PhasingTimeoutTicks = 180; // 3 seconds

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          STATIC STATE                              ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// The game tick on which cheese check should execute.
        /// Set to currentTick + 1 when block is placed, checked in PreAI.
        /// </summary>
        public static int DoCheeseCheckOnTick { get; set; } = int.MaxValue;

        /// <summary>
        /// The player who placed the block (for ownership check).
        /// </summary>
        public static int BlockPlacerPlayerIndex { get; set; } = -1;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          INSTANCE STATE                            ║
        // ╚════════════════════════════════════════════════════════════════════╝

        // Per-projectile instance required for tracking state
        public override bool InstancePerEntity => true;

        /// <summary>Game tick when cheese check was last performed</summary>
        private int lastCheeseCheckTick;

        /// <summary>Whether minion is currently in "phasing" state (flying through walls to owner)</summary>
        private bool isPhasingToOwner;

        /// <summary>Game tick when phasing started (for timeout)</summary>
        private uint phasingStartedTick;

        /// <summary>Original tileCollide value before phasing</summary>
        private bool originalTileCollide;

        /// <summary>Whether minion is currently following A* path waypoints</summary>
        private bool isFollowingPath;

        /// <summary>Original tileCollide value before path-following (to restore on exit)</summary>
        private bool pathFollowingOriginalTileCollide;

        /// <summary>The A* path waypoints from minion to owner (world pixel coords)</summary>
        private List<Vector2>? currentPathWaypoints;

        /// <summary>Current waypoint index we're heading toward</summary>
        private int currentWaypointIndex;

        /// <summary>Game tick when path-following ended (for cooldown)</summary>
        private uint pathFollowingEndedTick;

        /// <summary>Last distance to player when we started tracking "stuck" state</summary>
        private float stuckCheckStartDistance;

        /// <summary>Game tick when we started the stuck check</summary>
        private uint stuckCheckStartTick;

        /// <summary>How long minion must fail to make progress before triggering pathfinding (ticks, 60 = 1 second)</summary>
        private const int StuckDetectionTimeTicks = 60; // 1 second

        /// <summary>Minimum progress toward player (pixels) to reset stuck timer</summary>
        private const float StuckProgressThresholdPixels = 20f;

        /// <summary>True if horizontal axis was dominant when pathfinding started</summary>
        private bool pathStartDominantAxisWasHorizontal;

        /// <summary>The BLOCKING TILE position on dominant axis (tile coords) - exit when minion passes this + 1</summary>
        private int blockingTilePositionOnDominantAxis;

        /// <summary>Direction toward player on dominant axis when pathfinding started (+1 or -1)</summary>
        private int pathStartDirectionToPlayer;

        /// <summary>Cooldown after exiting path-following before re-entry allowed (ticks, 60 = 1 second)</summary>
        private const int PathFollowingCooldownTicks = 90; // 1.5 seconds

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          MAIN LOGIC                                ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// PostAI runs AFTER the projectile's normal AI.
        /// Velocity here is the AI's INTENDED velocity (before collision modifies it).
        /// This is where we check for stuck detection and apply phasing/path-following.
        /// </summary>
        public override void PostAI(Projectile projectile)
        {
            // Only process minions
            if (!projectile.minion)
                return;

            // Get owner
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            Player ownerPlayer = Main.player[projectile.owner];
            if (!ownerPlayer.active || ownerPlayer.dead)
                return;

            // Get distance to owner in tiles
            float distanceToOwnerPixels = Vector2.Distance(projectile.Center, ownerPlayer.Center);
            float distanceToOwnerTiles = distanceToOwnerPixels / 16f;

            // Only process if minion is within our check range
            if (distanceToOwnerTiles > MaxDistanceForCheckTiles)
                return;

            int currentTick = (int)Main.GameUpdateCount;

            // ═══════════════════════════════════════════════════════════
            // CHEESE PREVENTION: Check on block placement (delayed by 1 tick)
            // PlaceInWorld fires BEFORE the tile exists, so we wait until DoCheeseCheckOnTick
            // New block placement ALWAYS triggers check (no cooldown - player action is deliberate)
            // ═══════════════════════════════════════════════════════════
            if (EnableIsolatedReturn && currentTick >= DoCheeseCheckOnTick && BlockPlacerPlayerIndex == projectile.owner) {
                // Reset immediately so we don't re-check every frame
                DoCheeseCheckOnTick = int.MaxValue;

                if (DebugTethering) {
                    int clearanceW = TilePathfinder.CalculateTileClearance(projectile.width);
                    int clearanceH = TilePathfinder.CalculateTileClearance(projectile.height);
                    Main.NewText($"[TETHER] Block placed, checking {projectile.Name} (hitbox {projectile.width}x{projectile.height}px → {clearanceW}x{clearanceH} tiles)", Color.Yellow);
                }
                CheckCheesePrevention(projectile, ownerPlayer);
            }

            // ═══════════════════════════════════════════════════════════
            // QOL PATHING: Check if minion is blocked by solid tile while following player
            // Uses MinionStateExtractor to detect "Following" state, then checks if
            // the next tile toward player is solid. If blocked, compute A* path.
            // COOLDOWN: Don't re-enter pathfinding for 1.5 seconds after exiting (prevent thrashing)
            // ═══════════════════════════════════════════════════════════
            uint currentTickForCooldown = Main.GameUpdateCount;
            bool pathCooldownExpired = (currentTickForCooldown - pathFollowingEndedTick) > PathFollowingCooldownTicks;
            if (EnableQoLFollowPathing && !isPhasingToOwner && !isFollowingPath && pathCooldownExpired) {
                CheckBlockedAndPathfind(projectile, ownerPlayer);
            }

            // ═══════════════════════════════════════════════════════════
            // APPLY MOVEMENT: Override velocity for phasing/path-following
            // ═══════════════════════════════════════════════════════════
            if (isFollowingPath) {
                ApplyPathFollowingMovement(projectile, ownerPlayer);
            }
            else if (isPhasingToOwner) {
                ApplyPhasingMovement(projectile, ownerPlayer);
            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      CHEESE PREVENTION                             ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Cheese prevention check: Player placed a block, minion is nearby.
        /// If no simple path and no A* path, immediately phase minion to player.
        /// </summary>
        private void CheckCheesePrevention(Projectile minion, Player ownerPlayer)
        {
            // Check if minion has a simple L-path to player (clearance-aware)
            bool hasSimplePath = MinionHasSimplePathToLocation(minion, ownerPlayer.position);
            if (hasSimplePath) {
                // Simple path exists, no cheese prevention needed
                return;
            }

            // No LOS - try A* path (max 18 tiles)
            // Use top-left position (not Center) because clearance checking assumes tile = upper-left of hitbox
            List<Vector2>? pathToOwner = TilePathfinder.FindPath(
                minion.position,
                ownerPlayer.position,
                minion.width,
                minion.height,
                MaxAStarPathLengthTiles
            );

            if (pathToOwner != null && pathToOwner.Count > 0) {
                // Valid path exists - no cheese, minion can get back naturally
                if (DebugTethering) {
                    DebugPrintPathSteps(pathToOwner, $"Cheese check: {minion.Name}");
                }
                return;
            }

            // NO LOS and NO PATH = CHEESE DETECTED! Phase the minion back.
            if (DebugTethering) {
                Main.NewText($"[TETHER] CHEESE DETECTED! {minion.Name} has no path - phasing to player", Color.Red);
            }
            StartPhasingState(minion, "cheese prevention - no path after block placed");
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                     BLOCKED DETECTION (QOL PATHING)                ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// QoL blocked detection: If minion is in "Following" or "Idle" state and blocked from player,
        /// compute A* path and follow it.
        /// Note: "Idle" minions that can't reach player also need help (wall between them).
        /// </summary>
        private void CheckBlockedAndPathfind(Projectile minion, Player ownerPlayer)
        {
            // Step 1: Get minion state - must be "Following" or "Idle" (not Attacking, Returning, etc.)
            MinionStateInfo minionState = MinionStateExtractor.GetMinionState(minion);

            // Skip attacking minions - they're busy
            if (minionState.State == MinionState.Attacking)
                return;

            // Skip returning/dashing/spawning - these are already handling movement
            if (minionState.State == MinionState.Returning ||
                minionState.State == MinionState.Dashing ||
                minionState.State == MinionState.Spawning)
                return;

            // Only process Following and Idle states
            if (minionState.State != MinionState.Following && minionState.State != MinionState.Idle)
                return;

            // Skip always-phasing minions (they don't need pathfinding help)
            if (minionState.AlwaysPhases || minionState.CurrentlyPhasing)
                return;

            // Step 2: Compute distance and L-path check FIRST (used for both Idle and Following filtering)
            float distanceToOwnerPixels = Vector2.Distance(minion.Center, ownerPlayer.Center);
            Vector2 vectorToPlayer = ownerPlayer.Center - minion.Center;

            // Get the UPPER-LEFT tile of the minion (for proper edge detection)
            // Add 2 pixels to avoid tile boundary issues (if position is 352.0, we want tile 22 not 21)
            Vector2 adjustedPosition = minion.position + new Vector2(2f, 2f);
            Point minionTopLeftTile = adjustedPosition.ToTileCoordinates();
            int clearanceWidth = TilePathfinder.CalculateTileClearance(minion.width);
            int clearanceHeight = TilePathfinder.CalculateTileClearance(minion.height);

            // Check L-path to player (dominant axis first, then secondary)
            Point playerTopLeftTile = (ownerPlayer.position + new Vector2(2f, 2f)).ToTileCoordinates();
            bool lPathBlocked = IsLPathBlocked(minionTopLeftTile, playerTopLeftTile, clearanceWidth, clearanceHeight, out Point blockingTile);

            // Track which axis is dominant for exit condition later
            bool horizontalDominant = Math.Abs(vectorToPlayer.X) >= Math.Abs(vectorToPlayer.Y);

            // Debug: Check if the minion's own tile is blocked (would indicate bad tile conversion)
            if (DebugTethering && Main.GameUpdateCount % 120 == 0) {
                bool ownTileBlocked = IsSolidBlockingTile(minionTopLeftTile.X, minionTopLeftTile.Y);
                Main.NewText($"[DEBUG] {minion.Name} pos=({minion.position.X:F1},{minion.position.Y:F1})px → tile({minionTopLeftTile.X},{minionTopLeftTile.Y}), ownTileBlocked={ownTileBlocked}, clearance={clearanceWidth}x{clearanceHeight}, lPathBlocked={lPathBlocked}", Color.Cyan);
            }

            // Step 3: Filter based on state
            if (minionState.State == MinionState.Idle) {
                // For "Idle" minions: only help if L-path is blocked
                // (They might be marked idle due to short straight-line distance, but L-path blocked by wall)
                if (!lPathBlocked) {
                    return; // L-path is clear, genuinely idle - don't interfere
                }
                // L-path blocked = needs help, continue to pathfinding
                if (DebugTethering && Main.GameUpdateCount % 60 == 0) {
                    Main.NewText($"[TETHER] {minion.Name} is 'Idle' but L-path blocked - checking A*", Color.Yellow);
                }
            }
            else {
                // Following state - must be far enough from player to bother
                if (distanceToOwnerPixels < MinDistanceForQoLPathing)
                    return;
            }

            // Step 4: Use L-path result for blocked detection
            bool nextTileIsSolid = lPathBlocked;
            int checkTileX = blockingTile.X;
            int checkTileY = blockingTile.Y;

            // Step 4b: Also check "stuck" detection - minion hasn't made progress toward player
            uint currentTick = Main.GameUpdateCount;
            bool isStuck = false;

            if (stuckCheckStartTick == 0) {
                // Start tracking
                stuckCheckStartTick = currentTick;
                stuckCheckStartDistance = distanceToOwnerPixels;
            }
            else {
                // Check if we've made progress
                float distanceProgress = stuckCheckStartDistance - distanceToOwnerPixels;
                if (distanceProgress >= StuckProgressThresholdPixels) {
                    // Made progress! Reset timer
                    stuckCheckStartTick = currentTick;
                    stuckCheckStartDistance = distanceToOwnerPixels;
                }
                else if ((currentTick - stuckCheckStartTick) >= StuckDetectionTimeTicks) {
                    // Been stuck for too long without making progress
                    isStuck = true;
                    // Reset for next check
                    stuckCheckStartTick = 0;
                    stuckCheckStartDistance = 0f;
                }

                // Debug: Print stuck check state every 1000 frames
                if (DebugTethering && Main.GameUpdateCount % 1000 == 0) {
                    int elapsedTicks = (int)(currentTick - stuckCheckStartTick);
                    Main.NewText($"[STUCK] {minion.Name}: elapsed={elapsedTicks}/{StuckDetectionTimeTicks} ticks, startDist={stuckCheckStartDistance:F0}px, currDist={distanceToOwnerPixels:F0}px, progress={distanceProgress:F1}px (need {StuckProgressThresholdPixels}px), nextTileSolid={nextTileIsSolid}", Color.Magenta);
                }
            }

            if (!nextTileIsSolid && !isStuck)
                return; // Not blocked and not stuck, let normal AI handle it

            // Step 5: BLOCKED OR STUCK! Minion is following player but can't make progress.
            // Compute A* path around the obstacle.
            if (DebugTethering) {
                string direction = horizontalDominant
                    ? (vectorToPlayer.X > 0 ? "right" : "left")
                    : (vectorToPlayer.Y > 0 ? "down" : "up");
                string reason = isStuck ? "STUCK (no progress)" : $"BLOCKED {direction}";
                Main.NewText($"[TETHER] {minion.Name} {reason} at tile ({checkTileX},{checkTileY})", Color.Yellow);
            }

            // Use top-left position (not Center) because clearance checking assumes tile = upper-left of hitbox
            List<Vector2>? pathToOwner = TilePathfinder.FindPath(
                minion.position,
                ownerPlayer.position,
                minion.width,
                minion.height,
                MaxAStarPathLengthTiles
            );

            if (pathToOwner != null && pathToOwner.Count > 0) {
                // Found a path - follow it!
                // Pass the blocking tile coordinate on the dominant axis for exit threshold computation
                int blockingTileCoordOnAxis = horizontalDominant ? checkTileX : checkTileY;
                int pathLengthTiles = pathToOwner.Count - 1;
                StartPathFollowingState(minion, pathToOwner, horizontalDominant, vectorToPlayer, blockingTileCoordOnAxis, $"blocked detection - {pathLengthTiles} tile path found");
            }
            else {
                // No path found - phase toward player as fallback
                if (DebugTethering) {
                    Main.NewText($"[TETHER] {minion.Name} no A* path, phasing to player", Color.Orange);
                }
                StartPhasingState(minion, "blocked detection - no path available");
            }
        }

        /// <summary>
        /// Check if a tile is solid and blocks movement.
        /// Excludes platforms (tileSolidTop).
        /// </summary>
        private static bool IsSolidBlockingTile(int tileX, int tileY)
        {
            // Bounds check
            if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
                return true; // Out of bounds = solid

            Tile tile = Main.tile[tileX, tileY];
            return tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      SIMPLE PATH CHECK                              ║
        // ╚════════════════════════════════════════════════════════════════════╝

        // ────────────────────────────────────────────────────────────────────────────────────────
        // WHY "SIMPLE PATH" INSTEAD OF STRAIGHT-LINE LOS?
        //
        // Straight-line LOS (like Collision.CanHitLine) is almost NEVER useful for minion pathing
        // decisions. It fails constantly because:
        //   - Uses 1x1 pixel hitbox - ignores minion's actual NxM tile clearance
        //   - Can slip through "touching corners" (diagonally adjacent solid tiles)
        //   - Any diagonal clips corners that a real entity couldn't traverse
        //   - Moving minions rarely travel in perfect straight lines
        //   - Even a single protruding tile blocks the check
        //
        // The "simple path" check (L-path: dominant axis first, then secondary) represents a
        // REALISTIC path that a minion might actually take - walking horizontally then
        // dropping/climbing, or moving vertically then sliding over. It uses the minion's
        // actual tile clearance (NxM) to ensure the path is actually traversable.
        //
        // PHILOSOPHY: We should never do aggressive interventions (like phasing) based on
        // straight-line LOS alone. The simple path check is a "softer" test that better
        // reflects actual navigation feasibility before we decide the minion needs A* help.
        // ────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Check if minion has a simple L-path to the target location.
        /// Uses the minion's actual tile clearance (NxM) for accurate collision detection.
        /// Returns TRUE if path is CLEAR (minion can likely reach target without A* pathfinding).
        /// </summary>
        /// <param name="minion">The minion projectile (used for position and size)</param>
        /// <param name="targetLocation">Target world position (e.g., player.position)</param>
        /// <returns>True if simple path exists, false if blocked</returns>
        private static bool MinionHasSimplePathToLocation(Projectile minion, Vector2 targetLocation)
        {
            // Convert positions to tile coordinates (add 2px to avoid boundary issues)
            Point minionTile = (minion.position + new Vector2(2f, 2f)).ToTileCoordinates();
            Point targetTile = (targetLocation + new Vector2(2f, 2f)).ToTileCoordinates();

            // Calculate minion's tile clearance
            int clearanceWidth = TilePathfinder.CalculateTileClearance(minion.width);
            int clearanceHeight = TilePathfinder.CalculateTileClearance(minion.height);

            // Check if L-path is blocked - invert result for "has path" semantics
            bool isBlocked = IsLPathBlocked(minionTile, targetTile, clearanceWidth, clearanceHeight, out _);
            return !isBlocked;
        }

        /// <summary>
        /// Internal: Check if there's a clear "L-path" from start to goal.
        /// Uses Bresenham-style walking: move on the dominant axis first, then switch to the other.
        /// Returns true if ANY tile on the L-path is blocked.
        /// </summary>
        /// <param name="startTile">Start tile (minion's upper-left)</param>
        /// <param name="goalTile">Goal tile (player's upper-left)</param>
        /// <param name="clearanceWidth">Entity clearance width in tiles</param>
        /// <param name="clearanceHeight">Entity clearance height in tiles</param>
        /// <param name="blockingTile">Output: the first tile that blocked (for debug/exit condition)</param>
        /// <returns>True if path is blocked, false if clear</returns>
        private static bool IsLPathBlocked(Point startTile, Point goalTile, int clearanceWidth, int clearanceHeight, out Point blockingTile)
        {
            int dx = goalTile.X - startTile.X;
            int dy = goalTile.Y - startTile.Y;

            int stepX = dx > 0 ? 1 : -1;
            int stepY = dy > 0 ? 1 : -1;

            int currentX = startTile.X;
            int currentY = startTile.Y;

            // Determine which axis is dominant (larger delta)
            bool xDominant = Math.Abs(dx) >= Math.Abs(dy);

            if (xDominant) {
                // Walk X axis first (dominant), then Y axis
                // Check tile BEFORE moving to it (we check the NEXT tile, not current)
                while (currentX != goalTile.X) {
                    int nextX = currentX + stepX;
                    // Check if we can move to nextX (check tile ahead with clearance)
                    int checkTileX = stepX > 0 ? nextX + clearanceWidth - 1 : nextX;
                    if (IsTileBlockedWithClearance(checkTileX, currentY, clearanceWidth, clearanceHeight)) {
                        blockingTile = new Point(checkTileX, currentY);
                        return true;
                    }
                    currentX = nextX;
                }
                // Now walk Y axis
                while (currentY != goalTile.Y) {
                    int nextY = currentY + stepY;
                    int checkTileY = stepY > 0 ? nextY + clearanceHeight - 1 : nextY;
                    if (IsTileBlockedWithClearance(currentX, checkTileY, clearanceWidth, clearanceHeight)) {
                        blockingTile = new Point(currentX, checkTileY);
                        return true;
                    }
                    currentY = nextY;
                }
            }
            else {
                // Walk Y axis first (dominant), then X axis
                while (currentY != goalTile.Y) {
                    int nextY = currentY + stepY;
                    int checkTileY = stepY > 0 ? nextY + clearanceHeight - 1 : nextY;
                    if (IsTileBlockedWithClearance(currentX, checkTileY, clearanceWidth, clearanceHeight)) {
                        blockingTile = new Point(currentX, checkTileY);
                        return true;
                    }
                    currentY = nextY;
                }
                // Now walk X axis
                while (currentX != goalTile.X) {
                    int nextX = currentX + stepX;
                    int checkTileX = stepX > 0 ? nextX + clearanceWidth - 1 : nextX;
                    if (IsTileBlockedWithClearance(checkTileX, currentY, clearanceWidth, clearanceHeight)) {
                        blockingTile = new Point(checkTileX, currentY);
                        return true;
                    }
                    currentX = nextX;
                }
            }

            // Path is clear!
            blockingTile = goalTile;
            return false;
        }

        /// <summary>
        /// Check if a tile (considering entity clearance) is blocked.
        /// For X movement: checks a vertical strip of tiles.
        /// For Y movement: checks a horizontal strip of tiles.
        /// </summary>
        private static bool IsTileBlockedWithClearance(int tileX, int tileY, int clearanceWidth, int clearanceHeight)
        {
            // Check all tiles in the clearance area
            for (int offsetY = 0; offsetY < clearanceHeight; offsetY++) {
                for (int offsetX = 0; offsetX < clearanceWidth; offsetX++) {
                    if (IsSolidBlockingTile(tileX + offsetX, tileY + offsetY)) {
                        return true;
                    }
                }
            }
            return false;
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      PATH FOLLOWING                                ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Apply path-following movement - walk/fly through A* waypoints.
        /// Exit condition: minion has LOS to player OR has moved PAST the stuck point on dominant axis.
        /// </summary>
        private void ApplyPathFollowingMovement(Projectile minion, Player ownerPlayer)
        {
            // Exit condition: Minion's CENTER has moved PAST the exit threshold in PIXEL space
            // The threshold is computed from the blocking tile: (blockingTile + 1) * 16 + minionWidth/2
            // This ensures minion's center has definitively cleared the obstacle.

            // Get minion's current position on the dominant axis
            float minionCenterOnDominantAxis = pathStartDominantAxisWasHorizontal
                ? minion.Center.X
                : minion.Center.Y;

            // Compute exit threshold in world pixels from the blocking tile
            // exitThresholdWorldPixels = (blockingTile + direction) * 16 + minionHalfSize
            // This is the point where minion's CENTER must pass to have cleared the obstacle
            float minionHalfSizeOnAxis = pathStartDominantAxisWasHorizontal
                ? minion.width / 2f
                : minion.height / 2f;

            int exitTileCoord = blockingTilePositionOnDominantAxis + pathStartDirectionToPlayer;
            float exitThresholdWorldPixels = exitTileCoord * 16f + minionHalfSizeOnAxis;

            // Check if minion has moved past the threshold on the dominant axis
            bool hasMovedPastBlockingTile = pathStartDirectionToPlayer > 0
                ? minionCenterOnDominantAxis >= exitThresholdWorldPixels
                : minionCenterOnDominantAxis <= exitThresholdWorldPixels;

            if (hasMovedPastBlockingTile) {
                if (DebugTethering) {
                    string axisName = pathStartDominantAxisWasHorizontal ? "X" : "Y";
                    Main.NewText($"[TETHER] {minion.Name} passed blocking exit on {axisName} (center={minionCenterOnDominantAxis:F0}, threshold={exitThresholdWorldPixels:F0}) - ending path follow", Color.Green);
                }
                EndPathFollowingState(minion);
                return;
            }

            // Debug: Show why we're still path-following
            if (DebugTethering && Main.GameUpdateCount % 60 == 0) {
                string axisName = pathStartDominantAxisWasHorizontal ? "X" : "Y";
                Main.NewText($"[TETHER] {minion.Name} on {axisName}: center={minionCenterOnDominantAxis:F0}, need to reach {exitThresholdWorldPixels:F0}px", Color.Gray);
            }

            // Check if we have valid waypoints
            if (currentPathWaypoints == null || currentWaypointIndex >= currentPathWaypoints.Count) {
                // Path exhausted but not at owner - switch to phasing
                if (DebugTethering) {
                    Main.NewText($"[TETHER] {minion.Name} A* path exhausted, switching to phase", Color.Yellow);
                }
                EndPathFollowingState(minion);
                StartPhasingState(minion, "path exhausted");
                return;
            }

            // Move toward current waypoint - disable tileCollide so we can actually follow the path!
            minion.tileCollide = false;

            // Movement budget approach: consume waypoints as we move, carrying leftover distance forward
            // This prevents "stopping" at each waypoint and wasting motion
            float movementBudgetRemaining = PathFollowSpeed;  // 3 pixels this tick
            Vector2 currentPosition = minion.Center;
            Vector2 finalMoveDirection = Vector2.Zero;

            while (movementBudgetRemaining > 0.01f && currentWaypointIndex < currentPathWaypoints.Count) {
                Vector2 targetWaypoint = currentPathWaypoints[currentWaypointIndex];
                Vector2 toWaypoint = targetWaypoint - currentPosition;
                float distToWaypoint = toWaypoint.Length();

                if (distToWaypoint <= movementBudgetRemaining) {
                    // Can reach this waypoint with budget to spare - consume it and continue
                    currentPosition = targetWaypoint;
                    movementBudgetRemaining -= distToWaypoint;
                    if (distToWaypoint > 0.01f) {
                        finalMoveDirection = toWaypoint / distToWaypoint;
                    }
                    currentWaypointIndex++;
                } else {
                    // Can't reach waypoint this tick - use all remaining budget moving toward it
                    finalMoveDirection = toWaypoint / distToWaypoint;  // normalized direction
                    currentPosition += finalMoveDirection * movementBudgetRemaining;
                    movementBudgetRemaining = 0;
                }
            }

            // Check if we've exhausted the path (reached final waypoint)
            if (currentWaypointIndex >= currentPathWaypoints.Count) {
                if (DebugTethering) {
                    Main.NewText($"[TETHER] {minion.Name} reached end of A* path - success!", Color.Green);
                }
                EndPathFollowingState(minion);
                return;
            }

            // Apply position change directly (more precise than velocity-based movement)
            minion.Center = currentPosition;

            // Set velocity to point in final direction (for sprite facing and smooth visuals)
            minion.velocity = finalMoveDirection * 0.5f;

            if (DebugTethering && Main.GameUpdateCount % 30 == 0) {
                Vector2 nextWaypoint = currentPathWaypoints[currentWaypointIndex];
                float distToNextWp = Vector2.Distance(minion.Center, nextWaypoint);
                string horizDir = finalMoveDirection.X > 0.3f ? "R" : (finalMoveDirection.X < -0.3f ? "L" : "-");
                string vertDir = finalMoveDirection.Y > 0.3f ? "D" : (finalMoveDirection.Y < -0.3f ? "U" : "-");
                Main.NewText($"[TETHER] {minion.Name} wp {currentWaypointIndex}/{currentPathWaypoints.Count}, dist={distToNextWp:F0}px, dir={horizDir}{vertDir}", Color.Gray);
            }

            // Spawn subtle trail while path-following
            if (Main.rand.NextBool(8)) {
                Dust pathDust = Dust.NewDustPerfect(
                    minion.Center,
                    DustID.GreenTorch,
                    -minion.velocity * 0.1f,
                    Alpha: 180,
                    Scale: 0.4f
                );
                pathDust.noGravity = true;
            }
        }

        /// <summary>
        /// Apply phasing movement - fly through walls toward owner.
        /// </summary>
        private void ApplyPhasingMovement(Projectile minion, Player ownerPlayer)
        {
            uint currentTick = Main.GameUpdateCount;
            Vector2 directionToOwner = ownerPlayer.Center - minion.Center;
            float distanceToOwner = directionToOwner.Length();

            // Check if we've reached the owner (has simple path and close enough)
            bool hasSimplePath = MinionHasSimplePathToLocation(minion, ownerPlayer.position);
            if (hasSimplePath && distanceToOwner < PhasingArrivalDistance) {
                // Arrived - end phasing
                EndPhasingState(minion);

                if (DebugTethering) {
                    Main.NewText($"[TETHER] {minion.Name} reached owner - phasing complete", Color.Green);
                }
                return;
            }

            // Check for phasing timeout (stuck in geometry?)
            int phasingDurationTicks = (int)(currentTick - phasingStartedTick);
            if (phasingDurationTicks >= PhasingTimeoutTicks) {
                // Force teleport
                if (DebugTethering) {
                    Main.NewText($"[TETHER] {minion.Name} phasing timeout - force teleporting", Color.Red);
                }
                EndPhasingState(minion);
                TeleportMinionToPlayer(minion, ownerPlayer, "phasing timeout");
                return;
            }

            // Continue phasing - fly toward owner through walls
            minion.tileCollide = false;

            if (distanceToOwner > 0) {
                directionToOwner.Normalize();

                // Scale velocity based on distance - slow down as we approach
                float speedFactor = distanceToOwner >= PhasingSlowdownDistance
                    ? 1f
                    : distanceToOwner / PhasingSlowdownDistance;

                float currentSpeed = MathHelper.Lerp(2f, PhasingMaxFlySpeed, speedFactor);
                minion.velocity = directionToOwner * currentSpeed;
            }

            // Spawn trail particles while phasing
            if (Main.rand.NextBool(3)) {
                Dust phaseDust = Dust.NewDustPerfect(
                    minion.Center,
                    DustID.MagicMirror,
                    -minion.velocity * 0.2f,
                    Alpha: 150,
                    Scale: 0.6f
                );
                phaseDust.noGravity = true;
            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      STATE MANAGEMENT                              ║
        // ╚════════════════════════════════════════════════════════════════════╝

        private void StartPathFollowingState(Projectile minion, List<Vector2> pathWaypoints, bool dominantAxisIsHorizontal, Vector2 vectorToPlayer, int blockingTileCoord, string reason)
        {
            if (isFollowingPath || isPhasingToOwner)
                return; // Already in a recall state

            isFollowingPath = true;
            currentPathWaypoints = pathWaypoints;
            currentWaypointIndex = 0;

            // Store original tileCollide to restore on exit
            pathFollowingOriginalTileCollide = minion.tileCollide;

            // Record the dominant axis and direction for exit condition
            pathStartDominantAxisWasHorizontal = dominantAxisIsHorizontal;
            pathStartDirectionToPlayer = dominantAxisIsHorizontal
                ? (vectorToPlayer.X > 0 ? 1 : -1)
                : (vectorToPlayer.Y > 0 ? 1 : -1);

            // Store the BLOCKING TILE position (tile coordinate on dominant axis)
            // Exit condition will be when minion passes this tile + 1 toward player
            blockingTilePositionOnDominantAxis = blockingTileCoord;

            if (DebugTethering) {
                string axisName = dominantAxisIsHorizontal ? "X" : "Y";
                string dirName = pathStartDirectionToPlayer > 0 ? "+" : "-";
                int exitTile = blockingTileCoord + pathStartDirectionToPlayer;
                Main.NewText($"[TETHER] {minion.Name} starting A* path ({pathWaypoints.Count} wp) on {axisName}{dirName}, blocking={blockingTileCoord}, exit@{exitTile}: {reason}", Color.Lime);
            }
        }

        private void EndPathFollowingState(Projectile minion)
        {
            // Restore original tileCollide state
            minion.tileCollide = pathFollowingOriginalTileCollide;

            isFollowingPath = false;
            currentPathWaypoints = null;
            currentWaypointIndex = 0;
            pathStartDominantAxisWasHorizontal = false;
            blockingTilePositionOnDominantAxis = 0;
            pathStartDirectionToPlayer = 0;

            // Set cooldown to prevent immediate re-entry into pathfinding (1.5 seconds)
            pathFollowingEndedTick = Main.GameUpdateCount;
        }

        private void StartPhasingState(Projectile minion, string reason)
        {
            if (isPhasingToOwner)
                return; // Already phasing

            // End path-following if active
            if (isFollowingPath) {
                EndPathFollowingState(minion);
            }

            isPhasingToOwner = true;
            phasingStartedTick = Main.GameUpdateCount;
            originalTileCollide = minion.tileCollide;
            minion.tileCollide = false;

            // Spawn phase-out effect
            SpawnPhaseEffect(minion.Center, fadeOut: true);

            if (DebugTethering) {
                Main.NewText($"[TETHER] {minion.Name} starting PHASE to owner: {reason}", Color.Orange);
            }
        }

        private void EndPhasingState(Projectile minion)
        {
            isPhasingToOwner = false;
            minion.tileCollide = originalTileCollide;

            // Spawn phase-in effect
            SpawnPhaseEffect(minion.Center, fadeOut: false);
        }

        /// <summary>
        /// Force teleport minion to player (fallback when phasing fails).
        /// </summary>
        private void TeleportMinionToPlayer(Projectile minion, Player targetPlayer, string reason)
        {
            // Calculate new position near player
            float randomOffsetX = Main.rand.NextFloat(-40f, 40f);
            Vector2 newPosition = targetPlayer.Center + new Vector2(randomOffsetX, -20f);

            // Spawn dust effect at old position
            SpawnPhaseEffect(minion.Center, fadeOut: true);

            // Move minion
            minion.Center = newPosition;
            minion.velocity = Vector2.Zero;

            // Spawn dust effect at new position
            SpawnPhaseEffect(newPosition, fadeOut: false);

            if (DebugTethering) {
                Main.NewText($"[TETHER] Teleported {minion.Name}: {reason}", Color.Cyan);
            }
        }

        /// <summary>
        /// Spawn visual phase effect (dust particles).
        /// </summary>
        private static void SpawnPhaseEffect(Vector2 effectPosition, bool fadeOut)
        {
            int dustCount = fadeOut ? 15 : 20;
            int dustType = DustID.MagicMirror;

            for (int i = 0; i < dustCount; i++) {
                float angle = Main.rand.NextFloat() * MathHelper.TwoPi;
                float speed = Main.rand.NextFloat(1f, 3f);
                Vector2 velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                );

                Dust phaseDust = Dust.NewDustPerfect(
                    effectPosition + velocity * 3f,
                    dustType,
                    velocity * (fadeOut ? 2f : -0.5f),
                    Alpha: 100,
                    Scale: 0.8f
                );
                phaseDust.noGravity = true;
            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      PATH DEBUG                                    ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Debug helper: Print each step of the path as tile deltas.
        /// This shows the ACTUAL movements, not just waypoint count.
        /// </summary>
        private static void DebugPrintPathSteps(List<Vector2> pathWaypoints, string context)
        {
            if (pathWaypoints.Count == 0) {
                Main.NewText($"[TETHER] {context}: EMPTY PATH!", Color.Red);
                return;
            }

            // Convert world pixel waypoints to tile positions
            var tilePositions = new List<Point>();
            foreach (Vector2 worldPos in pathWaypoints) {
                tilePositions.Add(worldPos.ToTileCoordinates());
            }

            // Build string of relative moves
            var moves = new System.Text.StringBuilder();
            int totalManhattanDistance = 0;

            for (int i = 1; i < tilePositions.Count; i++) {
                Point prev = tilePositions[i - 1];
                Point curr = tilePositions[i];
                int dx = curr.X - prev.X;
                int dy = curr.Y - prev.Y;
                totalManhattanDistance += Math.Abs(dx) + Math.Abs(dy);
                moves.Append($"({dx:+0;-0;0},{dy:+0;-0;0})");
            }

            int waypointCount = pathWaypoints.Count;
            int stepCount = waypointCount - 1;

            Main.NewText($"[TETHER] {context}: {waypointCount} waypoints, {stepCount} steps, manhattan={totalManhattanDistance}", Color.Gray);
            if (moves.Length > 0) {
                // Print moves in chunks to avoid chat overflow
                string moveStr = moves.ToString();
                if (moveStr.Length > 100) {
                    Main.NewText($"  Moves: {moveStr[..100]}...", Color.DarkGray);
                }
                else {
                    Main.NewText($"  Moves: {moveStr}", Color.DarkGray);
                }
            }
        }

    }

    /// <summary>
    /// GlobalTile that detects when players place blocks.
    /// Sets the tick when placed for TetheredMinionProjectile to check on the NEXT frame.
    /// </summary>
    public class TetheredMinionTileDetector : GlobalTile
    {
        public override void PlaceInWorld(int i, int j, int type, Item item)
        {
            // Find which player placed this tile (check who has itemTime > 0 and is placing)
            for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++) {
                Player player = Main.player[playerIndex];
                if (player.active && !player.dead && player.itemTime > 0) {
                    // Check if player's held item could place this tile
                    if (player.HeldItem.createTile == type) {
                        // Schedule cheese check for next frame (tile doesn't exist yet in PlaceInWorld)
                        TetheredMinionProjectile.DoCheeseCheckOnTick = (int)Main.GameUpdateCount + 1;
                        TetheredMinionProjectile.BlockPlacerPlayerIndex = playerIndex;
                        return;
                    }
                }
            }
        }
    }
}