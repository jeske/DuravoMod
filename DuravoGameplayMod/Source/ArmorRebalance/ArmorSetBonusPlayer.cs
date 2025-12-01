// MIT Licensed - Copyright (c) 2025 David W. Jeske
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace DuravoQOLMod.ArmorRebalance
{
    /// <summary>
    /// Handles armor set bonuses for crit chance, movement speed, and the Shiny sparkle effect.
    /// Shield mechanic is handled separately by EmergencyShieldPlayer.
    /// </summary>
    public class ArmorSetBonusPlayer : ModPlayer
    {
        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                        TUNABLE CONSTANTS                           ║
        // ╚════════════════════════════════════════════════════════════════════╝

        // --- Sparkle Timing (tick-based: 60 ticks = 1 second) ---
        /// <summary>Duration in ticks for sparkle fade-out (normal)</summary>
        private const int SparkleFadeDurationTicks = 60; // 1 second

        /// <summary>Duration in ticks for FAST sparkle fade-out (when buff ends)</summary>
        private const int SparkleFastDecayDurationTicks = 12; // 0.2 seconds

        /// <summary>Fixed cooldown in ticks after sparkle finishes before it can trigger again</summary>
        private const int SparkleSpawnCooldownTicks = 300; // 5 seconds

        /// <summary>Maximum random pre-activation delay in ticks (staggers when sparkles appear)</summary>
        private const int SparklePreActivateMaxDelayTicks = SparkleSpawnCooldownTicks;

        // --- Ore Detection ---
        /// <summary>Range in tiles for mini-spelunker ore glow effect</summary>
        private const int OreDetectionRangeTiles = 8;

        /// <summary>Range in tiles over which sparkle intensity fades from 100% to 0% (beyond detection range)</summary>
        private const int SparkleFalloffRangeTiles = 2;

        // --- Darkness Glow ---
        /// <summary>Brightness threshold below which the "Shiny glow" activates (0.0 = total darkness, 1.0 = full light)</summary>
        private const float DarknessThresholdForGlow = 0.15f;

        /// <summary>Intensity of the emergency glow when in darkness (very dim - just enough to see player)</summary>
        private const float ShinyDarkGlowIntensity = 0.08f;

        // --- Debug Flags (set to false before release) ---
        /// <summary>DEBUG FLAG: Set to true to force Shiny effect active regardless of armor</summary>
        private const bool DebugForceShinyActive = false; // DEBUG: Set to false for release

        /// <summary>DEBUG FLAG: Set to true to also highlight stone (for easier testing)</summary>
        private const bool DebugHighlightStone = false;

        /// <summary>DEBUG FLAG: Set to true to also highlight Lead ore (bypasses tileSpelunker check)</summary>
        private const bool DebugHighlightLeadOre = false;

        /// <summary>DEBUG FLAG: Set to true to show persistent dim red sparkles at ALL sparkle point locations</summary>
        private const bool DebugShowSparkleLocations = false;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          INSTANCE STATE                            ║
        // ╚════════════════════════════════════════════════════════════════════╝

        // === MULTI-PIECE BUFF TRACKING ===
        /// <summary>Number of equipped armor pieces contributing to Shiny tag (includes Super Shiny)</summary>
        private int shinyPieceCount = 0;

        /// <summary>Number of equipped armor pieces contributing to Super Shiny tag</summary>
        private int superShinyPieceCount = 0;

        /// <summary>Whether the current chestplate grants the Heavy effect</summary>
        private bool hasHeavyChestplate = false;

        /// <summary>Multi-piece buff tags that armor can contribute (for Shiny ore detection only)</summary>
        public enum ArmorTag
        {
            None,
            Shiny,      // Copper, Tin, Silver - 2pc = ore sparkle (base range)
            SuperShiny  // Gold, Platinum - 2pc = ore sparkle (far range), counts as Shiny
        }

        // === SPARKLE TRACKING ===
        /// <summary>Active sparkles being rendered with manual fade control</summary>
        private static readonly System.Collections.Generic.List<ActiveSparkle> activeSparkles = new System.Collections.Generic.List<ActiveSparkle>();


        /// <summary>Tracks an active sparkle with spawn time for fade calculation</summary>
        private struct ActiveSparkle
        {
            public Vector2 WorldPosition;
            public uint QueuedTick;
            public int PreActivateDelayTicks;
            public Color SparkleColor;
            public float InitialScale;

            public uint BecomeVisibleTick => QueuedTick + (uint)PreActivateDelayTicks;
        }

        /// <summary>Tracks all known sparkle point positions for debug rendering</summary>
        private static readonly System.Collections.Generic.HashSet<Vector2> debugSparklePositions = new System.Collections.Generic.HashSet<Vector2>();

        // === BUFF STATE ===
        /// <summary>Set by ShinyBuff.Update() when the buff is active</summary>
        public bool HasShinyBuff { get; set; }

        /// <summary>Set by HeavyBuff.Update() when the buff is active</summary>
        public bool HasHeavyBuff { get; set; }

        /// <summary>Static flag for DrawSparkles to check if ANY player has the buff active</summary>
        private static bool anyPlayerHasShinyBuff = false;

        /// <summary>Game tick when the buff was last active (for fast decay calculation)</summary>
        private static uint lastBuffActiveTick = 0;

        public override void ResetEffects()
        {
            shinyPieceCount = 0;
            superShinyPieceCount = 0;
            hasHeavyChestplate = false;
            HasShinyBuff = false;
            HasHeavyBuff = false;
        }

        public override void UpdateEquips()
        {
            // Check if feature is enabled (static accessor for hot-path)
            if (!DuravoQOLModConfig.EnableArmorRebalance)
                return;

            CountArmorTags();
            ApplyChestplateEffects();

            // Calculate effective Shiny count (Super Shiny counts as Shiny)
            int effectiveShinyCount = shinyPieceCount + superShinyPieceCount;

            // Apply Shiny buff when 2+ Shiny-tagged pieces equipped
            if (DebugForceShinyActive || effectiveShinyCount >= 2) {
                Player.AddBuff(ModContent.BuffType<ShinyBuff>(), 2);
            }

            // Apply Heavy buff when Heavy chestplate is equipped
            if (hasHeavyChestplate) {
                Player.AddBuff(ModContent.BuffType<HeavyBuff>(), 2);
            }
        }

        /// <summary>
        /// Apply chestplate-specific effects based on equipped armor.
        /// Each chestplate gets exactly ONE bonus.
        /// </summary>
        private void ApplyChestplateEffects()
        {
            int chestplateType = Player.armor[1].type;

            switch (chestplateType) {
                // Tin: +15% movement speed
                case ItemID.TinChainmail:
                    Player.moveSpeed += 0.15f;
                    break;

                // Heavy: +15% knockback dealt
                case ItemID.IronChainmail:
                case ItemID.LeadChainmail:
                case ItemID.TungstenChainmail:
                    Player.GetKnockback(DamageClass.Generic) += 0.15f;
                    hasHeavyChestplate = true;
                    break;

                    // Shields: Handled by EmergencyShieldPlayer
                    // case ItemID.CopperChainmail:
                    // case ItemID.SilverChainmail:
                    // case ItemID.GoldChainmail:
                    // case ItemID.PlatinumChainmail:
            }
        }

        public override void PostUpdateBuffs()
        {
            bool buffActive = DebugForceShinyActive || HasShinyBuff;

            // Update static buff tracking (for fast decay in DrawSparkles)
            if (buffActive) {
                anyPlayerHasShinyBuff = true;
                lastBuffActiveTick = Main.GameUpdateCount;

                SpawnNewSparkles();
                ApplyShinyDarknessGlow();
            }
            else {
                anyPlayerHasShinyBuff = false;
            }

            // Always call cleanup (handles fast decay when buff ends)
            UpdateAndCleanupSparkles();
        }

        private void ApplyShinyDarknessGlow()
        {
            int playerTileX = (int)(Player.Center.X / 16f);
            int playerTileY = (int)(Player.Center.Y / 16f);

            float currentBrightness = Lighting.Brightness(playerTileX, playerTileY);

            if (currentBrightness < DarknessThresholdForGlow) {
                Lighting.AddLight(
                    Player.Center,
                    ShinyDarkGlowIntensity * 1.0f,
                    ShinyDarkGlowIntensity * 0.8f,
                    ShinyDarkGlowIntensity * 0.5f
                );
            }
        }

        private void SpawnNewSparkles()
        {
            int playerTileX = (int)(Player.Center.X / 16f);
            int playerTileY = (int)(Player.Center.Y / 16f);
            int tilesFound = 0;

            for (int scanTileX = playerTileX - OreDetectionRangeTiles; scanTileX <= playerTileX + OreDetectionRangeTiles; scanTileX++) {
                for (int scanTileY = playerTileY - OreDetectionRangeTiles; scanTileY <= playerTileY + OreDetectionRangeTiles; scanTileY++) {
                    if (!WorldGen.InWorld(scanTileX, scanTileY, 1))
                        continue;

                    Tile scannedTile = Main.tile[scanTileX, scanTileY];

                    bool isSpelunkerTile = Main.tileSpelunker[scannedTile.TileType];
                    bool isStoneForTesting = DebugHighlightStone && scannedTile.TileType == TileID.Stone;
                    bool isLeadForTesting = DebugHighlightLeadOre && scannedTile.TileType == TileID.Lead;

                    if (scannedTile.HasTile && (isSpelunkerTile || isStoneForTesting || isLeadForTesting)) {
                        tilesFound++;
                        TrySpawnSparkleForTile(scanTileX, scanTileY, scannedTile.TileType);
                    }
                }
            }

            if (DebugShowSparkleLocations && Main.GameUpdateCount % 120 == 0) {
                Main.NewText($"[SPARKLE DEBUG] Tiles found: {tilesFound}, Debug positions: {debugSparklePositions.Count}, Active sparkles: {activeSparkles.Count}", 255, 255, 0);
            }
        }

        private static void UpdateAndCleanupSparkles()
        {
            uint currentTick = Main.GameUpdateCount;

            // Use fast decay duration if buff is not active
            int effectiveFadeDurationTicks = anyPlayerHasShinyBuff
                ? SparkleFadeDurationTicks
                : SparkleFastDecayDurationTicks;

            for (int i = activeSparkles.Count - 1; i >= 0; i--) {
                ActiveSparkle sparkle = activeSparkles[i];
                uint visibleTick = sparkle.BecomeVisibleTick;
                int ageAfterVisibleTicks = (int)(currentTick - visibleTick);

                // When buff ends, calculate age relative to when buff ended for fast decay
                if (!anyPlayerHasShinyBuff && lastBuffActiveTick > visibleTick) {
                    // Sparkle was created while buff was active, now buff ended
                    // Treat age as time since buff ended + fast decay offset
                    int ticksSinceBuffEnded = (int)(currentTick - lastBuffActiveTick);
                    if (ticksSinceBuffEnded > SparkleFastDecayDurationTicks) {
                        activeSparkles.RemoveAt(i);
                        continue;
                    }
                }
                else if (ageAfterVisibleTicks > effectiveFadeDurationTicks) {
                    activeSparkles.RemoveAt(i);
                }
            }
        }

        /// <summary>Tracks last spawn tick per sparkle point for cooldown</summary>
        private static readonly System.Collections.Generic.Dictionary<int, uint> sparkleLastSpawnTicks = new System.Collections.Generic.Dictionary<int, uint>();

        private void TrySpawnSparkleForTile(int tileX, int tileY, int tileType)
        {
            int tileCoordinateHash = HashTileCoordinates(tileX, tileY);
            int dustType = GetDustTypeForTile(tileType);
            uint currentTick = Main.GameUpdateCount;

            for (int sparkleIndex = 0; sparkleIndex < 2; sparkleIndex++) {
                int sparklePointKey = tileCoordinateHash + sparkleIndex * 31337;

                Vector2 sparkleOffsetWithinTile = GetDeterministicSparkleOffset(tileCoordinateHash, sparkleIndex);
                Vector2 sparkleWorldPosition = new Vector2(tileX * 16, tileY * 16) + sparkleOffsetWithinTile;

                if (DebugShowSparkleLocations) {
                    debugSparklePositions.Add(sparkleWorldPosition);
                }

                uint lastSpawnTick = 0;
                sparkleLastSpawnTicks.TryGetValue(sparklePointKey, out lastSpawnTick);

                Random preActivateRandom = new Random(sparklePointKey);
                int preActivateDelayTicks = (int)(preActivateRandom.NextDouble() * SparklePreActivateMaxDelayTicks);

                int ticksSinceLastVisible = (int)(currentTick - lastSpawnTick);
                bool cooldownExpired = ticksSinceLastVisible >= (SparkleFadeDurationTicks + SparkleSpawnCooldownTicks);

                if (cooldownExpired) {
                    uint becomeVisibleTick = currentTick + (uint)preActivateDelayTicks;
                    sparkleLastSpawnTicks[sparklePointKey] = becomeVisibleTick;

                    Color sparkleColor = GetColorForOre(dustType);
                    const float FixedSparkleScale = 1.0f;

                    activeSparkles.Add(new ActiveSparkle {
                        WorldPosition = sparkleWorldPosition,
                        QueuedTick = currentTick,
                        PreActivateDelayTicks = preActivateDelayTicks,
                        SparkleColor = sparkleColor,
                        InitialScale = FixedSparkleScale
                    });
                }
            }
        }

        public static void DrawSparkles(SpriteBatch spriteBatch)
        {
            uint currentTick = Main.GameUpdateCount;

            Texture2D sparkleTexture = TextureAssets.Star[0].Value;
            Vector2 textureOrigin = new Vector2(sparkleTexture.Width / 2f, sparkleTexture.Height / 2f);

            if (DebugShowSparkleLocations) {
                foreach (Vector2 debugPos in debugSparklePositions) {
                    Vector2 debugScreenPos = debugPos - Main.screenPosition;

                    if (debugScreenPos.X > -50 && debugScreenPos.X < Main.screenWidth + 50 &&
                        debugScreenPos.Y > -50 && debugScreenPos.Y < Main.screenHeight + 50) {
                        spriteBatch.Draw(
                            sparkleTexture,
                            debugScreenPos,
                            null,
                            new Color(255, 50, 50, 200),
                            0f,
                            textureOrigin,
                            0.25f,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }

            if (activeSparkles.Count == 0) return;

            Vector2 localPlayerCenter = Main.LocalPlayer.Center;

            foreach (ActiveSparkle sparkle in activeSparkles) {
                uint visibleTick = sparkle.BecomeVisibleTick;
                int ageAfterVisibleTicks = (int)(currentTick - visibleTick);

                if (ageAfterVisibleTicks < 0) continue;

                // Calculate fade based on whether buff is active
                int effectiveFadeDurationTicks = anyPlayerHasShinyBuff
                    ? SparkleFadeDurationTicks
                    : SparkleFastDecayDurationTicks;

                // When buff ends, use time since buff ended for fade calculation
                float fadeProgress;
                if (!anyPlayerHasShinyBuff && lastBuffActiveTick > visibleTick) {
                    int ticksSinceBuffEnded = (int)(currentTick - lastBuffActiveTick);
                    fadeProgress = (float)ticksSinceBuffEnded / SparkleFastDecayDurationTicks;
                }
                else {
                    fadeProgress = (float)ageAfterVisibleTicks / effectiveFadeDurationTicks;
                }

                if (fadeProgress > 1f) continue;

                float distanceToPlayerTiles = Vector2.Distance(sparkle.WorldPosition, localPlayerCenter) / 16f;
                float distanceFalloffFactor;
                if (distanceToPlayerTiles <= OreDetectionRangeTiles) {
                    distanceFalloffFactor = 1.0f;
                }
                else if (distanceToPlayerTiles <= OreDetectionRangeTiles + SparkleFalloffRangeTiles) {
                    float falloffProgress = (distanceToPlayerTiles - OreDetectionRangeTiles) / SparkleFalloffRangeTiles;
                    distanceFalloffFactor = 1.0f - falloffProgress;
                }
                else {
                    continue;
                }

                float fadeMultiplier = 1f - fadeProgress;

                Color fadedColor = sparkle.SparkleColor * fadeMultiplier * distanceFalloffFactor;
                float currentScale = sparkle.InitialScale * (0.5f + fadeMultiplier * 0.5f) * 0.3f;

                Vector2 screenPosition = sparkle.WorldPosition - Main.screenPosition;

                spriteBatch.Draw(
                    sparkleTexture,
                    screenPosition,
                    null,
                    fadedColor,
                    0f,
                    textureOrigin,
                    currentScale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        public static void ClearDebugSparklePositions()
        {
            debugSparklePositions.Clear();
        }

        private static Color GetColorForOre(int dustType)
        {
            switch (dustType) {
                case 9: return new Color(255, 140, 50);    // Copper
                case 11: return new Color(200, 200, 200); // Tin/Gray
                case 8: return new Color(180, 180, 180);  // Iron
                case 14: return new Color(180, 200, 255); // Lead - brighter blue-silver
                case 63: return new Color(255, 255, 255); // Silver
                case 15: return new Color(100, 180, 255); // Cobalt/Sapphire
                case 6: return new Color(255, 150, 60);   // Fire/Palladium
                case 72: return new Color(255, 120, 220); // Orichalcum/Amethyst
                case 60: return new Color(255, 100, 100); // Adamantite/Ruby
                case 75: return new Color(100, 255, 100); // Chlorophyte/Emerald
                case 169: return new Color(255, 220, 80); // Topaz/Amber
                case 27: return new Color(180, 100, 220); // Meteorite
                default: return new Color(255, 230, 120); // Gold
            }
        }

        private static int GetDustTypeForTile(int tileType)
        {
            switch (tileType) {
                case TileID.Copper: return 9;
                case TileID.Tin: return 11;
                case TileID.Iron: return 8;
                case TileID.Lead: return 14;
                case TileID.Silver: return 63;
                case TileID.Tungsten: return 11;
                case TileID.Gold: return DustID.GoldFlame;
                case TileID.Platinum: return 63;
                case TileID.Cobalt: return 15;
                case TileID.Palladium: return 6;
                case TileID.Mythril: return 15;
                case TileID.Orichalcum: return 72;
                case TileID.Adamantite: return 60;
                case TileID.Titanium: return 11;
                case TileID.Chlorophyte: return 75;
                case TileID.Sapphire: return 15;
                case TileID.Ruby: return 60;
                case TileID.Emerald: return 75;
                case TileID.Topaz: return 169;
                case TileID.Amethyst: return 72;
                case TileID.Diamond: return 63;
                case TileID.AmberStoneBlock: return 169;
                case TileID.Hellstone: return 6;
                case TileID.Meteorite: return 27;
                default: return DustID.GoldFlame;
            }
        }

        private static int HashTileCoordinates(int tileX, int tileY)
        {
            return tileX * 31337 + tileY * 7919;
        }

        private static Vector2 GetDeterministicSparkleOffset(int tileHash, int sparkleIndex)
        {
            int seed = tileHash + sparkleIndex * 12345;
            float offsetX = ((seed & 0xF) + 0.5f);
            float offsetY = (((seed >> 4) & 0xF) + 0.5f);
            return new Vector2(offsetX, offsetY);
        }

        /// <summary>
        /// Count how many armor pieces contribute to each Shiny tag.
        /// Checks helmet (slot 0), chestplate (slot 1), and greaves (slot 2).
        /// Note: Iron/Lead/Tungsten are NOT part of the Shiny system - they use Heavy chestplate effect only.
        /// </summary>
        private void CountArmorTags()
        {
            shinyPieceCount = 0;
            superShinyPieceCount = 0;

            // Check all 3 armor slots
            for (int armorSlot = 0; armorSlot < 3; armorSlot++) {
                ArmorTag tag = GetArmorTagForItem(Player.armor[armorSlot].type);
                switch (tag) {
                    case ArmorTag.Shiny:
                        shinyPieceCount++;
                        break;
                    case ArmorTag.SuperShiny:
                        superShinyPieceCount++;
                        break;
                }
            }
        }

        /// <summary>
        /// Get the Shiny armor tag for a given item type.
        /// Only Shiny-system armors return a tag; Iron/Lead/Tungsten return None (Heavy is chestplate-only).
        /// </summary>
        public static ArmorTag GetArmorTagForItem(int itemType)
        {
            // Shiny: Copper, Tin, Silver
            switch (itemType) {
                // Copper armor
                case ItemID.CopperHelmet:
                case ItemID.CopperChainmail:
                case ItemID.CopperGreaves:
                // Tin armor
                case ItemID.TinHelmet:
                case ItemID.TinChainmail:
                case ItemID.TinGreaves:
                // Silver armor
                case ItemID.SilverHelmet:
                case ItemID.SilverChainmail:
                case ItemID.SilverGreaves:
                    return ArmorTag.Shiny;

                // Super Shiny: Gold, Platinum
                case ItemID.GoldHelmet:
                case ItemID.GoldChainmail:
                case ItemID.GoldGreaves:
                case ItemID.PlatinumHelmet:
                case ItemID.PlatinumChainmail:
                case ItemID.PlatinumGreaves:
                    return ArmorTag.SuperShiny;

                // Iron/Lead/Tungsten don't participate in Shiny system
                // Heavy effect is chestplate-only, not a 2pc buff
                default:
                    return ArmorTag.None;
            }
        }

        /// <summary>
        /// Get the current piece counts and Heavy status for tooltip display.
        /// Heavy is a chestplate-only effect, not a multi-piece buff.
        /// </summary>
        public (int shiny, int superShiny, bool hasHeavy) GetCurrentBuffStatus()
        {
            return (shinyPieceCount, superShinyPieceCount, hasHeavyChestplate);
        }
    }
}