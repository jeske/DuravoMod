using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace TerrariaSurvivalMod
{
    /// <summary>
    /// Client-side configuration for the Terraria Survival Mod.
    /// Access via ModContent.GetInstance<TerrariaSurvivalModConfig>().
    /// </summary>
    public class TerrariaSurvivalModConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          DEBUG OPTIONS                             ║
        // ╚════════════════════════════════════════════════════════════════════╝

        [Header("Debug")]
        
        [Label("Enable Debug Messages")]
        [Tooltip("Show detailed debug messages in chat (immunity status, damage blocking, etc.)")]
        [DefaultValue(false)]
        public bool EnableDebugMessages { get; set; }
    }
}