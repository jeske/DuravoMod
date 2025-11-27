// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace CrucibleMod
{
    /// <summary>
    /// Client-side configuration for the Terraria Survival Mod.
    /// Access via ModContent.GetInstance<TerrariaSurvivalModConfig>().
    /// </summary>
    public class CrucibleModConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      MINION BEHAVIOR (TOP LEVEL)                    ║
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