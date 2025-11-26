using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TerrariaSurvivalMod
{
    /// <summary>
    /// Saves and restores player position on world exit/enter.
    /// This prevents using logout as a free escape mechanism.
    /// </summary>
    public class PersistentPositionPlayer : ModPlayer
    {
        // Position saved when player exits the world
        private Vector2 savedExitPosition;
        
        // Whether we have a valid position to restore
        private bool hasValidSavedPosition;
        
        // Tracks remaining ticks of absolute damage immunity after world enter
        // Uses ModifyHurt to block ALL damage sources (not just collision)
        private int absoluteImmunityTicksRemaining;
        
        // Tracks remaining ticks for spawn light effect
        private int spawnLightTicksRemaining;

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
            // Always show spawn light effect for 4 seconds (240 ticks)
            spawnLightTicksRemaining = 240;
            
            if (!hasValidSavedPosition)
            {
                // No saved position, but still grant immunity for 3 seconds on any world enter
                absoluteImmunityTicksRemaining = 180;
                Main.NewText($"[TSM] World entered. Immune for 3s.", 100, 200, 255);
                return;
            }

            // Validate the position before restoring
            if (IsPositionSafeForPlayer(savedExitPosition))
            {
                Player.position = savedExitPosition;
                
                // Reset velocity to prevent continued falling/movement
                Player.velocity = Vector2.Zero;
                
                // Start ABSOLUTE immunity (blocks ALL damage via ModifyHurt)
                // 8 seconds = 480 ticks (for testing, reduce to ~180 for release)
                absoluteImmunityTicksRemaining = 480;
                
                // Log successful restore for debugging
                Main.NewText($"[TSM] Position restored. ABSOLUTE immune for 8s.", 100, 255, 100);
            }
            else
            {
                // Position was unsafe - log it and let normal spawn handle it
                absoluteImmunityTicksRemaining = 180; // Still grant 3s immunity
                Main.NewText($"[TSM] Saved position was unsafe, using spawn point. Immune for 3s.", 255, 200, 100);
            }

            // Clear the flag so we don't restore again
            hasValidSavedPosition = false;
        }
        
        /// <summary>
        /// Called every frame - handle spawn light effect and immunity timer.
        /// </summary>
        public override void PreUpdate()
        {
            // Handle spawn light effect (4 seconds of glowing light around player)
            if (spawnLightTicksRemaining > 0)
            {
                spawnLightTicksRemaining--;
                
                // Calculate fade based on remaining time (brighter at start, fades out)
                float lightIntensity = (float)spawnLightTicksRemaining / 240f;
                
                // Add bright protective light around player (cyan/white glow)
                Lighting.AddLight(Player.Center, 0.8f * lightIntensity, 1.0f * lightIntensity, 1.2f * lightIntensity);
                
                // Spawn occasional sparkle particles
                if (Main.rand.NextBool(3) && spawnLightTicksRemaining > 60)
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
            }
            
            // Handle absolute immunity timer
            if (absoluteImmunityTicksRemaining > 0)
            {
                absoluteImmunityTicksRemaining--;
                
                // Also set vanilla immunity for visual feedback (optional blinking)
                Player.immune = true;
                Player.immuneTime = Math.Max(Player.immuneTime, 2); // Minimal, just for visual
                
                // Log approximately every 2 seconds so we can see it's working
                if (absoluteImmunityTicksRemaining % 120 == 0 && absoluteImmunityTicksRemaining > 0)
                {
                    int secondsRemaining = absoluteImmunityTicksRemaining / 60;
                    Main.NewText($"[TSM] ABSOLUTE Immunity: {secondsRemaining}s remaining", 100, 255, 100);
                }
            }
        }
        
        /// <summary>
        /// Block ALL damage during absolute immunity period.
        /// This works for ALL damage sources: enemies, suffocation, lava, debuffs, etc.
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // DEBUG: Log ALL incoming damage with source info
            string damageSourceInfo = GetDamageSourceDebugInfo(modifiers);
            int incomingDamage = (int)modifiers.FinalDamage.Base;
            Main.NewText($"[TSM-DMG] {incomingDamage} dmg from: {damageSourceInfo}", 255, 100, 100);
            
            if (absoluteImmunityTicksRemaining > 0)
            {
                // Block ALL damage by setting final damage to 0
                modifiers.FinalDamage *= 0f;
                
                // Visual feedback that damage was blocked
                Main.NewText($"[TSM] BLOCKED! ({absoluteImmunityTicksRemaining / 60}s left)", 100, 255, 100);
                CombatText.NewText(Player.Hitbox, Color.Cyan, "Protected!");
            }
        }
        
        /// <summary>
        /// Extract debug info about the damage source from HurtModifiers.
        /// </summary>
        private static string GetDamageSourceDebugInfo(Player.HurtModifiers modifiers)
        {
            System.Text.StringBuilder damageInfo = new System.Text.StringBuilder();
            
            // Check damage reason flags
            if (modifiers.DamageSource.SourceNPCIndex >= 0 && modifiers.DamageSource.SourceNPCIndex < Main.maxNPCs)
            {
                NPC sourceNPC = Main.npc[modifiers.DamageSource.SourceNPCIndex];
                if (sourceNPC.active)
                {
                    damageInfo.Append($"NPC:{sourceNPC.FullName} (ID:{sourceNPC.type})");
                }
                else
                {
                    damageInfo.Append($"NPC:Inactive#{modifiers.DamageSource.SourceNPCIndex}");
                }
            }
            else if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
            {
                int projIndex = modifiers.DamageSource.SourceProjectileLocalIndex;
                if (projIndex < Main.maxProjectiles && Main.projectile[projIndex].active)
                {
                    Projectile sourceProj = Main.projectile[projIndex];
                    damageInfo.Append($"Projectile:{sourceProj.Name} (Type:{sourceProj.type})");
                }
                else
                {
                    damageInfo.Append($"Projectile:Unknown#{projIndex}");
                }
            }
            else if (modifiers.DamageSource.SourcePlayerIndex >= 0)
            {
                damageInfo.Append($"Player:{modifiers.DamageSource.SourcePlayerIndex}");
            }
            else
            {
                // No specific source - likely environmental
                damageInfo.Append("Environmental/Unknown");
            }
            
            // Add any additional context from the modifiers
            damageInfo.Append($" | Knockback:{modifiers.Knockback.Base:F1}");
            
            return damageInfo.ToString();
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