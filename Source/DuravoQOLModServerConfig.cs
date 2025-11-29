// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DuravoQOLMod
{
    /// <summary>
    /// Server-side (per-world) configuration for the DuravoQOLMod.
    /// These settings are stored in the world file and controlled by the server/host.
    /// In single-player, you control these. In multiplayer, the server host controls them.
    /// </summary>
    public class DuravoQOLModServerConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                STATIC ACCESSORS (ALWAYS USE THESE!)                ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Check if World/Server Persistent Position is enabled. ALWAYS use this static property.</summary>
        public static bool EnableWorldServerPersistentPosition => ModContent.GetInstance<DuravoQOLModServerConfig>()?.WorldServerPersistentPosition ?? false;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      SERVER/WORLD FEATURE TOGGLES                   ║
        // ╚════════════════════════════════════════════════════════════════════╝
        //
        // NOTE: WorldServerPersistentPosition is EXPERIMENTAL and defaults to OFF.
        // Issues:
        //   1. World can change while player is offline (blocks placed where they stood)
        //   2. Server needs "bump out of solid" logic for safety (not yet implemented)
        //   3. Full server authority requires more networking code
        //

        [Header("WorldFeatures")]
        [DefaultValue(false)]
        [ReloadRequired]
        public bool WorldServerPersistentPosition { get; set; } = false;
    }
}