// MIT Licensed - Copyright (c) 2025 David W. Jeske
using Terraria;
using Terraria.ModLoader;

namespace DuravoQOLMod.ArmorRebalance
{
    /// <summary>
    /// Buff that grants nearby ore highlighting while wearing 2+ Shiny-tagged armor pieces.
    /// The actual ore glow effect is handled by ArmorSetBonusPlayer.
    /// </summary>
    public class ShinyBuff : ModBuff
    {
        // Use custom icon from Assets/ShinyBuff.png
        public override string Texture => "DuravoQOLMod/Assets/ShinyBuff";

        public override void SetStaticDefaults()
        {
            // Buff description shown in tooltip
            // DisplayName defaults to "Shiny" from class name
            Main.buffNoTimeDisplay[Type] = true;  // Don't show duration countdown
            Main.debuff[Type] = false;            // This is a positive buff
            Main.pvpBuff[Type] = false;           // Not relevant for PvP
            Main.buffNoSave[Type] = true;         // Don't persist on logout
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Mark player as having the shiny effect active
            // ArmorSetBonusPlayer checks for this buff to apply ore glow
            player.GetModPlayer<ArmorRebalance.ArmorSetBonusPlayer>().HasShinyBuff = true;
        }
    }
}