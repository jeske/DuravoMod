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

        /// <summary>Cached: Check if Mini Healthbar feature is enabled (master toggle).</summary>
        public static bool EnableMiniHealthbar = true;

        /// <summary>Cached: Check if Mini Healthbar should always show (bypass auto-hide conditions).</summary>
        public static bool EnableMiniHealthbarAlwaysOn = false;

        /// <summary>Cached: Health % threshold to show healthbar (0.0 to 1.0)</summary>
        public static float MiniHealthbarShowAtHealthPercent = 0.50f;

        /// <summary>Cached: Damage % threshold to trigger healthbar (0.0 to 1.0)</summary>
        public static float MiniHealthbarShowOnDamagePercent = 0.10f;

        /// <summary>Cached: Auto-hide time in seconds (float for 0.25s precision)</summary>
        public static float MiniHealthbarAutoHideSeconds = 3f;

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

            // Mini healthbar
            EnableMiniHealthbar = MiniHealthbarEnabled;
            EnableMiniHealthbarAlwaysOn = MiniHealthbarAlwaysOn;
            MiniHealthbarShowAtHealthPercent = MiniHealthbarHealthThreshold / 100f;
            MiniHealthbarShowOnDamagePercent = MiniHealthbarDamageThreshold / 100f;
            MiniHealthbarAutoHideSeconds = MiniHealthbarLingerTime;

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
        // ║                      MINI HEALTHBAR                                 ║
        // ╚════════════════════════════════════════════════════════════════════╝

        [Header("MiniHealthbar")]
        [DefaultValue(true)]
        [Tooltip("Enable the mini healthbar that appears below your character during combat.\nShows health, recent damage, and shield status at a glance.")]
        public bool MiniHealthbarEnabled { get; set; } = true;

        [DefaultValue(false)]
        [Tooltip("Always show the mini healthbar, even when at full health.\nBy default, it only appears when health is low or taking damage.")]
        public bool MiniHealthbarAlwaysOn { get; set; } = false;

        [Range(0, 100)]
        [DefaultValue(50)]
        [Slider]
        [Tooltip("Show healthbar when health drops below this percentage.\n0% = never show based on health, 100% = always show when not full.")]
        public int MiniHealthbarHealthThreshold { get; set; } = 50;

        [Range(0, 100)]
        [DefaultValue(25)]
        [Slider]
        [Tooltip("Show healthbar temporarily when taking damage exceeding this percentage of max health.\n0% = any damage triggers it, 100% = only massive hits.")]
        public int MiniHealthbarDamageThreshold { get; set; } = 25;

        [Range(1f, 20f)]
        [DefaultValue(3f)]
        [Slider]
        [Increment(0.25f)]
        [Tooltip("How many seconds the healthbar stays visible after being triggered.\nLonger values keep it visible between damage events.")]
        public float MiniHealthbarLingerTime { get; set; } = 3f;

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