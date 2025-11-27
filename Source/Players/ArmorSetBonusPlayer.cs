using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace TerrariaSurvivalMod.Players
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

        // --- Sparkle Timing ---
        /// <summary>Duration in seconds for sparkle fade-out</summary>
        private const double SparkleFadeDurationSeconds = 1.0;

        /// <summary>Fixed cooldown in seconds after sparkle finishes before it can trigger again</summary>
        private const double SparkleSpawnCooldownSeconds = 5.0;

        /// <summary>Maximum random pre-activation delay (staggers when sparkles appear)</summary>
        private const double SparklePreActivateMaxDelaySeconds = SparkleSpawnCooldownSeconds;

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
        private const bool DebugForceShinyActive = true; // DEBUG: Set to false for release

        /// <summary>DEBUG FLAG: Set to true to also highlight stone (for easier testing)</summary>
        private const bool DebugHighlightStone = false;

        /// <summary>DEBUG FLAG: Set to true to also highlight Lead ore (bypasses tileSpelunker check)</summary>
        private const bool DebugHighlightLeadOre = true;

        /// <summary>DEBUG FLAG: Set to true to show persistent dim red sparkles at ALL sparkle point locations</summary>
        private const bool DebugShowSparkleLocations = false;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          INSTANCE STATE                            ║
        // ╚════════════════════════════════════════════════════════════════════╝

        // === SET BONUS TRACKING ===
        /// <summary>Current chestplate tier for single-piece bonuses</summary>
        private ChestplateTier currentChestplateTier = ChestplateTier.None;

        /// <summary>Whether player is wearing a full ore armor set (any tier)</summary>
        private bool hasFullOreArmorSet = false;

        /// <summary>Chestplate tiers for single-piece bonuses</summary>
        public enum ChestplateTier
        {
            None,
            TinCopper,      // Emergency Shield (handled by EmergencyShieldPlayer)
            IronLead,       // +10% crit chance
            SilverTungsten, // +15% move speed
            GoldPlatinum    // Emergency Shield (handled by EmergencyShieldPlayer)
        }

        // === SPARKLE TRACKING ===
        /// <summary>Active sparkles being rendered with manual fade control</summary>
        private static readonly System.Collections.Generic.List<ActiveSparkle> activeSparkles = new System.Collections.Generic.List<ActiveSparkle>();

        /// <summary>Tracks last spawn time per sparkle point for cooldown</summary>
        private static readonly System.Collections.Generic.Dictionary<int, double> sparkleLastSpawnTimes = new System.Collections.Generic.Dictionary<int, double>();

        /// <summary>Tracks an active sparkle with spawn time for fade calculation</summary>
        private struct ActiveSparkle
        {
            public Vector2 WorldPosition;
            public double QueuedTimeEpoch;
            public double PreActivateDelaySeconds;
            public Color SparkleColor;
            public float InitialScale;

            public double BecomeVisibleTimeEpoch => QueuedTimeEpoch + PreActivateDelaySeconds;
        }

        /// <summary>Get current time as epoch seconds (since 1970, never wraps)</summary>
        private static double GetEpochTimeSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        }

        /// <summary>Tracks all known sparkle point positions for debug rendering</summary>
        private static readonly System.Collections.Generic.HashSet<Vector2> debugSparklePositions = new System.Collections.Generic.HashSet<Vector2>();

        // === BUFF STATE ===
        /// <summary>Set by ShinyBuff.Update() when the buff is active</summary>
        public bool HasShinyBuff { get; set; }

        public override void ResetEffects()
        {
            currentChestplateTier = ChestplateTier.None;
            hasFullOreArmorSet = false;
            HasShinyBuff = false;
        }

        public override void UpdateEquips()
        {
            currentChestplateTier = DetectChestplateTier();
            hasFullOreArmorSet = DetectFullOreArmorSet();

            // Apply CHESTPLATE bonuses (tier-specific utility)
            switch (currentChestplateTier) {
                case ChestplateTier.IronLead:
                    // +10% crit chance from Iron/Lead chestplate
                    Player.GetCritChance(DamageClass.Generic) += 10f;
                    break;

                case ChestplateTier.SilverTungsten:
                    // +15% movement speed from Silver/Tungsten chestplate
                    Player.moveSpeed += 0.15f;
                    break;

                    // Shield bonuses (TinCopper, GoldPlatinum) handled by EmergencyShieldPlayer
            }

            // Apply Shiny buff when wearing full ore armor set
            if (DebugForceShinyActive || hasFullOreArmorSet) {
                Player.AddBuff(ModContent.BuffType<Buffs.ShinyBuff>(), 2);
            }
        }

        public override void PostUpdateBuffs()
        {
            if (DebugForceShinyActive || HasShinyBuff) {
                SpawnNewSparkles();
                UpdateAndCleanupSparkles();
                ApplyShinyDarknessGlow();
            }
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
            double currentTimeEpoch = GetEpochTimeSeconds();

            for (int i = activeSparkles.Count - 1; i >= 0; i--) {
                ActiveSparkle sparkle = activeSparkles[i];
                double visibleTimeEpoch = sparkle.BecomeVisibleTimeEpoch;
                double ageAfterVisible = currentTimeEpoch - visibleTimeEpoch;

                if (ageAfterVisible > SparkleFadeDurationSeconds) {
                    activeSparkles.RemoveAt(i);
                }
            }
        }

        private void TrySpawnSparkleForTile(int tileX, int tileY, int tileType)
        {
            int tileCoordinateHash = HashTileCoordinates(tileX, tileY);
            int dustType = GetDustTypeForTile(tileType);
            double currentTimeEpoch = GetEpochTimeSeconds();

            for (int sparkleIndex = 0; sparkleIndex < 2; sparkleIndex++) {
                int sparklePointKey = tileCoordinateHash + sparkleIndex * 31337;

                Vector2 sparkleOffsetWithinTile = GetDeterministicSparkleOffset(tileCoordinateHash, sparkleIndex);
                Vector2 sparkleWorldPosition = new Vector2(tileX * 16, tileY * 16) + sparkleOffsetWithinTile;

                if (DebugShowSparkleLocations) {
                    debugSparklePositions.Add(sparkleWorldPosition);
                }

                double lastSpawnTime = 0.0;
                sparkleLastSpawnTimes.TryGetValue(sparklePointKey, out lastSpawnTime);

                Random preActivateRandom = new Random(sparklePointKey);
                double preActivateDelaySeconds = preActivateRandom.NextDouble() * SparklePreActivateMaxDelaySeconds;

                double timeSinceLastVisible = currentTimeEpoch - lastSpawnTime;
                bool cooldownExpired = timeSinceLastVisible >= (SparkleFadeDurationSeconds + SparkleSpawnCooldownSeconds);

                if (cooldownExpired) {
                    double becomeVisibleTimeEpoch = currentTimeEpoch + preActivateDelaySeconds;
                    sparkleLastSpawnTimes[sparklePointKey] = becomeVisibleTimeEpoch;

                    Color sparkleColor = GetColorForOre(dustType);
                    const float FixedSparkleScale = 1.0f;

                    activeSparkles.Add(new ActiveSparkle {
                        WorldPosition = sparkleWorldPosition,
                        QueuedTimeEpoch = currentTimeEpoch,
                        PreActivateDelaySeconds = preActivateDelaySeconds,
                        SparkleColor = sparkleColor,
                        InitialScale = FixedSparkleScale
                    });
                }
            }
        }

        public static void DrawSparkles(SpriteBatch spriteBatch)
        {
            double currentTimeEpoch = GetEpochTimeSeconds();

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
                double visibleTimeEpoch = sparkle.BecomeVisibleTimeEpoch;
                double ageAfterVisible = currentTimeEpoch - visibleTimeEpoch;

                if (ageAfterVisible < 0) continue;
                if (ageAfterVisible > SparkleFadeDurationSeconds) continue;

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

                float fadeProgress = (float)(ageAfterVisible / SparkleFadeDurationSeconds);
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
        /// Detect which tier of ore chestplate the player is wearing.
        /// Only checks the chestplate slot - helmet and legs can be mixed.
        /// </summary>
        private ChestplateTier DetectChestplateTier()
        {
            int chestplateType = Player.armor[1].type;

            if (chestplateType == ItemID.TinChainmail || chestplateType == ItemID.CopperChainmail)
                return ChestplateTier.TinCopper;

            if (chestplateType == ItemID.IronChainmail || chestplateType == ItemID.LeadChainmail)
                return ChestplateTier.IronLead;

            if (chestplateType == ItemID.SilverChainmail || chestplateType == ItemID.TungstenChainmail)
                return ChestplateTier.SilverTungsten;

            if (chestplateType == ItemID.GoldChainmail || chestplateType == ItemID.PlatinumChainmail)
                return ChestplateTier.GoldPlatinum;

            return ChestplateTier.None;
        }

        /// <summary>
        /// Detect if player is wearing a FULL ore armor set (all 3 pieces from same tier).
        /// This grants the mini-spelunker bonus.
        /// </summary>
        private bool DetectFullOreArmorSet()
        {
            int helmetType = Player.armor[0].type;
            int chestplateType = Player.armor[1].type;
            int greavesType = Player.armor[2].type;

            // Tin set
            if (helmetType == ItemID.TinHelmet &&
                chestplateType == ItemID.TinChainmail &&
                greavesType == ItemID.TinGreaves)
                return true;

            // Copper set
            if (helmetType == ItemID.CopperHelmet &&
                chestplateType == ItemID.CopperChainmail &&
                greavesType == ItemID.CopperGreaves)
                return true;

            // Iron set
            if (helmetType == ItemID.IronHelmet &&
                chestplateType == ItemID.IronChainmail &&
                greavesType == ItemID.IronGreaves)
                return true;

            // Lead set
            if (helmetType == ItemID.LeadHelmet &&
                chestplateType == ItemID.LeadChainmail &&
                greavesType == ItemID.LeadGreaves)
                return true;

            // Silver set
            if (helmetType == ItemID.SilverHelmet &&
                chestplateType == ItemID.SilverChainmail &&
                greavesType == ItemID.SilverGreaves)
                return true;

            // Tungsten set
            if (helmetType == ItemID.TungstenHelmet &&
                chestplateType == ItemID.TungstenChainmail &&
                greavesType == ItemID.TungstenGreaves)
                return true;

            // Gold set
            if (helmetType == ItemID.GoldHelmet &&
                chestplateType == ItemID.GoldChainmail &&
                greavesType == ItemID.GoldGreaves)
                return true;

            // Platinum set
            if (helmetType == ItemID.PlatinumHelmet &&
                chestplateType == ItemID.PlatinumChainmail &&
                greavesType == ItemID.PlatinumGreaves)
                return true;

            return false;
        }
    }
}