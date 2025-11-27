using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariaSurvivalMod;

namespace TerrariaSurvivalMod
{
    /// <summary>
    /// Saves and restores player position on world exit/enter.
    /// This prevents using logout as a free escape mechanism.
    /// </summary>
    public class PersistentPositionPlayer : ModPlayer
    {
        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                        TUNABLE CONSTANTS                           ║
        // ╚════════════════════════════════════════════════════════════════════╝
        
        /// <summary>Duration of spawn immunity in seconds (DEBUG: 8s for testing, set to 3 for release)</summary>
        private const double SpawnImmunityDurationSeconds = 3.0;
        
        /// <summary>Get debug message setting from mod config</summary>
        private static bool DebugMessagesEnabled => ModContent.GetInstance<TerrariaSurvivalModConfig>()?.EnableDebugMessages ?? false;
        
        /// <summary>DEBUG: Block Environmental/Unknown damage during immunity (suffocation, etc)</summary>
        private const bool BlockEnvironmentalDamageDuringImmunity = true;
        
        /// <summary>DEBUG: Block NPC damage during immunity</summary>
        private const bool BlockNPCDamageDuringImmunity = true;
        
        /// <summary>DEBUG: Block Projectile damage during immunity</summary>
        private const bool BlockProjectileDamageDuringImmunity = true;
        
        /// <summary>DEBUG: Block Player (PvP) damage during immunity</summary>
        private const bool BlockPlayerDamageDuringImmunity = true;
        
        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          INSTANCE STATE                            ║
        // ╚════════════════════════════════════════════════════════════════════╝
        
        // Position saved when player exits the world
        private Vector2 savedExitPosition;
        
        // Whether we have a valid position to restore
        private bool hasValidSavedPosition;
        
        // Epoch time (seconds since 1970) when immunity expires (0 = not immune)
        private double immunityExpiresAtEpochSeconds;
        
        /// <summary>Get current time as epoch seconds (wall clock time, never wraps)</summary>
        private static double GetEpochTimeSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        }
        
        /// <summary>Check if immunity is currently active</summary>
        private bool IsImmunityActive => GetEpochTimeSeconds() < immunityExpiresAtEpochSeconds;
        
        /// <summary>Get remaining immunity time in seconds (0 if expired)</summary>
        private double GetImmunityRemainingSeconds()
        {
            double remaining = immunityExpiresAtEpochSeconds - GetEpochTimeSeconds();
            return remaining > 0 ? remaining : 0;
        }

        /// <summary>
        /// Save player position when exiting the world.
        /// Does NOT save if player is dead (let normal respawn handle it).
        /// </summary>
        public override void SaveData(TagCompound tag)
        {
            // Don't save position if player is dead - they should respawn normally
            if (Player.dead)
                return;

            tag["exitPositionX"] = Player.position.X;
            tag["exitPositionY"] = Player.position.Y;
            tag["hasExitPosition"] = true;
        }

        /// <summary>
        /// Load saved position data when player data is loaded.
        /// </summary>
        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("hasExitPosition") && tag.GetBool("hasExitPosition"))
            {
                float exitX = tag.GetFloat("exitPositionX");
                float exitY = tag.GetFloat("exitPositionY");
                savedExitPosition = new Vector2(exitX, exitY);
                hasValidSavedPosition = true;
            }
            else
            {
                hasValidSavedPosition = false;
            }
        }

        /// <summary>
        /// Restore player to their saved position when entering the world.
        /// Validates the position is safe before restoring.
        /// </summary>
        public override void OnEnterWorld()
        {
            // Try to restore player to saved position if available and safe
            if (hasValidSavedPosition && IsPositionSafeForPlayer(savedExitPosition))
            {
                // Nudge player UP by 1/5 tile (3.2 pixels) to prevent spawning inside ground
                const float PositionNudgeUpPixels = 16f / 5f;
                Player.position = savedExitPosition - new Vector2(0, PositionNudgeUpPixels);
                Player.velocity = Vector2.Zero;
                Main.NewText($"[TSM] Position restored. Immune for {SpawnImmunityDurationSeconds}s.", 100, 255, 100);
            }
            else if (hasValidSavedPosition)
            {
                Main.NewText($"[TSM] Saved position was unsafe, using spawn point. Immune for {SpawnImmunityDurationSeconds}s.", 255, 200, 100);
            }
            else
            {
                Main.NewText($"[TSM] World entered. Immune for {SpawnImmunityDurationSeconds}s.", 100, 200, 255);
            }

            // Clear saved position flag
            hasValidSavedPosition = false;
            
            // Grant spawn immunity (ONE place)
            immunityExpiresAtEpochSeconds = GetEpochTimeSeconds() + SpawnImmunityDurationSeconds;
        }
        
        // Track last logged second to avoid spam
        private int lastLoggedSecond = -1;
        
        /// <summary>
        /// Called every frame - handle spawn light effect during immunity.
        /// </summary>
        public override void PreUpdate()
        {
            // Handle immunity visual effects (light + particles)
            if (IsImmunityActive)
            {
                double remainingSeconds = GetImmunityRemainingSeconds();
                
                // Calculate fade based on remaining time vs total duration (brighter at start, fades out)
                float lightIntensity = (float)(remainingSeconds / SpawnImmunityDurationSeconds);
                
                // Add bright protective light around player (cyan/white glow)
                Lighting.AddLight(Player.Center, 0.8f * lightIntensity, 1.0f * lightIntensity, 1.2f * lightIntensity);
                
                // Spawn occasional sparkle particles (stop when nearly expired)
                if (Main.rand.NextBool(3) && remainingSeconds > 1.0)
                {
                    float angle = Main.rand.NextFloat() * MathHelper.TwoPi;
                    float radius = Main.rand.NextFloat(10f, 30f);
                    Vector2 dustPos = Player.Center + new Vector2(
                        (float)Math.Cos(angle) * radius,
                        (float)Math.Sin(angle) * radius
                    );
                    
                    Dust spawnDust = Dust.NewDustPerfect(
                        dustPos,
                        DustID.MagicMirror,
                        Vector2.Zero,
                        Alpha: 100,
                        Scale: 0.6f * lightIntensity
                    );
                    spawnDust.noGravity = true;
                    spawnDust.fadeIn = 0.5f;
                }
                
                // Log approximately every 2 seconds so we can see it's working (only if debug enabled)
                int currentSecond = (int)remainingSeconds;
                if (DebugMessagesEnabled && currentSecond != lastLoggedSecond && currentSecond % 2 == 0 && currentSecond > 0)
                {
                    lastLoggedSecond = currentSecond;
                    Main.NewText($"[TSM] ABSOLUTE Immunity: {currentSecond}s remaining", 100, 255, 100);
                }
            }
            else if (lastLoggedSecond != -1)
            {
                // Immunity just ended - log it once (only if debug enabled)
                if (DebugMessagesEnabled)
                {
                    Main.NewText($"[TSM] Immunity ENDED - you can now take damage!", 255, 100, 100);
                }
                lastLoggedSecond = -1;
            }
        }
        
        /// <summary>
        /// Damage source categories for selective blocking during immunity.
        /// </summary>
        private enum DamageSourceCategory
        {
            Environmental,  // Suffocation, drowning, lava tile contact, etc.
            NPC,            // Enemy contact or attacks
            Projectile,     // Hostile projectiles
            Player          // PvP damage
        }
        
        /// <summary>
        /// Block damage during immunity period with per-category control.
        /// This allows experimenting with which damage types need full immunity.
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // Only process during immunity period
            if (!IsImmunityActive)
                return;
            
            // Categorize the damage source
            DamageSourceCategory damageCategory = CategorizeDamageSource(modifiers);
            
            // Get actual incoming damage values BEFORE any modification
            int sourceDamageBase = (int)modifiers.SourceDamage.Base;
            int finalDamageBase = (int)modifiers.FinalDamage.Base;
            
            // Check if we should block this damage category
            bool shouldBlock = ShouldBlockDamageCategory(damageCategory);
            
            if (shouldBlock)
            {
                // Debug log BEFORE blocking (so we see original values)
                if (DebugMessagesEnabled)
                {
                    string sourceDetails = GetDamageSourceDetails(modifiers, damageCategory);
                    int secondsRemaining = (int)GetImmunityRemainingSeconds();
                    Main.NewText($"[TSM] BLOCKING {damageCategory}: src={sourceDamageBase} final={finalDamageBase} | {sourceDetails} ({secondsRemaining}s left)", 100, 255, 100);
                }
                
                // Block this damage
                modifiers.FinalDamage *= 0f;
                CombatText.NewText(Player.Hitbox, Color.Cyan, "Protected!");
            }
            else
            {
                if (DebugMessagesEnabled)
                {
                    string sourceDetails = GetDamageSourceDetails(modifiers, damageCategory);
                    Main.NewText($"[TSM] ALLOWING {damageCategory}: src={sourceDamageBase} final={finalDamageBase} | {sourceDetails}", 255, 100, 100);
                }
            }
        }
        
        /// <summary>
        /// Determine the category of damage source.
        /// </summary>
        private static DamageSourceCategory CategorizeDamageSource(Player.HurtModifiers modifiers)
        {
            if (modifiers.DamageSource.SourceNPCIndex >= 0 && modifiers.DamageSource.SourceNPCIndex < Main.maxNPCs)
            {
                return DamageSourceCategory.NPC;
            }
            else if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
            {
                return DamageSourceCategory.Projectile;
            }
            else if (modifiers.DamageSource.SourcePlayerIndex >= 0)
            {
                return DamageSourceCategory.Player;
            }
            else
            {
                return DamageSourceCategory.Environmental;
            }
        }
        
        /// <summary>
        /// Check if we should block this damage category based on current flags.
        /// </summary>
        private static bool ShouldBlockDamageCategory(DamageSourceCategory category)
        {
            switch (category)
            {
                case DamageSourceCategory.Environmental:
                    return BlockEnvironmentalDamageDuringImmunity;
                case DamageSourceCategory.NPC:
                    return BlockNPCDamageDuringImmunity;
                case DamageSourceCategory.Projectile:
                    return BlockProjectileDamageDuringImmunity;
                case DamageSourceCategory.Player:
                    return BlockPlayerDamageDuringImmunity;
                default:
                    return true; // Block unknown by default
            }
        }
        
        /// <summary>
        /// Get detailed info about the damage source for logging.
        /// </summary>
        private static string GetDamageSourceDetails(Player.HurtModifiers modifiers, DamageSourceCategory category)
        {
            switch (category)
            {
                case DamageSourceCategory.NPC:
                    int npcIndex = modifiers.DamageSource.SourceNPCIndex;
                    if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
                    {
                        NPC sourceNPC = Main.npc[npcIndex];
                        if (sourceNPC.active)
                            return $"{sourceNPC.FullName} (Type:{sourceNPC.type})";
                        return $"Inactive NPC #{npcIndex}";
                    }
                    return "Invalid NPC index";
                    
                case DamageSourceCategory.Projectile:
                    int projIndex = modifiers.DamageSource.SourceProjectileLocalIndex;
                    if (projIndex >= 0 && projIndex < Main.maxProjectiles && Main.projectile[projIndex].active)
                    {
                        Projectile sourceProj = Main.projectile[projIndex];
                        return $"{sourceProj.Name} (Type:{sourceProj.type})";
                    }
                    return $"Unknown projectile #{projIndex}";
                    
                case DamageSourceCategory.Player:
                    return $"Player #{modifiers.DamageSource.SourcePlayerIndex}";
                    
                case DamageSourceCategory.Environmental:
                    return $"Knockback:{modifiers.Knockback.Base:F1}";
                    
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Check if a position is safe to spawn the player at.
        /// Returns false if the player would be inside solid blocks, lava, or other hazards.
        /// </summary>
        /// <param name="positionToCheck">World position to validate</param>
        /// <returns>True if position is safe, false if player would be in danger</returns>
        private bool IsPositionSafeForPlayer(Vector2 positionToCheck)
        {
            // Player.position is top-left corner; convert to tile coordinates
            // Add 8 pixels (half tile) for center alignment check
            int baseTileX = (int)((positionToCheck.X + 8) / 16);
            int baseTileY = (int)((positionToCheck.Y + 8) / 16);

            // Check tile bounds (player is ~2 tiles wide, ~3 tiles tall)
            if (baseTileX < 1 || baseTileX >= Main.maxTilesX - 3 ||
                baseTileY < 1 || baseTileY >= Main.maxTilesY - 4)
            {
                return false; // Out of world bounds
            }

            // Check a 2x3 tile area (player hitbox size)
            for (int xOffset = 0; xOffset < 2; xOffset++)
            {
                for (int yOffset = 0; yOffset < 3; yOffset++)
                {
                    int tileX = baseTileX + xOffset;
                    int tileY = baseTileY + yOffset;

                    Tile tileAtPosition = Main.tile[tileX, tileY];
                    
                    // Check if tile is solid and would block the player
                    if (tileAtPosition.HasTile && Main.tileSolid[tileAtPosition.TileType])
                    {
                        return false; // Would spawn inside solid blocks
                    }
                    
                    // Check for dangerous liquids
                    if (tileAtPosition.LiquidAmount > 0)
                    {
                        // LiquidType: 0=water, 1=lava, 2=honey, 3=shimmer
                        if (tileAtPosition.LiquidType == Terraria.ID.LiquidID.Lava)
                        {
                            return false; // Would spawn in lava
                        }
                    }
                }
            }
            
            // Check for solid ground beneath the player (don't spawn in mid-air over void)
            int groundCheckY = baseTileY + 3; // Just below player's feet
            if (groundCheckY < Main.maxTilesY)
            {
                bool hasGroundOrPlatform = false;
                for (int xOffset = 0; xOffset < 2; xOffset++)
                {
                    Tile groundTile = Main.tile[baseTileX + xOffset, groundCheckY];
                    if (groundTile.HasTile && (Main.tileSolid[groundTile.TileType] || Main.tileSolidTop[groundTile.TileType]))
                    {
                        hasGroundOrPlatform = true;
                        break;
                    }
                }
                
                // If no ground within 10 tiles, might be dangerous fall - still allow but warn
                if (!hasGroundOrPlatform)
                {
                    int emptyTilesBelow = 0;
                    for (int checkY = groundCheckY; checkY < groundCheckY + 30 && checkY < Main.maxTilesY; checkY++)
                    {
                        Tile checkTile = Main.tile[baseTileX, checkY];
                        if (checkTile.HasTile && Main.tileSolid[checkTile.TileType])
                            break;
                        if (checkTile.LiquidAmount > 0 && checkTile.LiquidType == Terraria.ID.LiquidID.Lava)
                            return false; // Lava below
                        emptyTilesBelow++;
                    }
                    
                    // More than 25 tiles of empty space = dangerous fall
                    if (emptyTilesBelow >= 25)
                    {
                        return false; // Would die from fall damage
                    }
                }
            }

            return true;
        }
    }
}