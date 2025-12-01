// MIT Licensed - Copyright (c) 2025 David W. Jeske
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using System;

namespace DuravoQOLMod.ArmorRebalance
{
    /// <summary>
    /// Handles the Emergency Shield mechanic for Copper/Tin and Gold/Platinum armor.
    /// When player takes damage while wearing shield-tier armor and shield is off cooldown,
    /// a shield activates that absorbs incoming damage.
    /// </summary>
    public class EmergencyShieldPlayer : ModPlayer
    {
        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                        TUNABLE CONSTANTS                           ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>DEBUG: Reads from mod config - enables verbose shield activation logging</summary>
        private static bool DebugShieldActivation => ModContent.GetInstance<DuravoQOLModConfig>()?.Debug?.DebugArmorShields ?? false;

        /// <summary>Cooldown in seconds for Copper/Tin tier shield</summary>
        private const int CopperTinShieldCooldownSeconds = 60;

        /// <summary>Cooldown in seconds for Gold/Platinum tier shield</summary>
        private const int GoldPlatinumShieldCooldownSeconds = 120;

        /// <summary>Shield HP for Copper/Tin tier (flat amount)</summary>
        private const int CopperTinShieldHP = 30;

        /// <summary>Shield duration in seconds for Copper/Tin tier</summary>
        private const int CopperTinShieldDurationSeconds = 5;

        /// <summary>Shield HP percentage for Gold/Platinum tier (15% of max HP)</summary>
        private const float GoldPlatinumShieldHPPercent = 0.15f;

        /// <summary>Shield duration in seconds for Gold/Platinum tier</summary>
        private const int GoldPlatinumShieldDurationSeconds = 10;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          INSTANCE STATE                            ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Remaining shield HP that absorbs damage</summary>
        private int shieldCurrentHP;

        /// <summary>Maximum shield HP when activated (for visual ratio)</summary>
        private int shieldMaxHP;

        /// <summary>Remaining duration in ticks (60 ticks = 1 second)</summary>
        private int shieldDurationRemainingTicks;

        // NOTE: Cooldown is tracked via FragileBuff on the player, NOT an internal timer.
        // This prevents exploits like armor-swapping to reset cooldown.

        /// <summary>Whether the current shield is Gold/Platinum tier (affects color)</summary>
        private bool isGoldTierShield;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          PUBLIC PROPERTIES                         ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Check if emergency shield is currently active and has HP remaining.</summary>
        public bool HasActiveShield => shieldCurrentHP > 0 && shieldDurationRemainingTicks > 0;

        /// <summary>Current shield HP for display purposes.</summary>
        public int CurrentShieldHP => shieldCurrentHP;

        /// <summary>Ratio of current shield HP to maximum (for visual fill).</summary>
        public float ShieldRatio => shieldMaxHP > 0 ? (float)shieldCurrentHP / shieldMaxHP : 0f;

        /// <summary>Whether the current shield is from Gold/Platinum tier (affects color).</summary>
        public bool IsGoldTierShield => isGoldTierShield;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          ARMOR DETECTION                           ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Shield armor tiers</summary>
        public enum ShieldArmorTier
        {
            None,
            CopperTin,      // 30 HP flat, 5s duration, 60s cooldown
            GoldPlatinum    // 15% max HP, 10s duration, 120s cooldown, purges debuffs
        }

        /// <summary>
        /// Detect which shield-tier chestplate the player is wearing.
        /// Copper/Silver provide 30HP flat shield, Gold/Platinum provide 15% HP shield.
        /// Tin provides speed bonus instead (handled in ArmorSetBonusPlayer).
        /// </summary>
        private ShieldArmorTier DetectShieldArmorTier()
        {
            Item chestplate = Player.armor[1];

            if (chestplate == null || chestplate.IsAir)
                return ShieldArmorTier.None;

            int chestplateType = chestplate.type;

            // Copper/Silver tier (30HP flat shield)
            if (chestplateType == ItemID.CopperChainmail ||
                chestplateType == ItemID.SilverChainmail)
                return ShieldArmorTier.CopperTin;

            // Gold/Platinum tier (15% HP shield)
            if (chestplateType == ItemID.GoldChainmail || chestplateType == ItemID.PlatinumChainmail)
                return ShieldArmorTier.GoldPlatinum;

            return ShieldArmorTier.None;
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          TICK UPDATES                              ║
        // ╚════════════════════════════════════════════════════════════════════╝

        public override void PostUpdate()
        {
            // Tick down shield duration
            if (shieldDurationRemainingTicks > 0) {
                shieldDurationRemainingTicks--;
                if (shieldDurationRemainingTicks == 0) {
                    // Shield expired
                    shieldCurrentHP = 0;
                    if (DebugShieldActivation)
                        Main.NewText("[SHIELD] Shield expired (duration ran out)", Color.Gray);
                }
            }

            // Cooldown is tracked via FragileBuff, not internal timer
            // This prevents armor-swap exploits
        }

        /// <summary>
        /// Check if shield is on cooldown by checking for FragileBuff.
        /// Using the buff system prevents armor-swap exploits.
        /// </summary>
        private bool IsShieldOnCooldown()
        {
            return Player.HasBuff(ModContent.BuffType<FragileBuff>());
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                    DAMAGE PROCESSING (SHIELD)                      ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Process incoming damage from external source (called by GlobalNPC.ModifyHitPlayer).
        /// This is the correct entry point for NPC-to-player damage interception.
        /// </summary>
        /// <param name="modifiers">The damage modifiers to adjust</param>
        /// <param name="incomingDamage">The raw damage amount</param>
        /// <param name="damageSource">Description of damage source for debug logging</param>
        public void ProcessIncomingDamage(ref Player.HurtModifiers modifiers, int incomingDamage, string damageSource)
        {
            // Check if feature is enabled (static accessor)
            if (!DuravoQOLModConfig.EnableArmorRebalance)
                return;

            if (DebugShieldActivation) {
                Main.NewText($"[SHIELD] ProcessIncomingDamage: {incomingDamage} from {damageSource}", Color.Yellow);
            }

            if (incomingDamage <= 0) {
                if (DebugShieldActivation)
                    Main.NewText("[SHIELD] ABORT: Incoming damage <= 0", Color.Red);
                return;
            }

            // Step 1: Detect armor tier
            ShieldArmorTier armorTier = DetectShieldArmorTier();

            if (DebugShieldActivation) {
                bool onCooldown = IsShieldOnCooldown();
                Main.NewText($"[SHIELD] Armor tier: {armorTier}, HasActiveShield: {HasActiveShield}, OnCooldown: {onCooldown}", Color.Yellow);
            }

            // Step 2: If no active shield, try to create one
            if (!HasActiveShield) {
                // Check cooldown via FragileBuff (prevents armor-swap exploit)
                if (IsShieldOnCooldown()) {
                    if (DebugShieldActivation) {
                        int cooldownRemaining = Player.buffTime[Player.FindBuffIndex(ModContent.BuffType<FragileBuff>())];
                        Main.NewText($"[SHIELD] ABORT: On cooldown (Fragile buff: {cooldownRemaining / 60}s remaining)", Color.Orange);
                    }
                    return;
                }

                // Check armor tier
                if (armorTier == ShieldArmorTier.None) {
                    if (DebugShieldActivation)
                        Main.NewText("[SHIELD] ABORT: Not wearing shield-tier armor", Color.Orange);
                    return;
                }

                // Create shield based on tier
                CreateShield(armorTier);
            }

            // Step 3: Absorb damage if we have a shield
            if (HasActiveShield) {
                AbsorbDamageWithShield(ref modifiers, incomingDamage);
            }
            else {
                if (DebugShieldActivation)
                    Main.NewText("[SHIELD] No active shield after creation attempt - damage NOT absorbed", Color.Red);
            }
        }

        /// <summary>
        /// Create a new shield based on armor tier.
        /// Cooldown is tracked via FragileBuff to prevent armor-swap exploits.
        /// </summary>
        private void CreateShield(ShieldArmorTier armorTier)
        {
            if (DebugShieldActivation)
                Main.NewText($"[SHIELD] Creating shield for tier: {armorTier}", Color.Cyan);

            if (armorTier == ShieldArmorTier.CopperTin) {
                shieldMaxHP = CopperTinShieldHP;
                shieldDurationRemainingTicks = CopperTinShieldDurationSeconds * 60;
                isGoldTierShield = false;

                // Add Fragile debuff for cooldown tracking (prevents armor-swap exploit)
                Player.AddBuff(ModContent.BuffType<FragileBuff>(), CopperTinShieldCooldownSeconds * 60);
            }
            else // GoldPlatinum
            {
                shieldMaxHP = (int)(Player.statLifeMax2 * GoldPlatinumShieldHPPercent);
                shieldDurationRemainingTicks = GoldPlatinumShieldDurationSeconds * 60;
                isGoldTierShield = true;

                // Add Fragile debuff for cooldown tracking (prevents armor-swap exploit)
                Player.AddBuff(ModContent.BuffType<FragileBuff>(), GoldPlatinumShieldCooldownSeconds * 60);

                // Gold tier purges debuffs (but NOT Fragile)
                PurgeCommonDebuffs();
            }

            shieldCurrentHP = shieldMaxHP;

            if (DebugShieldActivation)
                Main.NewText($"[SHIELD] Shield created: {shieldCurrentHP} HP, {shieldDurationRemainingTicks / 60}s duration", Color.Cyan);

            // Visual feedback - burst of particles
            SpawnShieldActivationParticles();
        }

        /// <summary>
        /// Absorb damage with the active shield.
        /// </summary>
        private void AbsorbDamageWithShield(ref Player.HurtModifiers modifiers, int incomingDamage)
        {
            int absorbedDamage = Math.Min(shieldCurrentHP, incomingDamage);
            shieldCurrentHP -= absorbedDamage;

            // Calculate remaining percentage for display
            int remainingPercent = shieldMaxHP > 0
                ? (int)(100f * shieldCurrentHP / shieldMaxHP)
                : 0;

            if (DebugShieldActivation)
                Main.NewText($"[SHIELD] Absorbing {absorbedDamage} damage, shield now {shieldCurrentHP}/{shieldMaxHP} ({remainingPercent}%)", Color.Lime);

            // Negate the damage
            if (absorbedDamage >= incomingDamage) {
                modifiers.FinalDamage *= 0f; // Completely negate
                if (DebugShieldActivation)
                    Main.NewText("[SHIELD] Damage fully negated!", Color.Lime);
            }
            else {
                // Partial absorption (shield broke mid-hit)
                modifiers.SourceDamage.Base -= absorbedDamage;
                if (DebugShieldActivation)
                    Main.NewText($"[SHIELD] Partial absorption, remaining damage: {modifiers.SourceDamage.Base}", Color.Yellow);
            }

            // Show combat text - use localized string
            string blockedText = Language.GetTextValue("Mods.DuravoQOLMod.ArmorRebalance.CombatText.ShieldBlocked", absorbedDamage);
            CombatText.NewText(Player.Hitbox, Color.Cyan, blockedText);

            // Check if shield broke
            if (shieldCurrentHP <= 0) {
                if (DebugShieldActivation)
                    Main.NewText("[SHIELD] Shield broke!", Color.Orange);
            }
        }

        /// <summary>
        /// Spawn particle burst when shield activates.
        /// </summary>
        private void SpawnShieldActivationParticles()
        {
            for (int i = 0; i < 30; i++) {
                float angle = Main.rand.NextFloat() * MathHelper.TwoPi;
                Vector2 velocity = new Vector2(
                    (float)Math.Cos(angle) * 3f,
                    (float)Math.Sin(angle) * 3f
                );
                Dust.NewDust(Player.Center, 0, 0, DustID.MagicMirror, velocity.X, velocity.Y, Alpha: 100, Scale: 1.2f);
            }
        }

        /// <summary>
        /// Clear common negative debuffs (gold/platinum set bonus).
        /// </summary>
        private void PurgeCommonDebuffs()
        {
            Player.ClearBuff(BuffID.OnFire);
            Player.ClearBuff(BuffID.OnFire3); // Hellfire
            Player.ClearBuff(BuffID.Poisoned);
            Player.ClearBuff(BuffID.Venom);
            Player.ClearBuff(BuffID.Chilled);
            Player.ClearBuff(BuffID.Frozen);
            Player.ClearBuff(BuffID.Burning);
            Player.ClearBuff(BuffID.Bleeding);
            Player.ClearBuff(BuffID.Confused);
            Player.ClearBuff(BuffID.Slow);
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          VISUAL EFFECTS                            ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Draw shield dust particles around the player when shield is active.
        /// </summary>
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (!HasActiveShield)
                return;

            // Create dust particles around player for shield effect
            if (Main.rand.NextBool(3)) // ~20 particles per second
            {
                float angle = Main.rand.NextFloat() * MathHelper.TwoPi;
                float dustRadius = 10f;
                Vector2 dustPosition = Player.Center + new Vector2(
                    (float)Math.Cos(angle) * dustRadius,
                    (float)Math.Sin(angle) * dustRadius
                );

                Dust shieldDust = Dust.NewDustPerfect(
                    dustPosition,
                    DustID.MagicMirror,
                    Vector2.Zero,
                    Alpha: 100,
                    Scale: 0.8f
                );
                shieldDust.noGravity = true;
            }
        }
    }
}