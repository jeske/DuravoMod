// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DuravoQOLMod
{
    /// <summary>
    /// Client-side configuration for the DuravoQOLMod.
    /// Access via ModContent.GetInstance<DuravoQOLModConfig>() for full config,
    /// or use static properties for hot-path checks (e.g., EnableArmorRebalance).
    /// </summary>
    public class DuravoQOLModConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                STATIC ACCESSORS (ALWAYS USE THESE!)                ║
        // ╚════════════════════════════════════════════════════════════════════╝
        //
        // USAGE PATTERN: Always use static Enable* properties when checking if a feature is enabled.
        //
        //   ✓ CORRECT:   if (!DuravoQOLModConfig.EnableArmorRebalance) return;
        //   ✗ WRONG:     if (!ModContent.GetInstance<DuravoQOLModConfig>().ArmorRebalance) return;
        //
        // WHY: Static properties provide consistent access and avoid repeated GetInstance() calls
        // in hot paths (per-tick methods like PreAI, UpdateEquips, etc.). The null-coalescing
        // pattern also provides safe defaults if config hasn't loaded yet.
        //

        /// <summary>Check if Armor Rebalance feature is enabled. ALWAYS use this static property.</summary>
        public static bool EnableArmorRebalance => ModContent.GetInstance<DuravoQOLModConfig>()?.ArmorRebalance ?? true;

        /// <summary>Check if Enemy Smart Hopping feature is enabled. ALWAYS use this static property.</summary>
        public static bool EnableEnemySmartHopping => ModContent.GetInstance<DuravoQOLModConfig>()?.EnemySmartHopping ?? true;

        /// <summary>Check if Client Persistent Position is enabled (stores in player file). ALWAYS use this static property.</summary>
        public static bool EnableClientPersistentPosition => ModContent.GetInstance<DuravoQOLModConfig>()?.ClientPersistentPosition ?? true;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      FEATURE TOGGLES (TOP LEVEL)                    ║
        // ╚════════════════════════════════════════════════════════════════════╝

        [Header("FeatureToggles")]
        [DefaultValue(true)]
        public bool ClientPersistentPosition { get; set; } = true;

        [DefaultValue(true)]
        public bool ArmorRebalance { get; set; } = true;

        [DefaultValue(true)]
        public bool EnemySmartHopping { get; set; } = true;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      MINION BEHAVIOR                                ║
        // ╚════════════════════════════════════════════════════════════════════╝

        [Header("MinionBehavior")]
        [DefaultValue(true)]
        public bool MinionSmartPathfinding { get; set; } = true;

        [DefaultValue(true)]
        public bool MinionIsolatedReturn { get; set; } = true;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      DEBUG (SEPARATE PAGE)                          ║
        // ╚════════════════════════════════════════════════════════════════════╝

        [SeparatePage]
        public DebugSettings Debug { get; set; } = new();
    }

    /// <summary>
    /// Debug and development settings.
    /// </summary>
    public class DebugSettings
    {
        [DefaultValue(false)]
        public bool DebugPlayerPersistence { get; set; }

        [DefaultValue(false)]
        public bool DebugMinionPathfinding { get; set; }

        [DefaultValue(false)]
        public bool DebugArmorShields { get; set; }
    }
}