// MIT Licensed - Copyright (c) 2025 David W. Jeske
using Terraria;
using Terraria.ModLoader;

namespace DuravoQOLMod.ArmorRebalance
{
    /// <summary>
    /// Visual indicator that the player cannot acquire an emergency shield temporarily.
    /// This is purely informational - the actual cooldown is tracked in ArmorSetBonusPlayer.
    /// Uses vanilla Buff_63 texture.
    /// </summary>
    public class FragileBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_63";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;           // Count as a debuff
            Main.buffNoSave[Type] = true;       // Don't save to player
            Main.buffNoTimeDisplay[Type] = false; // Show time remaining
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // This buff is purely visual - no gameplay effect
            // The actual shield cooldown is tracked in ArmorSetBonusPlayer.emergencyShieldCooldownTicks
        }
    }
}