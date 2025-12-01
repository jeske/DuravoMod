// MIT Licensed - Copyright (c) 2025 David W. Jeske
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using DuravoQOLMod.ArmorRebalance;

namespace DuravoQOLMod.MiniPlayerHealthbar
{
    /// <summary>
    /// Tracks player health state for the mini healthbar display.
    /// Monitors recent damage to show the "yellow drain" animation effect.
    /// </summary>
    public class MiniHealthbarPlayer : ModPlayer
    {
        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                        CONSTANTS & CONFIG                          ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>When health drops below this value (4 hearts), always show the healthbar (fallback threshold)</summary>
        private const int HealthAbsoluteThreshold = 80; // 4 hearts

        /// <summary>How long (in ticks) the yellow "damage taken" bar takes to drain</summary>
        private const int DamageAnimationDurationTicks = 60; // 1 second

        // Config-driven values (accessed via cached static properties):
        // - DuravoQOLModConfig.MiniHealthbarShowAtHealthPercent (0.0 to 1.0)
        // - DuravoQOLModConfig.MiniHealthbarShowOnDamagePercent (0.0 to 1.0)
        // - DuravoQOLModConfig.MiniHealthbarAutoHideSeconds (int seconds)

        /// <summary>Get the linger time in ticks from config (seconds * 60)</summary>
        private static int HealthbarLingerTicks => (int)(DuravoQOLModConfig.MiniHealthbarAutoHideSeconds * 60f);

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          INSTANCE STATE                            ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>The health value displayed by the yellow bar (trails actual health)</summary>
        private int displayedHealthForYellowBar;

        /// <summary>How many ticks until the yellow bar catches up to current health</summary>
        private int damageAnimationTicksRemaining;

        /// <summary>How much total damage the yellow bar needs to animate away</summary>
        private int damageToAnimateAway;

        /// <summary>Health at the start of the animation (for interpolation)</summary>
        private int healthAtAnimationStart;

        /// <summary>How many ticks the healthbar should remain visible</summary>
        private int healthbarVisibleTicks;

        /// <summary>Track previous health to detect damage</summary>
        private int previousHealth;

        /// <summary>Accumulated damage in recent window for threshold check</summary>
        private int recentDamageAccumulator;

        /// <summary>Ticks until recent damage accumulator resets</summary>
        private int recentDamageResetTicks;

        /// <summary>How long to accumulate damage before the window resets (1 second @ 60fps)</summary>
        private const int RecentDamageWindowTicks = 60; // 1 second

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          PUBLIC PROPERTIES                         ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Whether the mini healthbar should currently be displayed</summary>
        public bool ShouldShowHealthbar => DuravoQOLModConfig.EnableMiniHealthbar
            && (DuravoQOLModConfig.EnableMiniHealthbarAlwaysOn || healthbarVisibleTicks > 0 || CheckShowConditions());

        /// <summary>Current health as a ratio (0.0 to 1.0)</summary>
        public float CurrentHealthRatio => Player.statLifeMax2 > 0
            ? MathHelper.Clamp((float)Player.statLife / Player.statLifeMax2, 0f, 1f)
            : 0f;

        /// <summary>Yellow bar health as a ratio (trails actual health for damage animation)</summary>
        public float YellowBarHealthRatio => Player.statLifeMax2 > 0
            ? MathHelper.Clamp((float)displayedHealthForYellowBar / Player.statLifeMax2, 0f, 1f)
            : 0f;

        /// <summary>Get shield info from armor system if available</summary>
        public (bool hasShield, int shieldHP, int maxShieldHP, bool isGoldTier) GetShieldInfo()
        {
            var shieldPlayer = Player.GetModPlayer<EmergencyShieldPlayer>();
            if (shieldPlayer != null && shieldPlayer.HasActiveShield) {
                // Calculate max shield HP based on tier
                // CopperTin = 30HP flat, GoldPlatinum = 15% of max health
                int maxShieldHP = shieldPlayer.IsGoldTierShield
                    ? (int)(Player.statLifeMax2 * 0.15f)
                    : 30;
                return (true, shieldPlayer.CurrentShieldHP, maxShieldHP, shieldPlayer.IsGoldTierShield);
            }
            return (false, 0, 0, false);
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          INITIALIZATION                            ║
        // ╚════════════════════════════════════════════════════════════════════╝

        public override void Initialize()
        {
            displayedHealthForYellowBar = 0;
            damageAnimationTicksRemaining = 0;
            damageToAnimateAway = 0;
            healthAtAnimationStart = 0;
            healthbarVisibleTicks = 0;
            previousHealth = 0;
            recentDamageAccumulator = 0;
            recentDamageResetTicks = 0;
        }

        public override void OnEnterWorld()
        {
            // Initialize to current health so yellow bar starts correct
            displayedHealthForYellowBar = Player.statLife;
            previousHealth = Player.statLife;
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          TICK UPDATES                              ║
        // ╚════════════════════════════════════════════════════════════════════╝

        public override void PostUpdate()
        {
            UpdateDamageTracking();
            UpdateDamageAnimation();
            UpdateVisibility();
        }

        /// <summary>
        /// Track damage taken this frame and accumulate for threshold checks.
        /// </summary>
        private void UpdateDamageTracking()
        {
            int currentHealth = Player.statLife;
            int damageTaken = previousHealth - currentHealth;

            // Detect damage (positive change means health went down)
            if (damageTaken > 0) {
                // Accumulate recent damage
                recentDamageAccumulator += damageTaken;
                recentDamageResetTicks = RecentDamageWindowTicks;

                // Start/extend damage animation
                StartDamageAnimation(damageTaken);

                // Show healthbar
                healthbarVisibleTicks = HealthbarLingerTicks;
            }

            // Detect healing - update yellow bar immediately for heals
            if (damageTaken < 0) {
                // Health increased - snap yellow bar to current if it would be below
                if (displayedHealthForYellowBar < currentHealth) {
                    displayedHealthForYellowBar = currentHealth;
                }
            }

            previousHealth = currentHealth;

            // Decay recent damage accumulator
            if (recentDamageResetTicks > 0) {
                recentDamageResetTicks--;
                if (recentDamageResetTicks == 0) {
                    recentDamageAccumulator = 0;
                }
            }
        }

        /// <summary>
        /// Start or extend the yellow bar drain animation.
        /// </summary>
        private void StartDamageAnimation(int newDamage)
        {
            // If animation already running, just add to remaining damage
            if (damageAnimationTicksRemaining > 0) {
                // Keep current displayedHealthForYellowBar, extend animation
                damageToAnimateAway = displayedHealthForYellowBar - Player.statLife;
            }
            else {
                // Start fresh animation
                healthAtAnimationStart = displayedHealthForYellowBar;
                damageToAnimateAway = newDamage;
            }

            damageAnimationTicksRemaining = DamageAnimationDurationTicks;
        }

        /// <summary>
        /// Animate the yellow bar draining down to current health.
        /// </summary>
        private void UpdateDamageAnimation()
        {
            if (damageAnimationTicksRemaining > 0) {
                damageAnimationTicksRemaining--;

                // Calculate interpolation progress (1.0 at start, 0.0 at end)
                float progress = (float)damageAnimationTicksRemaining / DamageAnimationDurationTicks;

                // Ease-out function for smooth deceleration
                float easedProgress = progress * progress; // Quadratic ease-out (inverted)

                // Interpolate yellow bar from animation start to current health
                int targetHealth = Player.statLife;
                displayedHealthForYellowBar = targetHealth + (int)(damageToAnimateAway * easedProgress);

                // Clamp to valid range
                displayedHealthForYellowBar = (int)MathHelper.Clamp(
                    displayedHealthForYellowBar,
                    targetHealth,
                    Player.statLifeMax2
                );
            }
            else {
                // Animation complete - snap to current health
                displayedHealthForYellowBar = Player.statLife;
            }
        }

        /// <summary>
        /// Update healthbar visibility timer.
        /// </summary>
        private void UpdateVisibility()
        {
            // Check if conditions warrant showing
            if (CheckShowConditions()) {
                healthbarVisibleTicks = HealthbarLingerTicks;
            }

            // Decay visibility timer
            if (healthbarVisibleTicks > 0) {
                healthbarVisibleTicks--;
            }
        }

        /// <summary>
        /// Check if any condition for showing the healthbar is met.
        /// </summary>
        private bool CheckShowConditions()
        {
            // Condition 1: Health below percentage threshold (from config slider)
            float healthThreshold = DuravoQOLModConfig.MiniHealthbarShowAtHealthPercent;
            if (healthThreshold > 0 && CurrentHealthRatio < healthThreshold) {
                return true;
            }

            // Condition 2: Health below absolute threshold (4 hearts - hardcoded fallback)
            if (Player.statLife < HealthAbsoluteThreshold) {
                return true;
            }

            // Condition 3: Recent damage exceeds threshold (from config slider)
            float damageThreshold = DuravoQOLModConfig.MiniHealthbarShowOnDamagePercent;
            if (damageThreshold > 0) {
                float recentDamagePercent = Player.statLifeMax2 > 0
                    ? (float)recentDamageAccumulator / Player.statLifeMax2
                    : 0f;
                if (recentDamagePercent >= damageThreshold) {
                    return true;
                }
            }

            // Condition 4: Shield is active
            var shieldPlayer = Player.GetModPlayer<EmergencyShieldPlayer>();
            if (shieldPlayer != null && shieldPlayer.HasActiveShield) {
                return true;
            }

            return false;
        }
    }
}