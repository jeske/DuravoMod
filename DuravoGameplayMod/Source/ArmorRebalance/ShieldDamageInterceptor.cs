// MIT Licensed - Copyright (c) 2025 David W. Jeske
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DuravoQOLMod.ArmorRebalance
{
    /// <summary>
    /// GlobalNPC that intercepts NPC contact damage TO the player to apply shield absorption
    /// and Heavy buff knockback effects.
    /// This is the correct place to modify incoming NPC damage - NOT ModPlayer.ModifyHurt.
    /// </summary>
    public class ShieldDamageNPCInterceptor : GlobalNPC
    {
        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                        HEAVY BUFF CONSTANTS                        ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Knockback resistance granted by Heavy buff (15% reduction)</summary>
        private const float HeavyKnockbackResistance = 0.15f;

        /// <summary>Knockback reflection multiplier (15% of what player would take)</summary>
        private const float HeavyKnockbackReflection = 0.15f;

        /// <summary>Base knockback force for contact damage reflection (pixels per tick)</summary>
        private const float BaseContactKnockbackForce = 8f;

        /// <summary>DEBUG: Reads from mod config - enables verbose shield activation logging</summary>
        private static bool DebugShieldActivation => ModContent.GetInstance<DuravoQOLModConfig>()?.Debug?.DebugArmorShields ?? false;

        /// <summary>
        /// Called when an NPC is about to hit a player. This is where we intercept and reduce damage.
        /// Also handles Heavy buff knockback effects.
        /// </summary>
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            // ═══════════════════════════════════════════════════════════
            // HEAVY BUFF: Knockback resistance and reflection
            // ═══════════════════════════════════════════════════════════
            ArmorSetBonusPlayer armorBonusPlayer = target.GetModPlayer<ArmorSetBonusPlayer>();
            if (armorBonusPlayer.HasHeavyBuff) {
                // -15% knockback taken
                modifiers.Knockback *= (1f - HeavyKnockbackResistance);

                // Reflect 15% of contact knockback to enemy
                // Only apply to NPCs that can be knocked back 
                // npc.knockBackResist really means knockBackSUCCEPTABILITY  (aka it's multiplicative)
                if (npc.knockBackResist > 0f && !npc.boss) {
                    ApplyKnockbackReflection(npc, target);
                }

                if (DebugShieldActivation) {
                    Main.NewText($"[HEAVY] Knockback reduced by {HeavyKnockbackResistance * 100}%, reflected {HeavyKnockbackReflection * 100}% to {npc.FullName}", Color.Cyan);
                }
            }

            // ═══════════════════════════════════════════════════════════
            // SHIELD: Damage absorption
            // ═══════════════════════════════════════════════════════════
            EmergencyShieldPlayer shieldPlayer = target.GetModPlayer<EmergencyShieldPlayer>();

            // Get incoming damage (NPC's base contact damage)
            int incomingDamage = npc.damage;

            if (DebugShieldActivation) {
                Main.NewText($"[SHIELD] NPC {npc.FullName} hitting player! NPC damage: {incomingDamage}", Color.Yellow);
            }

            // Delegate to the shield player for processing
            shieldPlayer.ProcessIncomingDamage(ref modifiers, incomingDamage, $"NPC: {npc.FullName}");
        }

        /// <summary>
        /// Apply knockback to the NPC (reflection from Heavy buff).
        /// Pushes the enemy away from the player.
        /// </summary>
        private static void ApplyKnockbackReflection(NPC enemyNPC, Player targetPlayer)
        {
            // Direction: from player toward enemy (pushes enemy away)
            Vector2 knockbackDirection = enemyNPC.Center - targetPlayer.Center;
            if (knockbackDirection.Length() > 0) {
                knockbackDirection.Normalize();
            }
            else {
                // Fallback if exactly overlapping
                knockbackDirection = new Vector2(targetPlayer.direction, 0);
            }

            // Calculate knockback force
            // NOTE: npc.knockBackResist is actually a MULTIPLIER (1.0 = full knockback, 0.0 = immune)
            // Despite its name, it means "how much knockback this NPC receives", not "how much it resists"
            float npcKnockbackSusceptibility = enemyNPC.knockBackResist;  // 0-1 scale
            float reflectedKnockback = BaseContactKnockbackForce * HeavyKnockbackReflection * npcKnockbackSusceptibility;

            // Apply velocity change to NPC
            enemyNPC.velocity += knockbackDirection * reflectedKnockback;
        }
    }

    /// <summary>
    /// GlobalProjectile that intercepts projectile damage TO the player to apply shield absorption.
    /// Works in tandem with ShieldDamageNPCInterceptor to block all entity-based damage sources.
    /// </summary>
    public class ShieldDamageProjectileInterceptor : GlobalProjectile
    {
        /// <summary>DEBUG: Reads from mod config - enables verbose shield activation logging</summary>
        private static bool DebugShieldActivation => ModContent.GetInstance<DuravoQOLModConfig>()?.Debug?.DebugArmorShields ?? false;

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