using Microsoft.Xna.Framework;
using Terraria;
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
            if (!hasValidSavedPosition)
                return;

            // Validate the position before restoring
            if (IsPositionSafeForPlayer(savedExitPosition))
            {
                Player.position = savedExitPosition;
                
                // Reset velocity to prevent continued falling/movement
                Player.velocity = Vector2.Zero;
                
                // Give brief immunity to prevent instant damage on spawn
                Player.immune = true;
                Player.immuneTime = 60; // 1 second immunity
            }
            // else: fall back to spawn point (default behavior)

            // Clear the flag so we don't restore again
            hasValidSavedPosition = false;
        }

        /// <summary>
        /// Check if a position is safe to spawn the player at.
        /// Returns false if the player would be inside solid blocks.
        /// </summary>
        /// <param name="positionToCheck">World position to validate</param>
        /// <returns>True if position is safe, false if player would be stuck</returns>
        private bool IsPositionSafeForPlayer(Vector2 positionToCheck)
        {
            // Convert to tile coordinates
            Point tileCoordinates = positionToCheck.ToTileCoordinates();

            // Check tile bounds
            if (tileCoordinates.X < 0 || tileCoordinates.X >= Main.maxTilesX - 2 ||
                tileCoordinates.Y < 0 || tileCoordinates.Y >= Main.maxTilesY - 3)
            {
                return false; // Out of world bounds
            }

            // Check a 2x3 tile area (player hitbox size)
            // Player is about 2 tiles wide and 3 tiles tall
            for (int xOffset = 0; xOffset < 2; xOffset++)
            {
                for (int yOffset = 0; yOffset < 3; yOffset++)
                {
                    int tileX = tileCoordinates.X + xOffset;
                    int tileY = tileCoordinates.Y + yOffset;

                    Tile tileAtPosition = Main.tile[tileX, tileY];
                    
                    // Check if tile is solid and would block the player
                    if (tileAtPosition.HasTile && Main.tileSolid[tileAtPosition.TileType])
                    {
                        return false; // Would spawn inside solid blocks
                    }
                }
            }

            return true;
        }
    }
}