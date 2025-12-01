// MIT Licensed - Copyright (c) 2025 David W. Jeske
// ╔════════════════════════════════════════════════════════════════════════════════╗
// ║  CLIENT-SIDE ONLY - This code runs on the PLAYER'S MACHINE                     ║
// ║                                                                                 ║
// ║  This file does NOT interact with PersistentPositionWorld.cs at all!           ║
// ║  - Stores position in the player's .plr file (client-side storage)             ║
// ║  - Controlled by client config: ClientPersistentPosition                       ║
// ║  - Works even on servers that don't have this mod                              ║
// ║                                                                                 ║
// ║  PersistentPositionWorld.cs is a COMPLETELY SEPARATE system for server-side    ║
// ║  storage that runs on the server machine.                                      ║
// ╚════════════════════════════════════════════════════════════════════════════════╝
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DuravoQOLMod.PersistentPosition
{
    /// <summary>
    /// CLIENT-SIDE position persistence. Saves/restores from player file.
    /// Completely independent from PersistentPositionWorld (server-side storage).
    /// </summary>
    public class PersistentPositionPlayer : ModPlayer
    {
        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          INSTANCE STATE                            ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Position saved when player exits the world</summary>
        private Vector2 savedExitPosition;

        /// <summary>Whether we have a valid position to restore</summary>
        private bool hasValidSavedPosition;

        /// <summary>Get debug player persistence setting from mod config</summary>
        private static bool DebugMessagesEnabled => ModContent.GetInstance<DuravoQOLModConfig>()?.Debug?.DebugPlayerPersistence ?? false;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                    SAVE/LOAD POSITION DATA                         ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Save player position when exiting the world.
        /// Does NOT save if player is dead (let normal respawn handle it).
        /// </summary>
        public override void SaveData(TagCompound tag)
        {
            // Check if CLIENT-SIDE position storage is enabled (player's personal preference)
            if (!DuravoQOLModConfig.EnableClientPersistentPosition)
                return;

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
            if (tag.ContainsKey("hasExitPosition") && tag.GetBool("hasExitPosition")) {
                float exitX = tag.GetFloat("exitPositionX");
                float exitY = tag.GetFloat("exitPositionY");
                savedExitPosition = new Vector2(exitX, exitY);
                hasValidSavedPosition = true;
            }
            else {
                hasValidSavedPosition = false;
            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      POSITION RESTORE                              ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Restore player to their saved position when entering the world.
        /// ALWAYS restores to saved position (or nudged version) - no free teleport exploits.
        /// Only fails back to spawn point if position is out of world bounds (bug/glitch).
        /// </summary>
        public override void OnEnterWorld()
        {
            // Check if CLIENT-SIDE position storage is enabled - if not, skip position restore but still clear saved data
            if (!DuravoQOLModConfig.EnableClientPersistentPosition) {
                hasValidSavedPosition = false;
                return;
            }

            int immunityDurationTicks = TemporarySpawnImmunityPlayer.DefaultImmunityDurationTicks;
            float immunitySeconds = immunityDurationTicks / 60f;

            if (hasValidSavedPosition) {
                // Check for world bounds - only hard fail case (indicates bug/glitch)
                if (!IsPositionInWorldBounds(savedExitPosition)) {
                    Main.NewText($"[DuravoQOL] Saved position out of bounds (bug?), using spawn. Immune for {immunitySeconds}s.", 255, 100, 100);
                }
                else {
                    // Find best spawn position (may be bumped if inside solid tiles)
                    Vector2 finalSpawnPosition = FindBestSpawnPosition(savedExitPosition);

                    // Nudge player UP by 1/5 tile (3.2 pixels) to prevent spawning inside ground
                    const float PositionNudgeUpPixels = 16f / 5f;
                    Player.position = finalSpawnPosition - new Vector2(0, PositionNudgeUpPixels);
                    Player.velocity = Vector2.Zero;

                    if (finalSpawnPosition == savedExitPosition) {
                        Main.NewText($"[DuravoQOL] Position restored. Immune for {immunitySeconds}s.", 100, 255, 100);
                    }
                    else {
                        Main.NewText($"[DuravoQOL] Position restored (nudged from terrain). Immune for {immunitySeconds}s.", 255, 200, 100);
                    }
                }
            }
            else {
                Main.NewText($"[DuravoQOL] World entered. Immune for {immunitySeconds}s.", 100, 200, 255);
            }

            // Clear saved position flag
            hasValidSavedPosition = false;

            // Grant spawn immunity via shared system
            TemporarySpawnImmunityPlayer.GrantImmunityToLocalPlayer();
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                    POSITION VALIDATION HELPERS                     ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Check if position is within valid world bounds.
        /// This is the ONLY hard fail - indicates data corruption or bug.
        /// </summary>
        private static bool IsPositionInWorldBounds(Vector2 positionToCheck)
        {
            int baseTileX = (int)((positionToCheck.X + 8) / 16);
            int baseTileY = (int)((positionToCheck.Y + 8) / 16);

            // Player is ~2 tiles wide, ~3 tiles tall
            return baseTileX >= 1 && baseTileX < Main.maxTilesX - 3 &&
                   baseTileY >= 1 && baseTileY < Main.maxTilesY - 4;
        }

        /// <summary>
        /// Check if a position overlaps any solid tiles (player hitbox: 2x3 tiles).
        /// </summary>
        private static bool IsPositionInSolidTiles(Vector2 positionToCheck)
        {
            int baseTileX = (int)((positionToCheck.X + 8) / 16);
            int baseTileY = (int)((positionToCheck.Y + 8) / 16);

            // Check a 2x3 tile area (player hitbox size)
            for (int xOffset = 0; xOffset < 2; xOffset++) {
                for (int yOffset = 0; yOffset < 3; yOffset++) {
                    int tileX = baseTileX + xOffset;
                    int tileY = baseTileY + yOffset;

                    // Bounds check for the specific tile
                    if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
                        continue;

                    Tile tileAtPosition = Main.tile[tileX, tileY];

                    // Check if tile is solid (not platforms)
                    if (tileAtPosition.HasTile && Main.tileSolid[tileAtPosition.TileType] && !Main.tileSolidTop[tileAtPosition.TileType]) {
                        return true; // In solid tile
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Find the best spawn position, trying bump offsets if inside solid tiles.
        /// Returns the original position if clear, or a bumped position if found, or original anyway if no bump works.
        /// </summary>
        private Vector2 FindBestSpawnPosition(Vector2 originalPosition)
        {
            // If original position is clear, use it
            if (!IsPositionInSolidTiles(originalPosition)) {
                return originalPosition;
            }

            // Try bump offsets: -0.5 to 2 tiles in 0.5 tile (8 pixel) increments
            // Prioritize smaller bumps first
            float[] bumpDistancesTiles = { -0.5f, 0.5f, -1f, 1f, -1.5f, 1.5f, -2f, 2f };
            const float PixelsPerTile = 16f;

            foreach (float xBumpTiles in bumpDistancesTiles) {
                foreach (float yBumpTiles in bumpDistancesTiles) {
                    // Skip (0, 0) - we already checked original
                    if (xBumpTiles == 0f && yBumpTiles == 0f)
                        continue;

                    Vector2 bumpedPosition = originalPosition + new Vector2(xBumpTiles * PixelsPerTile, yBumpTiles * PixelsPerTile);

                    // Check bounds first
                    if (!IsPositionInWorldBounds(bumpedPosition))
                        continue;

                    // Check if bumped position is clear
                    if (!IsPositionInSolidTiles(bumpedPosition)) {
                        // Debug log the bump vector
                        if (DebugMessagesEnabled) {
                            Vector2 bumpVector = bumpedPosition - originalPosition;
                            Main.NewText($"[Client DEBUG] Position bumped by ({bumpVector.X:F1}, {bumpVector.Y:F1})px = ({xBumpTiles:F1}, {yBumpTiles:F1}) tiles", 255, 200, 100);
                        }
                        return bumpedPosition; // Found a clear spot
                    }
                }
            }

            // No clear bump found - spawn at original anyway (player will be pushed out by game)
            return originalPosition;
        }
    }
}