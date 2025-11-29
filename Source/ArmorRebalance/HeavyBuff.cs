// MIT Licensed - Copyright (c) 2025 David W. Jeske
using Terraria;
using Terraria.ModLoader;

namespace DuravoQOLMod.ArmorRebalance
{
    /// <summary>
    /// Buff that indicates Heavy chestplate effect is active.
    /// Grants +15% knockback dealt (effect applied by ArmorSetBonusPlayer).
    /// </summary>
    public class HeavyBuff : ModBuff
    {
        // Use vanilla Buff_115 icon (Titan Potion - knockback buff)
        public override string Texture => "Terraria/Images/Buff_115";

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;  // Don't show duration countdown
            Main.debuff[Type] = false;            // This is a positive buff
            Main.pvpBuff[Type] = false;           // Not relevant for PvP
            Main.buffNoSave[Type] = true;         // Don't persist on logout
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Mark player as having the Heavy effect active
            player.GetModPlayer<ArmorSetBonusPlayer>().HasHeavyBuff = true;
        }
    }
}