// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System.Collections.Generic;
using Terraria.ID;

namespace DuravoQOLMod.TetheredMinions
{
    /// <summary>
    /// Minion projectile types that we track for tethering.
    /// Values match Terraria ProjectileID constants.
    /// </summary>
    public enum KnownMinionProjectileType
    {
        // Pre-Hardmode Ground Minions
        BabySlime = ProjectileID.BabySlime,               // 266 - Slime Staff

        // Pre-Hardmode Flying Minions
        BabyBird = ProjectileID.BabyBird,                 // 759 - Finch Staff
        Hornet = ProjectileID.Hornet,                     // 373 - Hornet Staff
        FlyingImp = ProjectileID.FlyingImp,               // 375 - Imp Staff
        VampireFrog = ProjectileID.VampireFrog,           // 758 - Vampire Frog Staff

        // Hardmode Ground Minions
        VenomSpider = ProjectileID.VenomSpider,           // 390 - Spider Staff
        JumperSpider = ProjectileID.JumperSpider,         // 391 - Spider Staff
        DangerousSpider = ProjectileID.DangerousSpider,   // 392 - Spider Staff
        Pygmy = ProjectileID.Pygmy,                       // 191 - Pygmy Staff
        Pygmy2 = ProjectileID.Pygmy2,                     // 192 - Pygmy Staff
        Pygmy3 = ProjectileID.Pygmy3,                     // 193 - Pygmy Staff
        Pygmy4 = ProjectileID.Pygmy4,                     // 194 - Pygmy Staff
        OneEyedPirate = ProjectileID.OneEyedPirate,       // 393 - Pirate Staff
        SoulscourgePirate = ProjectileID.SoulscourgePirate, // 394 - Pirate Staff
        PirateCaptain = ProjectileID.PirateCaptain,       // 395 - Pirate Staff

        // Hardmode Flying Minions
        Retanimini = ProjectileID.Retanimini,             // 387 - Optic Staff (Twins)
        Spazmamini = ProjectileID.Spazmamini,             // 388 - Optic Staff (Twins)
        Raven = ProjectileID.Raven,                       // 317 - Raven Staff
        BatOfLight = ProjectileID.BatOfLight,             // 755 - Sanguine Staff
        Tempest = ProjectileID.Tempest,                   // 407 - Tempest Staff (Sharknado)
        DeadlySphere = ProjectileID.DeadlySphere,         // 533 - Deadly Sphere Staff
        UFOMinion = ProjectileID.UFOMinion,               // 423 - Xeno Staff
        Smolstar = ProjectileID.Smolstar,                 // 864 - Blade Staff (Enchanted Dagger)

        // Post-Moon Lord Minions
        StardustCellMinion = ProjectileID.StardustCellMinion, // 613 - Stardust Cell Staff
        StardustDragon1 = ProjectileID.StardustDragon1,   // 625 - Stardust Dragon Staff (head)
        StardustDragon2 = ProjectileID.StardustDragon2,   // 626 - Stardust Dragon Staff (body)
        StardustDragon3 = ProjectileID.StardustDragon3,   // 627 - Stardust Dragon Staff (tail)
        StardustDragon4 = ProjectileID.StardustDragon4,   // 628 - Stardust Dragon Staff (segment)
        EmpressBlade = ProjectileID.EmpressBlade,         // 946 - Terraprisma

        // 1.4.1+ Minions (if IDs exist)
        // StormTiger variants - Desert Tiger Staff
        StormTigerGem = ProjectileID.StormTigerGem,       // 831
        StormTigerAttack = ProjectileID.StormTigerAttack, // 832
        StormTigerTier1 = ProjectileID.StormTigerTier1,   // 833
        StormTigerTier2 = ProjectileID.StormTigerTier2,   // 834
        StormTigerTier3 = ProjectileID.StormTigerTier3,   // 835
    }

    /// <summary>
    /// How a minion moves and interacts with tiles.
    /// </summary>
    public enum MinionMovementType
    {
        /// <summary>Ground-bound minion (walks, jumps, blocked by terrain)</summary>
        Ground,

        /// <summary>Flying minion that CANNOT pass through blocks (blocked by terrain)</summary>
        Fly,

        /// <summary>Flying minion that CAN pass through blocks (noclip/phase)</summary>
        Phase
    }

    /// <summary>
    /// Contains leash and detection range data for a minion type.
    /// Distances are in TILES (not pixels). Multiply by 16 for pixels.
    /// </summary>
    public readonly struct MinionRangeInfo
    {
        /// <summary>Projectile type ID from Terraria.ID.ProjectileID</summary>
        public readonly int ProjectileType;

        /// <summary>Human-readable name of the minion</summary>
        public readonly string DisplayName;

        /// <summary>Base enemy detection range in tiles</summary>
        public readonly float EnemyDetectionRangeTiles;

        /// <summary>Leash (return to player) distance in tiles. -1 = no vanilla leash behavior</summary>
        public readonly float LeashDistanceTiles;

        /// <summary>Whether this minion uses minionPos scaling (+2.5 tiles per slot)</summary>
        public readonly bool UsesMinionPosScaling;

        /// <summary>How the minion moves (ground, fly, phase-through-blocks)</summary>
        public readonly MinionMovementType MovementType;

        public MinionRangeInfo(
            int projectileType,
            string displayName,
            float enemyDetectionRangeTiles,
            float leashDistanceTiles,
            MinionMovementType movementType,
            bool usesMinionPosScaling = false)
        {
            ProjectileType = projectileType;
            DisplayName = displayName;
            EnemyDetectionRangeTiles = enemyDetectionRangeTiles;
            LeashDistanceTiles = leashDistanceTiles;
            MovementType = movementType;
            UsesMinionPosScaling = usesMinionPosScaling;
        }

        /// <summary>Get effective leash distance for a specific minion slot position</summary>
        public float GetEffectiveLeashDistance(int minionSlotPosition)
        {
            if (LeashDistanceTiles < 0) {
                return -1f; // No leash
            }
            if (UsesMinionPosScaling) {
                return LeashDistanceTiles + (2.5f * minionSlotPosition);
            }
            return LeashDistanceTiles;
        }

        /// <summary>Get effective detection range for a specific minion slot position</summary>
        public float GetEffectiveDetectionRange(int minionSlotPosition, bool playerTargeted = false)
        {
            float baseRange = EnemyDetectionRangeTiles;
            if (UsesMinionPosScaling) {
                baseRange += 2.5f * minionSlotPosition;
            }
            // Many minions get boosted range when player targets with whip (1.5x)
            if (playerTargeted && baseRange >= 62.5f) {
                return 187.5f;
            }
            return baseRange;
        }
    }

    /// <summary>
    /// Static lookup table for minion leash and detection ranges.
    /// Based on Terraria 1.4.4.9 source code analysis.
    /// </summary>
    public static class MinionLeashData
    {
        /// <summary>Default conservative leash distance for unknown minions (in tiles)</summary>
        public const float DefaultLeashDistanceTiles = 100f;

        /// <summary>Default conservative detection range for unknown minions (in tiles)</summary>
        public const float DefaultDetectionRangeTiles = 100f;

        // Pre-computed lookup table for all known minions
        private static readonly Dictionary<int, MinionRangeInfo> MinionRangeTable = new() {
            // ═══════════════════════════════════════════════════════════
            //  PRE-HARDMODE MINIONS
            // ═══════════════════════════════════════════════════════════

            // Baby Slime - Ground minion, no flying leash
            [ProjectileID.BabySlime] = new MinionRangeInfo(
                ProjectileID.BabySlime, "Baby Slime",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: -1f,  // Ground minion, uses different return logic
                movementType: MinionMovementType.Ground
            ),

            // Baby Finch - Tight leash (rests on player head when idle)
            [ProjectileID.BabyBird] = new MinionRangeInfo(
                ProjectileID.BabyBird, "Baby Finch",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: 50f,
                movementType: MinionMovementType.Fly  // Flies but blocked by tiles
            ),

            // Hornet - Tight leash, cannot pass through blocks
            [ProjectileID.Hornet] = new MinionRangeInfo(
                ProjectileID.Hornet, "Hornet",
                enemyDetectionRangeTiles: 125f,
                leashDistanceTiles: 62.5f,
                movementType: MinionMovementType.Fly
            ),

            // Flying Imp - Tight leash, cannot pass through blocks
            [ProjectileID.FlyingImp] = new MinionRangeInfo(
                ProjectileID.FlyingImp, "Flying Imp",
                enemyDetectionRangeTiles: 125f,
                leashDistanceTiles: 62.5f,
                movementType: MinionMovementType.Fly
            ),

            // Vampire Frog - Ground/water minion
            [ProjectileID.VampireFrog] = new MinionRangeInfo(
                ProjectileID.VampireFrog, "Vampire Frog",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: -1f,  // Ground minion
                movementType: MinionMovementType.Ground
            ),

            // ═══════════════════════════════════════════════════════════
            //  HARDMODE GROUND MINIONS
            // ═══════════════════════════════════════════════════════════

            // Spider variants - minionPos scaling, wall-climbing (ground-bound)
            [ProjectileID.VenomSpider] = new MinionRangeInfo(
                ProjectileID.VenomSpider, "Venom Spider",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: 87.5f,
                movementType: MinionMovementType.Ground,  // Wall-climbing but still ground-bound
                usesMinionPosScaling: true
            ),
            [ProjectileID.JumperSpider] = new MinionRangeInfo(
                ProjectileID.JumperSpider, "Jumper Spider",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: 87.5f,
                movementType: MinionMovementType.Ground,
                usesMinionPosScaling: true
            ),
            [ProjectileID.DangerousSpider] = new MinionRangeInfo(
                ProjectileID.DangerousSpider, "Dangerous Spider",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: 87.5f,
                movementType: MinionMovementType.Ground,
                usesMinionPosScaling: true
            ),

            // Pygmy variants - minionPos scaling, ground-bound
            [ProjectileID.Pygmy] = new MinionRangeInfo(
                ProjectileID.Pygmy, "Pygmy",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: 62.5f,
                movementType: MinionMovementType.Ground,
                usesMinionPosScaling: true
            ),
            [ProjectileID.Pygmy2] = new MinionRangeInfo(
                ProjectileID.Pygmy2, "Pygmy",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: 62.5f,
                movementType: MinionMovementType.Ground,
                usesMinionPosScaling: true
            ),
            [ProjectileID.Pygmy3] = new MinionRangeInfo(
                ProjectileID.Pygmy3, "Pygmy",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: 62.5f,
                movementType: MinionMovementType.Ground,
                usesMinionPosScaling: true
            ),
            [ProjectileID.Pygmy4] = new MinionRangeInfo(
                ProjectileID.Pygmy4, "Pygmy",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: 62.5f,
                movementType: MinionMovementType.Ground,
                usesMinionPosScaling: true
            ),

            // Pirate variants - Parrot carries back when too far, ground-bound
            [ProjectileID.OneEyedPirate] = new MinionRangeInfo(
                ProjectileID.OneEyedPirate, "One-Eyed Pirate",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: -1f,  // Parrot carries, not standard leash
                movementType: MinionMovementType.Ground
            ),
            [ProjectileID.SoulscourgePirate] = new MinionRangeInfo(
                ProjectileID.SoulscourgePirate, "Soulscourge Pirate",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: -1f,
                movementType: MinionMovementType.Ground
            ),
            [ProjectileID.PirateCaptain] = new MinionRangeInfo(
                ProjectileID.PirateCaptain, "Pirate Captain",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: -1f,
                movementType: MinionMovementType.Ground
            ),

            // ═══════════════════════════════════════════════════════════
            //  HARDMODE FLYING MINIONS
            // ═══════════════════════════════════════════════════════════

            // Twins (Optic Staff) - TIGHTEST flying leash! Cannot pass through blocks!
            [ProjectileID.Retanimini] = new MinionRangeInfo(
                ProjectileID.Retanimini, "Retanimini",
                enemyDetectionRangeTiles: 125f,
                leashDistanceTiles: 75f,  // ⚠️ TIGHTEST flying minion!
                movementType: MinionMovementType.Fly  // Cannot pass through blocks
            ),
            [ProjectileID.Spazmamini] = new MinionRangeInfo(
                ProjectileID.Spazmamini, "Spazmamini",
                enemyDetectionRangeTiles: 125f,
                leashDistanceTiles: 75f,  // ⚠️ TIGHTEST flying minion!
                movementType: MinionMovementType.Fly  // Cannot pass through blocks
            ),

            // Raven - Medium leash, cannot pass through blocks
            [ProjectileID.Raven] = new MinionRangeInfo(
                ProjectileID.Raven, "Raven",
                enemyDetectionRangeTiles: 56.25f,
                leashDistanceTiles: 87.5f,
                movementType: MinionMovementType.Fly
            ),

            // Sanguine Bat - Flies THROUGH blocks (phase)
            [ProjectileID.BatOfLight] = new MinionRangeInfo(
                ProjectileID.BatOfLight, "Sanguine Bat",
                enemyDetectionRangeTiles: 100f,
                leashDistanceTiles: -1f,  // Flies through blocks, special behavior
                movementType: MinionMovementType.Phase
            ),

            // Sharknado (Tempest Staff) - Loose leash, flies THROUGH blocks
            [ProjectileID.Tempest] = new MinionRangeInfo(
                ProjectileID.Tempest, "Sharknado",
                enemyDetectionRangeTiles: 125f,
                leashDistanceTiles: 125f,
                movementType: MinionMovementType.Phase
            ),

            // Deadly Sphere - Medium leash, cannot pass through blocks
            [ProjectileID.DeadlySphere] = new MinionRangeInfo(
                ProjectileID.DeadlySphere, "Deadly Sphere",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: 93.75f,
                movementType: MinionMovementType.Fly
            ),

            // UFO (Xeno Staff) - Loose leash, phases through blocks when returning
            [ProjectileID.UFOMinion] = new MinionRangeInfo(
                ProjectileID.UFOMinion, "UFO",
                enemyDetectionRangeTiles: 125f,
                leashDistanceTiles: 125f,
                movementType: MinionMovementType.Phase  // Phases when returning, blocked when attacking
            ),

            // Enchanted Dagger (Blade Staff) - Tight leash
            [ProjectileID.Smolstar] = new MinionRangeInfo(
                ProjectileID.Smolstar, "Enchanted Dagger",
                enemyDetectionRangeTiles: 50f,
                leashDistanceTiles: 56.25f,
                movementType: MinionMovementType.Fly
            ),

            // ═══════════════════════════════════════════════════════════
            //  POST-MOON LORD MINIONS
            // ═══════════════════════════════════════════════════════════

            // Stardust Cell - Medium leash, cannot pass through blocks
            [ProjectileID.StardustCellMinion] = new MinionRangeInfo(
                ProjectileID.StardustCellMinion, "Stardust Cell",
                enemyDetectionRangeTiles: 125f,
                leashDistanceTiles: 84.4f,
                movementType: MinionMovementType.Fly
            ),

            // Stardust Dragon - Loose leash, flies THROUGH blocks
            [ProjectileID.StardustDragon1] = new MinionRangeInfo(
                ProjectileID.StardustDragon1, "Stardust Dragon (Head)",
                enemyDetectionRangeTiles: 62.5f,
                leashDistanceTiles: 125f,
                movementType: MinionMovementType.Phase
            ),
            [ProjectileID.StardustDragon2] = new MinionRangeInfo(
                ProjectileID.StardustDragon2, "Stardust Dragon (Body)",
                enemyDetectionRangeTiles: -1f,  // Body follows head
                leashDistanceTiles: -1f,
                movementType: MinionMovementType.Phase
            ),
            [ProjectileID.StardustDragon3] = new MinionRangeInfo(
                ProjectileID.StardustDragon3, "Stardust Dragon (Tail)",
                enemyDetectionRangeTiles: -1f,
                leashDistanceTiles: -1f,
                movementType: MinionMovementType.Phase
            ),
            [ProjectileID.StardustDragon4] = new MinionRangeInfo(
                ProjectileID.StardustDragon4, "Stardust Dragon (Segment)",
                enemyDetectionRangeTiles: -1f,
                leashDistanceTiles: -1f,
                movementType: MinionMovementType.Phase
            ),

            // Terraprisma - Flies THROUGH blocks
            [ProjectileID.EmpressBlade] = new MinionRangeInfo(
                ProjectileID.EmpressBlade, "Terraprisma",
                enemyDetectionRangeTiles: 100f,
                leashDistanceTiles: -1f,  // Flies through blocks, very long range
                movementType: MinionMovementType.Phase
            ),

            // ═══════════════════════════════════════════════════════════
            //  DESERT TIGER (1.4.1+)
            // ═══════════════════════════════════════════════════════════

            [ProjectileID.StormTigerGem] = new MinionRangeInfo(
                ProjectileID.StormTigerGem, "Desert Tiger Gem",
                enemyDetectionRangeTiles: -1f,  // Tracking gem, not the minion itself
                leashDistanceTiles: -1f,
                movementType: MinionMovementType.Ground
            ),
            [ProjectileID.StormTigerAttack] = new MinionRangeInfo(
                ProjectileID.StormTigerAttack, "Desert Tiger Attack",
                enemyDetectionRangeTiles: -1f,  // Attack effect
                leashDistanceTiles: -1f,
                movementType: MinionMovementType.Ground
            ),
            [ProjectileID.StormTigerTier1] = new MinionRangeInfo(
                ProjectileID.StormTigerTier1, "Desert Tiger (Tier 1)",
                enemyDetectionRangeTiles: 100f,
                leashDistanceTiles: -1f,  // Pounce-based, not traditional leash
                movementType: MinionMovementType.Ground
            ),
            [ProjectileID.StormTigerTier2] = new MinionRangeInfo(
                ProjectileID.StormTigerTier2, "Desert Tiger (Tier 2)",
                enemyDetectionRangeTiles: 100f,
                leashDistanceTiles: -1f,
                movementType: MinionMovementType.Ground
            ),
            [ProjectileID.StormTigerTier3] = new MinionRangeInfo(
                ProjectileID.StormTigerTier3, "Desert Tiger (Tier 3)",
                enemyDetectionRangeTiles: 100f,
                leashDistanceTiles: -1f,
                movementType: MinionMovementType.Ground
            ),
        };

        /// <summary>
        /// Get the range info for a minion by its projectile type.
        /// Returns null if the minion is not in our table (unknown minion).
        /// </summary>
        public static MinionRangeInfo? GetMinionRangeInfo(int projectileType)
        {
            if (MinionRangeTable.TryGetValue(projectileType, out MinionRangeInfo info)) {
                return info;
            }
            return null;
        }

        /// <summary>
        /// Get the vanilla leash distance for a minion in tiles.
        /// Returns -1 if the minion has no standard leash (ground minion, phase-through, etc.)
        /// Returns default value for unknown minions.
        /// </summary>
        public static float GetLeashDistanceTiles(int projectileType, int minionSlotPosition = 0)
        {
            if (MinionRangeTable.TryGetValue(projectileType, out MinionRangeInfo info)) {
                return info.GetEffectiveLeashDistance(minionSlotPosition);
            }
            return DefaultLeashDistanceTiles;
        }

        /// <summary>
        /// Get the vanilla leash distance for a minion in PIXELS.
        /// </summary>
        public static float GetLeashDistancePixels(int projectileType, int minionSlotPosition = 0)
        {
            return GetLeashDistanceTiles(projectileType, minionSlotPosition) * 16f;
        }

        /// <summary>
        /// Get the enemy detection range for a minion in tiles.
        /// Returns default value for unknown minions.
        /// </summary>
        public static float GetDetectionRangeTiles(int projectileType, int minionSlotPosition = 0, bool playerTargeted = false)
        {
            if (MinionRangeTable.TryGetValue(projectileType, out MinionRangeInfo info)) {
                return info.GetEffectiveDetectionRange(minionSlotPosition, playerTargeted);
            }
            return DefaultDetectionRangeTiles;
        }

        /// <summary>
        /// Check if a minion has a known vanilla leash behavior.
        /// Minions without vanilla leash (ground minions, phase-through) return false.
        /// </summary>
        public static bool HasVanillaLeash(int projectileType)
        {
            if (MinionRangeTable.TryGetValue(projectileType, out MinionRangeInfo info)) {
                return info.LeashDistanceTiles > 0;
            }
            return true;  // Assume unknown minions have some leash behavior
        }

        /// <summary>
        /// Check if a projectile type is a known minion in our table.
        /// </summary>
        public static bool IsKnownMinion(int projectileType)
        {
            return MinionRangeTable.ContainsKey(projectileType);
        }

        /// <summary>
        /// Get count of known minions in the table.
        /// </summary>
        public static int KnownMinionCount => MinionRangeTable.Count;
    }
}