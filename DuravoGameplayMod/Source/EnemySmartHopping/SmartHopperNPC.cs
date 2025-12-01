// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DuravoQOLMod.EnemySmartHopping
{
    /// <summary>
    /// GlobalNPC that makes ground enemies calculate precise jump trajectories
    /// to land on top of short walls (1-4 tiles) instead of overshooting.
    /// 
    /// This prevents cheese tactics like:
    /// - Murder holes (2-tile pits that cause zombies to overshoot)
    /// - Short barricades (walls that zombies jump over instead of onto)
    /// - Ledge exploits (natural terrain that breaks enemy pathing)
    /// </summary>
    public class SmartHopperNPC : GlobalNPC
    {
        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                        CONFIG ACCESSORS                            ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Get debug enemy smart hopping setting from mod config</summary>
        private static bool DebugSmartHopping => ModContent.GetInstance<DuravoQOLModConfig>()?.Debug?.DebugEnemySmartHopping ?? false;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                        TUNABLE CONSTANTS                           ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Terraria's default gravity (pixels per tick²)</summary>
        private const float GravityPixelsPerTickSquared = 0.3f;

        /// <summary>Pixels per tile</summary>
        private const float PixelsPerTile = 16f;

        /// <summary>Maximum wall height we'll smart-hop over (tiles)</summary>
        private const int MaxWallHeightTiles = 4;

        /// <summary>Height multiplier for initial velocity (15% extra to clear edge)</summary>
        private const float HeightMarginMultiplier = 1.15f;

        /// <summary>Minimum vertical velocity for smart hop</summary>
        private const float MinVerticalVelocity = -2f;

        /// <summary>Maximum vertical velocity for smart hop</summary>
        private const float MaxVerticalVelocity = -12f;

        /// <summary>Maximum horizontal velocity for smart hop</summary>
        private const float MaxHorizontalSpeed = 5f;

        /// <summary>
        /// Required vertical clearance above the wall for the NPC to land (tiles).
        /// Default is 3 tiles (typical zombie height).
        /// </summary>
        private const int RequiredLandingClearanceTiles = 3;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          MAIN LOGIC                                ║
        // ╚════════════════════════════════════════════════════════════════════╝

        public override bool PreAI(NPC npc)
        {
            // Check if feature is enabled in config (static accessor for hot-path)
            if (!DuravoQOLModConfig.EnableEnemySmartHopping)
                return true;

            // Only process walking-type enemies (not flying, swimming, etc.)
            if (!IsWalkingTypeEnemy(npc))
                return true;

            // Must be currently grounded (not mid-jump)
            // Some NPCs have tiny Y velocity when grounded due to slopes
            bool isCurrentlyGrounded = Math.Abs(npc.velocity.Y) <= 0.1f;
            if (!isCurrentlyGrounded)
                return true;

            // Must have a valid target
            if (npc.target < 0 || npc.target >= Main.maxPlayers)
                return true;

            Player targetPlayer = Main.player[npc.target];
            if (!targetPlayer.active || targetPlayer.dead)
                return true;

            // Determine direction to player (-1 = left, +1 = right)
            int directionToPlayer = (targetPlayer.Center.X > npc.Center.X) ? 1 : -1;

            // Get NPC's tile position (center of NPC)
            Point npcTilePosition = npc.Center.ToTileCoordinates();

            // Check if blocked by solid wall in direction of player
            int adjacentTileX = npcTilePosition.X + directionToPlayer;
            int npcFeetTileY = npcTilePosition.Y; // NPC's feet level

            if (!IsSolidBlockingTile(adjacentTileX, npcFeetTileY))
                return true; // Not blocked - normal AI handles this

            // Find the wall height - how many tiles until we find clear space?
            int wallHeightTiles = FindWallHeight(adjacentTileX, npcFeetTileY);

            if (wallHeightTiles == 0 || wallHeightTiles > MaxWallHeightTiles)
                return true; // Wall too high or no clearance - normal AI

            // Check if landing zone is clear (NPC needs vertical space to land)
            if (!IsLandingZoneClear(adjacentTileX, npcFeetTileY - wallHeightTiles, RequiredLandingClearanceTiles))
                return true; // Can't fit on top of wall - normal AI

            // Calculate and apply smart jump!
            ApplySmartJumpVelocity(npc, wallHeightTiles, directionToPlayer);

            return true; // Let normal AI continue (we just adjusted velocity)
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      PHYSICS CALCULATIONS                          ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Calculate and apply precise jump velocity to land on top of a wall.
        /// 
        /// Physics:
        /// - Vertical: vy = -sqrt(2 * gravity * height) * 1.15 (with margin)
        /// - Time to peak: t_peak = -vy / gravity
        /// - Peak height: peak = vy² / (2 * gravity)
        /// - Fall distance: fall = peak - wallHeight
        /// - Fall time: t_fall = sqrt(2 * fall / gravity)
        /// - Total time: t_total = t_peak + t_fall
        /// - Horizontal: vx = distance / t_total * direction
        /// </summary>
        private void ApplySmartJumpVelocity(NPC npc, int wallHeightTiles, int directionToPlayer)
        {
            float targetHeightPixels = wallHeightTiles * PixelsPerTile;

            // Calculate vertical velocity: enough to reach target height with 15% margin
            float verticalVelocityForHeight = -(float)Math.Sqrt(2f * GravityPixelsPerTickSquared * targetHeightPixels);
            float smartJumpVerticalVelocity = verticalVelocityForHeight * HeightMarginMultiplier;

            // Clamp for sanity
            smartJumpVerticalVelocity = Math.Clamp(smartJumpVerticalVelocity, MaxVerticalVelocity, MinVerticalVelocity);

            // Calculate time in air
            float timeToReachPeak = -smartJumpVerticalVelocity / GravityPixelsPerTickSquared;
            float peakHeightPixels = (smartJumpVerticalVelocity * smartJumpVerticalVelocity) / (2f * GravityPixelsPerTickSquared);
            float fallDistancePixels = peakHeightPixels - targetHeightPixels;
            float timeToFallToWall = (float)Math.Sqrt(2f * Math.Max(0f, fallDistancePixels) / GravityPixelsPerTickSquared);
            float totalFlightTimeTicks = timeToReachPeak + timeToFallToWall;

            // Calculate horizontal velocity to land on wall
            // We want to land about 1.5 tiles into the wall (solidly on top)
            float horizontalDistancePixels = PixelsPerTile * 1.5f;
            float smartJumpHorizontalVelocity = (horizontalDistancePixels / totalFlightTimeTicks) * directionToPlayer;

            // Clamp horizontal velocity
            smartJumpHorizontalVelocity = Math.Clamp(smartJumpHorizontalVelocity, -MaxHorizontalSpeed, MaxHorizontalSpeed);

            // Apply the calculated velocities
            npc.velocity.Y = smartJumpVerticalVelocity;
            npc.velocity.X = smartJumpHorizontalVelocity;

            if (DebugSmartHopping) {
                Main.NewText($"[DuravoQOL] Smart hop: {npc.FullName} wall={wallHeightTiles}t, vy={smartJumpVerticalVelocity:F2}, vx={smartJumpHorizontalVelocity:F2}, t={totalFlightTimeTicks:F1}", Color.Lime);
            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                       TILE DETECTION                               ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Find wall height: how many tiles up until we find clear space?
        /// Returns 0 if wall is higher than MAX_WALL_HEIGHT or no clearance found.
        /// </summary>
        private int FindWallHeight(int wallTileX, int baseTileY)
        {
            for (int heightOffset = 0; heightOffset <= MaxWallHeightTiles; heightOffset++) {
                int checkTileY = baseTileY - heightOffset - 1; // Check one above current height
                if (!IsSolidBlockingTile(wallTileX, checkTileY)) {
                    // Found clear space at this height
                    return heightOffset + 1;
                }
            }
            return 0; // Wall too high
        }

        /// <summary>
        /// Check if landing zone has enough vertical clearance for NPC to land.
        /// </summary>
        private bool IsLandingZoneClear(int landingTileX, int landingTileY, int requiredClearanceTiles)
        {
            for (int heightOffset = 0; heightOffset < requiredClearanceTiles; heightOffset++) {
                if (IsSolidBlockingTile(landingTileX, landingTileY - heightOffset)) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check if a tile is solid and blocks movement.
        /// Excludes platforms (tileSolidTop).
        /// </summary>
        private bool IsSolidBlockingTile(int tileX, int tileY)
        {
            // Bounds check
            if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
                return true; // Out of bounds = solid

            Tile tile = Main.tile[tileX, tileY];
            return tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                       NPC CLASSIFICATION                           ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Check if NPC is a walking-type enemy (movement TYPE, not current state).
        ///
        /// AI Styles:
        /// - 3: Fighter AI (zombies, skeletons, goblins, etc.)
        /// - 26: Unicorn AI (unicorn)
        /// - 38: Tortoise AI (giant tortoise)
        ///
        /// These are walking enemies that move by walking/jumping.
        /// Flying, swimming, and teleporting enemies are excluded.
        ///
        /// NOTE: This checks NPC TYPE, not whether it's currently grounded.
        /// Use velocity.Y check for current grounded state.
        /// </summary>
        private bool IsWalkingTypeEnemy(NPC npc)
        {
            // Must be hostile
            if (npc.friendly || npc.townNPC)
                return false;

            // Check AI style for ground-walking behavior
            return npc.aiStyle == 3 ||   // Fighter AI (zombies, skeletons, etc.)
                   npc.aiStyle == 26 ||  // Unicorn AI
                   npc.aiStyle == 38;    // Tortoise AI
        }
    }
}