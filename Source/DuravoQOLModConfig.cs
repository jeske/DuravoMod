// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DuravoQOLMod
{
    /// <summary>
    /// Client-side configuration for the DuravoQOLMod.
    /// Use cached static booleans (Enable*) for hot-path checks.
    /// Values are cached on startup and updated via OnChanged().
    /// </summary>
    public class DuravoQOLModConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                CACHED STATIC BOOLEANS (USE THESE!)                 ║
        // ╚════════════════════════════════════════════════════════════════════╝
        //
        // USAGE PATTERN: Use these cached static booleans in hot paths (render, update, etc.)
        //
        //   ✓ CORRECT:   if (!DuravoQOLModConfig.EnableArmorRebalance) return;
        //   ✗ WRONG:     if (!ModContent.GetInstance<DuravoQOLModConfig>().ArmorRebalance) return;
        //
        // WHY: Cached booleans avoid GetInstance() calls every frame.
        // Values are updated automatically when config changes via OnChanged().
        //

        /// <summary>Cached: Check if Armor Rebalance feature is enabled.</summary>
        public static bool EnableArmorRebalance = true;

        /// <summary>Cached: Check if Enemy Smart Hopping feature is enabled.</summary>
        public static bool EnableEnemySmartHopping = false;

        /// <summary>Cached: Check if Client Persistent Position is enabled.</summary>
        public static bool EnableClientPersistentPosition = true;

        /// <summary>Cached: Check if Crafting Panel feature is enabled (master toggle).</summary>
        public static bool EnableCraftingPanel = true;

        /// <summary>Cached: Check if Crafting Panel should only show seen items.</summary>
        public static bool EnableCraftingPanelOnlyShowSeenItems = true;

        /// <summary>Cached: Check if auto-show at benches toggle should be respected.</summary>
        public static bool EnableCraftingPanelStickyAutoShow = true;

        /// <summary>Cached: Check if Minion Smart Pathfinding is enabled.</summary>
        public static bool EnableMinionSmartPathfinding = true;

        /// <summary>Cached: Check if Minion Isolated Return is enabled.</summary>
        public static bool EnableMinionIsolatedReturn = true;

        /// <summary>Cached: Debug - Player Persistence.</summary>
        public static bool DebugEnablePlayerPersistence = false;

        /// <summary>Cached: Debug - Minion Pathfinding.</summary>
        public static bool DebugEnableMinionPathfinding = false;

        /// <summary>Cached: Debug - Armor Shields.</summary>
        public static bool DebugEnableArmorShields = false;

        /// <summary>Cached: Debug - Enemy Smart Hopping.</summary>
        public static bool DebugEnableEnemySmartHopping = false;

        /// <summary>
        /// Called when config values change (including on initial load).
        /// Updates all cached static booleans.
        /// </summary>
        public override void OnChanged()
        {
            // Feature toggles
            EnableArmorRebalance = ArmorRebalance;
            EnableEnemySmartHopping = EnemySmartHopping;
            EnableClientPersistentPosition = ClientPersistentPosition;
            
            // Crafting panel
            EnableCraftingPanel = CraftingPanelEnabled;
            EnableCraftingPanelOnlyShowSeenItems = CraftingPanelOnlyShowSeenItems;
            EnableCraftingPanelStickyAutoShow = CraftingPanelStickyAutoShowAtBenches;
            
            // Minion behavior
            EnableMinionSmartPathfinding = MinionSmartPathfinding;
            EnableMinionIsolatedReturn = MinionIsolatedReturn;
            
            // Debug settings
            DebugEnablePlayerPersistence = Debug.DebugPlayerPersistence;
            DebugEnableMinionPathfinding = Debug.DebugMinionPathfinding;
            DebugEnableArmorShields = Debug.DebugArmorShields;
            DebugEnableEnemySmartHopping = Debug.DebugEnemySmartHopping;
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      FEATURE TOGGLES (TOP LEVEL)                    ║
        // ╚════════════════════════════════════════════════════════════════════╝

        [Header("FeatureToggles")]
        [DefaultValue(true)]
        public bool ClientPersistentPosition { get; set; } = true;

        [DefaultValue(true)]
        public bool ArmorRebalance { get; set; } = true;

        [DefaultValue(false)]
        public bool EnemySmartHopping { get; set; } = false;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      CRAFTING PANEL                                 ║
        // ╚════════════════════════════════════════════════════════════════════╝

        [Header("CraftingPanel")]
        [DefaultValue(true)]
        [Tooltip("Enable the Duravo Crafting Panel feature.\nWhen disabled, the panel button and all panel features are hidden.")]
        public bool CraftingPanelEnabled { get; set; } = true;

        [DefaultValue(true)]
        [Tooltip("When enabled, the auto-show at benches toggle is respected.\nWhen disabled, the panel never auto-opens at crafting stations.")]
        public bool CraftingPanelStickyAutoShowAtBenches { get; set; } = true;

        [DefaultValue(true)]
        [Tooltip("Only show craftable items in the Crafting Panel if you've seen at least one recipe ingredient.\nPreserves the discovery experience as you progress.")]
        public bool CraftingPanelOnlyShowSeenItems { get; set; } = true;

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

        [DefaultValue(false)]
        public bool DebugEnemySmartHopping { get; set; }
    }
}