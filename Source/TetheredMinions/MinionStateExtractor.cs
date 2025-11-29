// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace DuravoQOLMod.TetheredMinions
{
    /// <summary>
    /// Represents the current behavioral state of a minion.
    /// Extracted from analysis of decompiled Terraria 1.4.0.5 Projectile.cs.
    /// </summary>
    public enum MinionState
    {
        Unknown,
        Idle,             // Near player, no target, stationary
        Following,        // Moving toward player (NOT phasing) - PRIMARY PATHFIND TARGET
        Attacking,        // Has target, moving toward/fighting enemy
        Returning,        // Exceeded leash, PHASING back to player (ai[0]==1)
        Dashing,          // UFO/Cell teleport-dash (phasing)
        Spawning,         // Initial spawn/alpha fade-in
    }

    /// <summary>
    /// Movement style of the minion.
    /// </summary>
    public enum MinionLocomotion
    {
        Unknown,
        Flying,
        Ground,
        Worm,             // Stardust Dragon segments
    }

    /// <summary>
    /// Comprehensive state information extracted from a minion projectile.
    /// </summary>
    public struct MinionStateInfo
    {
        public MinionState State;
        public MinionLocomotion Locomotion;
        public bool AlwaysPhases;           // Never needs pathfinding (Stardust Dragon, Terraprisma, etc.)
        public bool CurrentlyPhasing;       // Right now ignoring tiles (Returning/Dashing/ai[0]==1)
        public bool HasTarget;
        public int TargetNPCIndex;          // -1 if no target
        public Vector2 TargetPosition;      // Target center if valid
        public bool TargetIsWhipTagged;     // Player-designated target
        public int MinionPosIndex;          // For ground minions (formation slot)
        public float AttackCooldown;        // ai[1] for most ranged minions

        public bool TargetDataValid => HasTarget && TargetNPCIndex >= 0;

        /// <summary>
        /// True if minion is trying to reach player but NOT phasing.
        /// This is the primary pathfinding assist case.
        /// </summary>
        public bool NeedsPathToPlayer => State == MinionState.Following || State == MinionState.Idle;
    }

    /// <summary>
    /// Static utility class for extracting minion state from projectiles.
    /// Based on decompiled Terraria 1.4.0.5 Projectile AI analysis.
    /// </summary>
    public static class MinionStateExtractor
    {
        // Minions that ALWAYS phase through blocks
        private static readonly HashSet<int> AlwaysPhasingTypes = new()
        {
            625, 626, 627, 628,  // Stardust Dragon segments
            407,                  // Sharknado (forced in AI)
            755,                  // Sanguine Bat
            946,                  // Terraprisma
        };

        // Ground-based aiStyles (Pygmy=26, Pirate=67)
        private static readonly HashSet<int> GroundAiStyles = new() { 26, 67 };

        /// <summary>
        /// Extracts comprehensive state info from a minion projectile.
        /// Returns struct with State=Unknown if not a recognized minion.
        /// </summary>
        public static MinionStateInfo GetMinionState(Projectile proj)
        {
            var info = new MinionStateInfo {
                State = MinionState.Unknown,
                Locomotion = MinionLocomotion.Unknown,
                AlwaysPhases = false,
                CurrentlyPhasing = false,
                HasTarget = false,
                TargetNPCIndex = -1,
                TargetPosition = Vector2.Zero,
                TargetIsWhipTagged = false,
                MinionPosIndex = -1,
                AttackCooldown = 0f,
            };

            if (!proj.active || !proj.minion)
                return info;

            // Determine locomotion type
            info.Locomotion = GetLocomotion(proj);
            info.AlwaysPhases = AlwaysPhasingTypes.Contains(proj.type);

            // Extract state based on aiStyle
            switch (proj.aiStyle) {
                case 26: // Pygmy, Spider (ground-based)
                    ExtractAiStyle26(proj, ref info);
                    break;

                case 54: // Raven
                    ExtractAiStyle54(proj, ref info);
                    break;

                case 62: // Hornet, Imp, Sharknado, UFO, Stardust Cell
                    ExtractAiStyle62(proj, ref info);
                    break;

                case 66: // Twins, Deadly Sphere
                    ExtractAiStyle66(proj, ref info);
                    break;

                case 67: // Pirate minions
                    ExtractAiStyle67(proj, ref info);
                    break;

                case 121: // Stardust Dragon
                    ExtractAiStyle121(proj, ref info);
                    break;

                case 156: // Sanguine Bat, Terraprisma
                    ExtractAiStyle156(proj, ref info);
                    break;

                default:
                    // Unknown aiStyle - try generic extraction
                    ExtractGeneric(proj, ref info);
                    break;
            }

            // Check for whip-tagged target (universal)
            CheckWhipTarget(proj, ref info);

            // Determine if currently phasing
            info.CurrentlyPhasing = info.AlwaysPhases ||
                                    info.State == MinionState.Returning ||
                                    info.State == MinionState.Dashing ||
                                    !proj.tileCollide;

            return info;
        }

        private static MinionLocomotion GetLocomotion(Projectile proj)
        {
            // Worm-style (Stardust Dragon)
            if (proj.type >= 625 && proj.type <= 628)
                return MinionLocomotion.Worm;

            // Ground-based aiStyles
            if (GroundAiStyles.Contains(proj.aiStyle))
                return MinionLocomotion.Ground;

            return MinionLocomotion.Flying;
        }

        /// <summary>
        /// aiStyle 26: Pygmy (191-194), Spider (376-379)
        /// Ground-based minions with minionPos formation
        /// ai[0]: target NPC whoAmI (0 or negative = no target)
        /// ai[1]: attack cooldown timer
        /// </summary>
        private static void ExtractAiStyle26(Projectile proj, ref MinionStateInfo info)
        {
            info.MinionPosIndex = proj.minionPos;
            info.AttackCooldown = proj.ai[1];

            int targetIdx = (int)proj.ai[0];
            if (targetIdx > 0 && targetIdx < Main.maxNPCs) {
                NPC target = Main.npc[targetIdx];
                if (target.active && target.CanBeChasedBy(proj)) {
                    info.HasTarget = true;
                    info.TargetNPCIndex = targetIdx;
                    info.TargetPosition = target.Center;
                    info.State = MinionState.Attacking;
                    return;
                }
            }

            // No target - determine if idle, following, or returning (phasing)
            Player owner = Main.player[proj.owner];
            float distanceToOwner = Vector2.Distance(proj.Center, owner.Center);

            // Check if currently phasing (tileCollide forced off by exceeding leash)
            if (!proj.tileCollide) {
                info.State = MinionState.Returning; // Phasing back
            }
            else if (distanceToOwner > 80f) {
                // Far enough to be actively following (not at destination)
                info.State = MinionState.Following; // PRIMARY PATHFIND CASE
            }
            else {
                info.State = MinionState.Idle; // Close to player, stationary
            }
        }

        /// <summary>
        /// aiStyle 54: Raven (317)
        /// Flying contact minion, bounces on tiles at 0.6x
        /// ai[0]: 0 = idle/follow, 1+ = has target
        /// </summary>
        private static void ExtractAiStyle54(Projectile proj, ref MinionStateInfo info)
        {
            if (proj.ai[0] >= 1f) {
                info.State = MinionState.Attacking;
                // Raven target is selected per-frame, not stored
            }
            else {
                Player owner = Main.player[proj.owner];
                float distanceToOwner = Vector2.Distance(proj.Center, owner.Center);

                if (!proj.tileCollide)
                    info.State = MinionState.Returning; // Phasing
                else if (distanceToOwner > 100f)
                    info.State = MinionState.Following;
                else
                    info.State = MinionState.Idle;
            }
        }

        /// <summary>
        /// aiStyle 62: Hornet (373), Imp (375), Sharknado (407), UFO (423), Cell (613)
        /// ai[0]: 0 = normal, 1 = returning (PHASING), 2 = dashing
        /// ai[1]: attack/dash cooldown
        /// localAI[0]: dash cooldown (UFO/Cell)
        /// </summary>
        private static void ExtractAiStyle62(Projectile proj, ref MinionStateInfo info)
        {
            info.AttackCooldown = proj.ai[1];

            // ai[0] == 1 is the PHASING return state
            if ((int)proj.ai[0] == 1) {
                info.State = MinionState.Returning; // Phasing back to player
                return;
            }

            if ((int)proj.ai[0] == 2) {
                info.State = MinionState.Dashing;
                return;
            }

            // ai[0] == 0: normal state - could be idle, following, or attacking
            Player owner = Main.player[proj.owner];
            float distanceToOwner = Vector2.Distance(proj.Center, owner.Center);

            // These minions acquire targets per-frame, so we check distance
            if (distanceToOwner > 100f)
                info.State = MinionState.Following;
            else
                info.State = MinionState.Idle;
            // Note: CheckWhipTarget may upgrade this to Attacking
        }

        /// <summary>
        /// aiStyle 66: Twins (387-388), Deadly Sphere (533)
        /// ai[0]: state machine value
        /// Deadly Sphere: 0-5 idle/follow, 6-8 attacking, 9+ returning (PHASING)
        /// Twins: 0 idle/follow, 1 returning (PHASING), 2 attacking
        /// </summary>
        private static void ExtractAiStyle66(Projectile proj, ref MinionStateInfo info)
        {
            float state = proj.ai[0];
            Player owner = Main.player[proj.owner];
            float distanceToOwner = Vector2.Distance(proj.Center, owner.Center);

            if (proj.type == 533) { // Deadly Sphere
                if (state >= 9f) {
                    info.State = MinionState.Returning; // Phasing
                }
                else if (state >= 6f && state <= 8f) {
                    info.State = MinionState.Attacking;
                }
                else if (distanceToOwner > 100f) {
                    info.State = MinionState.Following;
                }
                else {
                    info.State = MinionState.Idle;
                }
            }
            else { // Twins (387, 388)
                if (state == 1f) {
                    info.State = MinionState.Returning; // Phasing
                }
                else if (state == 2f) {
                    info.State = MinionState.Attacking;
                }
                else if (distanceToOwner > 100f) {
                    info.State = MinionState.Following;
                }
                else {
                    info.State = MinionState.Idle;
                }
            }
        }

        /// <summary>
        /// aiStyle 67: Pirate minions (393-395)
        /// ai[0]: 0 = grounded, 1 = being carried by parrot (PHASING)
        /// ai[1]: timer
        /// localAI[0]: attack cooldown
        /// </summary>
        private static void ExtractAiStyle67(Projectile proj, ref MinionStateInfo info)
        {
            info.AttackCooldown = proj.localAI[0];

            if (proj.ai[0] == 1f) {
                // Being carried by parrot = returning and phasing
                info.State = MinionState.Returning;
                info.CurrentlyPhasing = true;
            }
            else {
                Player owner = Main.player[proj.owner];
                float distanceToOwner = Vector2.Distance(proj.Center, owner.Center);

                if (distanceToOwner > 80f)
                    info.State = MinionState.Following; // Walking toward player
                else
                    info.State = MinionState.Idle;
            }
        }

        /// <summary>
        /// aiStyle 121: Stardust Dragon (625-628)
        /// Only head (625) has meaningful state
        /// Always phases, worm locomotion
        /// </summary>
        private static void ExtractAiStyle121(Projectile proj, ref MinionStateInfo info)
        {
            if (proj.type != 625) {
                // Body/tail segment - just follows head
                info.State = MinionState.Unknown;
                return;
            }

            // Head checks for targets each frame
            // Always phases so Following vs Returning distinction less relevant
            Player owner = Main.player[proj.owner];
            float distanceToOwner = Vector2.Distance(proj.Center, owner.Center);
            info.State = distanceToOwner > 150f ? MinionState.Following : MinionState.Idle;
            // Note: State will be upgraded to Attacking by CheckWhipTarget if applicable
        }

        /// <summary>
        /// aiStyle 156: Sanguine Bat (755), Terraprisma (946)
        /// ai[0]: -1 = teleporting, 0 = idle, 1+ = attacking
        /// Always phases
        /// </summary>
        private static void ExtractAiStyle156(Projectile proj, ref MinionStateInfo info)
        {
            if (proj.ai[0] == -1f) {
                info.State = MinionState.Spawning;
            }
            else if (proj.ai[0] >= 1f) {
                info.State = MinionState.Attacking;
            }
            else {
                // ai[0] == 0: idle or following (always phases so less distinction)
                Player owner = Main.player[proj.owner];
                float distanceToOwner = Vector2.Distance(proj.Center, owner.Center);
                info.State = distanceToOwner > 100f ? MinionState.Following : MinionState.Idle;
            }
        }

        /// <summary>
        /// Generic fallback for unknown aiStyles
        /// </summary>
        private static void ExtractGeneric(Projectile proj, ref MinionStateInfo info)
        {
            Player owner = Main.player[proj.owner];
            float distanceToOwner = Vector2.Distance(proj.Center, owner.Center);

            if (!proj.tileCollide) {
                info.State = MinionState.Returning; // Phasing
            }
            else if (distanceToOwner > 100f) {
                info.State = MinionState.Following;
            }
            else if (proj.velocity.LengthSquared() < 1f) {
                info.State = MinionState.Idle;
            }
            else {
                info.State = MinionState.Unknown;
            }
        }

        /// <summary>
        /// Check if owner has a whip-tagged target
        /// </summary>
        private static void CheckWhipTarget(Projectile proj, ref MinionStateInfo info)
        {
            Player owner = Main.player[proj.owner];

            if (!owner.HasMinionAttackTargetNPC)
                return;

            int targetIdx = owner.MinionAttackTargetNPC;
            if (targetIdx < 0 || targetIdx >= Main.maxNPCs)
                return;

            NPC target = Main.npc[targetIdx];
            if (!target.active || !target.CanBeChasedBy(proj))
                return;

            // Check line of sight (unless always-phasing)
            bool canSee = info.AlwaysPhases || Collision.CanHitLine(
                proj.Center, 1, 1, target.Center, 1, 1);

            if (canSee) {
                info.HasTarget = true;
                info.TargetNPCIndex = targetIdx;
                info.TargetPosition = target.Center;
                info.TargetIsWhipTagged = true;

                if (info.State == MinionState.Idle || info.State == MinionState.Following)
                    info.State = MinionState.Attacking;
            }
        }

        /// <summary>
        /// Quick check: does this minion need pathfinding help right now?
        /// Primary use case: ground minions FOLLOWING player through terrain
        /// </summary>
        public static bool NeedsPathfindingConsideration(Projectile proj)
        {
            if (!proj.active || !proj.minion)
                return false;

            var state = GetMinionState(proj);

            // Never help always-phasing minions
            if (state.AlwaysPhases || state.CurrentlyPhasing)
                return false;

            // GROUND MINIONS FOLLOWING = PRIMARY TARGET
            // This is the main QoL use case
            if (state.Locomotion == MinionLocomotion.Ground &&
                state.State == MinionState.Following) {
                return true;
            }

            // FLYING MINIONS in Following state also need consideration
            if (state.Locomotion == MinionLocomotion.Flying &&
                state.State == MinionState.Following &&
                proj.tileCollide) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the position the minion is trying to reach
        /// </summary>
        public static Vector2 GetTargetDestination(Projectile proj, MinionStateInfo state)
        {
            if (state.HasTarget && state.State == MinionState.Attacking)
                return state.TargetPosition;

            // Otherwise, returning to player
            Player owner = Main.player[proj.owner];

            // Ground minions have formation positions
            if (state.Locomotion == MinionLocomotion.Ground && state.MinionPosIndex >= 0) {
                // Offset based on minionPos (alternating left/right of player)
                float offset = (state.MinionPosIndex + 1) * 20f;
                if (state.MinionPosIndex % 2 == 1)
                    offset = -offset;
                return owner.Center + new Vector2(offset, 0);
            }

            // Flying minions hover above player
            return owner.Center - new Vector2(0, 60);
        }
    }
}