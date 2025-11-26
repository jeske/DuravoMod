using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaSurvivalMod.Buffs
{
    /// <summary>
    /// Buff that grants nearby ore highlighting while wearing full ore armor set.
    /// The actual ore glow effect is handled by ArmorSetBonusPlayer.
    /// </summary>
    public class ShinyBuff : ModBuff
    {
        // Use vanilla Buff_72 icon (shiny sparkle effect)
        public override string Texture => "Terraria/Images/Buff_72";

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
            player.GetModPlayer<Players.ArmorSetBonusPlayer>().HasShinyBuff = true;
        }
    }
}