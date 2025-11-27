using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TerrariaSurvivalMod.Players;

namespace TerrariaSurvivalMod.NPCs
{
    /// <summary>
    /// GlobalNPC that intercepts NPC damage TO the player to apply shield absorption.
    /// This is the correct place to modify incoming damage - NOT ModPlayer.ModifyHurt.
    /// </summary>
    public class ShieldDamageInterceptor : GlobalNPC
    {
        /// <summary>DEBUG: Set to true for verbose shield activation logging</summary>
        private const bool DebugShieldActivation = true;

        /// <summary>
        /// Called when an NPC is about to hit a player. This is where we intercept and reduce damage.
        /// </summary>
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            // Get our shield player instance
            EmergencyShieldPlayer shieldPlayer = target.GetModPlayer<EmergencyShieldPlayer>();
            
            // Get incoming damage (this should have the actual value now!)
            int incomingDamage = npc.damage; // NPC's base contact damage
            
            if (DebugShieldActivation) {
                Main.NewText($"[SHIELD] NPC {npc.FullName} hitting player! NPC damage: {incomingDamage}", Color.Yellow);
            }
            
            // Delegate to the shield player for processing
            shieldPlayer.ProcessIncomingDamage(ref modifiers, incomingDamage, $"NPC: {npc.FullName}");
        }
    }
}