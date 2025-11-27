// MIT Licensed - Copyright (c) 2025 David W. Jeske
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CrucibleMod.ArmorRebalance
{
    /// <summary>
    /// GlobalNPC that intercepts NPC contact damage TO the player to apply shield absorption.
    /// This is the correct place to modify incoming NPC damage - NOT ModPlayer.ModifyHurt.
    /// </summary>
    public class ShieldDamageNPCInterceptor : GlobalNPC
    {
        /// <summary>DEBUG: Reads from mod config - enables verbose shield activation logging</summary>
        private static bool DebugShieldActivation => ModContent.GetInstance<CrucibleModConfig>()?.Debug?.DebugArmorShields ?? false;

        /// <summary>
        /// Called when an NPC is about to hit a player. This is where we intercept and reduce damage.
        /// </summary>
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            // Get our shield player instance
            EmergencyShieldPlayer shieldPlayer = target.GetModPlayer<EmergencyShieldPlayer>();

            // Get incoming damage (NPC's base contact damage)
            int incomingDamage = npc.damage;

            if (DebugShieldActivation) {
                Main.NewText($"[SHIELD] NPC {npc.FullName} hitting player! NPC damage: {incomingDamage}", Color.Yellow);
            }

            // Delegate to the shield player for processing
            shieldPlayer.ProcessIncomingDamage(ref modifiers, incomingDamage, $"NPC: {npc.FullName}");
        }
    }

    /// <summary>
    /// GlobalProjectile that intercepts projectile damage TO the player to apply shield absorption.
    /// Works in tandem with ShieldDamageNPCInterceptor to block all entity-based damage sources.
    /// </summary>
    public class ShieldDamageProjectileInterceptor : GlobalProjectile
    {
        /// <summary>DEBUG: Reads from mod config - enables verbose shield activation logging</summary>
        private static bool DebugShieldActivation => ModContent.GetInstance<CrucibleModConfig>()?.Debug?.DebugArmorShields ?? false;

        /// <summary>
        /// Called when a hostile projectile is about to hit a player. This is where we intercept projectile damage.
        /// </summary>
        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
            // Only process hostile projectiles
            if (!projectile.hostile)
                return;

            // Get our shield player instance
            EmergencyShieldPlayer shieldPlayer = target.GetModPlayer<EmergencyShieldPlayer>();

            // Get incoming damage from projectile
            int incomingDamage = projectile.damage;

            if (DebugShieldActivation) {
                Main.NewText($"[SHIELD] Projectile {projectile.Name} hitting player! Damage: {incomingDamage}", Color.Yellow);
            }

            // Delegate to the shield player for processing
            shieldPlayer.ProcessIncomingDamage(ref modifiers, incomingDamage, $"Projectile: {projectile.Name}");
        }
    }
}