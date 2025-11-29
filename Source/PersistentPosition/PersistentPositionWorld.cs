// MIT Licensed - Copyright (c) 2025 David W. Jeske
// ╔════════════════════════════════════════════════════════════════════════════════╗
// ║  SERVER-SIDE ONLY - This code runs on the SERVER/HOST MACHINE                  ║
// ║                                                                                 ║
// ║  This file does NOT interact with PersistentPositionPlayer.cs at all!          ║
// ║  - Stores positions in the world's .wld file (server-side storage)             ║
// ║  - Controlled by server config: WorldServerPersistentPosition                  ║
// ║  - Only the server/host can change this setting                                ║
// ║  - Positions captured periodically while players are connected                 ║
// ║                                                                                 ║
// ║  PersistentPositionPlayer.cs is a COMPLETELY SEPARATE system for client-side   ║
// ║  storage that runs on each player's machine.                                   ║
// ╚════════════════════════════════════════════════════════════════════════════════╝
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DuravoQOLMod.PersistentPosition
{
    /// <summary>
    /// SERVER-SIDE position persistence. Stores positions in world file.
    /// Completely independent from PersistentPositionPlayer (client-side storage).
    /// Positions are captured periodically while players are connected.
    /// </summary>
    public class PersistentPositionWorld : ModSystem
    {
        /// <summary>How often to update position data (in frames, 60fps = 180 frames per 3 seconds)</summary>
        private const int PositionUpdateIntervalFrames = 180; // ~3 seconds at 60fps

        /// <summary>Frame counter for periodic updates</summary>
        private int framesSinceLastPositionUpdate = 0;

        /// <summary>
        /// Player exit positions stored in this world, keyed by player name.
        /// In single-player there's only one entry; in multiplayer, one per player who has visited.
        /// Dictionary entries are created once per player; position values are updated in-place.
        /// </summary>
        private Dictionary<string, Vector2> playerExitPositions = new();

        /// <summary>
        /// Get the singleton instance of this ModSystem.
        /// </summary>
        public static PersistentPositionWorld Instance => ModContent.GetInstance<PersistentPositionWorld>();

        /// <summary>
        /// Save a player's exit position to world data.
        /// Called when a player saves/exits.
        /// </summary>
        public void SavePlayerExitPosition(string playerName, Vector2 position)
        {
            playerExitPositions[playerName] = position;
        }

        /// <summary>
        /// Try to get a player's saved exit position.
        /// Returns true if a position was found, false otherwise.
        /// </summary>
        public bool TryGetSavedPosition(string playerName, out Vector2 position)
        {
            return playerExitPositions.TryGetValue(playerName, out position);
        }

        /// <summary>
        /// Clear a player's saved position (after successful restore).
        /// </summary>
        public void ClearSavedPosition(string playerName)
        {
            playerExitPositions.Remove(playerName);
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                    SERVER-SIDE POSITION RESTORE                    ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Send saved position to a specific client. Called when player joins server.
        /// Server runs FindBestSpawnPosition before sending to handle world changes.
        /// </summary>
        /// <param name="playerWhoAmI">The player index (whoAmI) to send to</param>
        /// <param name="playerName">The player's name (used as dictionary key)</param>
        public void SendSavedPositionToClient(int playerWhoAmI, string playerName)
        {
            // DEBUG: Show entry
            ChatHelper.BroadcastChatMessage(
                NetworkText.FromLiteral($"[DEBUG] SendSavedPositionToClient: playerWhoAmI={playerWhoAmI}, playerName={playerName}"),
                new Color(255, 150, 255));

            // Only run on server
            if (Main.netMode != NetmodeID.Server) {
                ChatHelper.BroadcastChatMessage(
                    NetworkText.FromLiteral($"[DEBUG] ABORT: Not server (netMode={Main.netMode})"),
                    new Color(255, 100, 100));
                return;
            }

            // Check if feature is enabled
            if (!DuravoQOLModServerConfig.EnableWorldServerPersistentPosition) {
                ChatHelper.BroadcastChatMessage(
                    NetworkText.FromLiteral($"[DEBUG] ABORT: Feature disabled"),
                    new Color(255, 100, 100));
                return;
            }

            // Check if we have a saved position for this player
            if (!TryGetSavedPosition(playerName, out Vector2 savedPosition)) {
                ChatHelper.BroadcastChatMessage(
                    NetworkText.FromLiteral($"[DEBUG] ABORT: No saved position for '{playerName}'"),
                    new Color(255, 100, 100));
                return;
            }

            // TODO: This bump logic is basic and may not be enough for server authority.
            // Potential exploit: Player A logs out at position X. Player B intentionally
            // places blocks covering position X to trap Player A. When A logs back in,
            // the bump logic might push them through walls they shouldn't pass through.
            // For true server authority, we may need:
            //   1. Record the "safety bubble" around logout position
            //   2. Detect if world changed substantially near saved position
            //   3. If area is now dangerous/blocked, fall back to spawn point
            //   4. Consider impenetrable/special block types that should never be bumped through
            Vector2 safePosition = FindBestSpawnPositionPublic(savedPosition);

            // Send position packet to client
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)ModPacketType.RestoreSavedPosition);
            packet.Write(safePosition.X);
            packet.Write(safePosition.Y);
            packet.Send(playerWhoAmI);

            // DEBUG: Confirm packet sent
            int tileX = (int)(safePosition.X / 16);
            int tileY = (int)(safePosition.Y / 16);
            ChatHelper.BroadcastChatMessage(
                NetworkText.FromLiteral($"[DEBUG] SENT packet to player {playerWhoAmI} at tile ({tileX}, {tileY})"),
                new Color(100, 255, 100));
        }

        /// <summary>
        /// Find the best spawn position for world-side restore.
        /// Tries to bump player out of solid tiles if world changed while they were offline.
        /// Public so it can be called from WorldPositionSyncPlayer in single-player mode.
        /// </summary>
        public static Vector2 FindBestSpawnPositionPublic(Vector2 originalPosition)
        {
            // If original position is clear, use it
            if (!IsPositionInSolidTiles(originalPosition)) {
                return originalPosition;
            }

            // Try bump offsets: -0.5 to 2 tiles in 0.5 tile (8 pixel) increments
            float[] bumpDistancesTiles = { -0.5f, 0.5f, -1f, 1f, -1.5f, 1.5f, -2f, 2f };
            const float PixelsPerTile = 16f;

            foreach (float xBumpTiles in bumpDistancesTiles) {
                foreach (float yBumpTiles in bumpDistancesTiles) {
                    if (xBumpTiles == 0f && yBumpTiles == 0f)
                        continue;

                    Vector2 bumpedPosition = originalPosition + new Vector2(xBumpTiles * PixelsPerTile, yBumpTiles * PixelsPerTile);

                    if (!IsPositionInWorldBounds(bumpedPosition))
                        continue;

                    if (!IsPositionInSolidTiles(bumpedPosition)) {
                        return bumpedPosition;
                    }
                }
            }

            // No clear bump found - return original (player will be pushed out by game)
            return originalPosition;
        }

        /// <summary>
        /// Check if position is within valid world bounds.
        /// </summary>
        private static bool IsPositionInWorldBounds(Vector2 positionToCheck)
        {
            int baseTileX = (int)((positionToCheck.X + 8) / 16);
            int baseTileY = (int)((positionToCheck.Y + 8) / 16);

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

            for (int xOffset = 0; xOffset < 2; xOffset++) {
                for (int yOffset = 0; yOffset < 3; yOffset++) {
                    int tileX = baseTileX + xOffset;
                    int tileY = baseTileY + yOffset;

                    if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
                        continue;

                    Tile tileAtPosition = Main.tile[tileX, tileY];

                    if (tileAtPosition.HasTile && Main.tileSolid[tileAtPosition.TileType] && !Main.tileSolidTop[tileAtPosition.TileType]) {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Clear all position data when world is unloaded.
        /// </summary>
        public override void OnWorldUnload()
        {
            playerExitPositions.Clear();
            framesSinceLastPositionUpdate = 0;
        }

        /// <summary>
        /// Periodically capture player positions while they are connected.
        /// This runs every frame but only updates the dictionary every ~3 seconds.
        /// This ensures positions are saved even if server shuts down unexpectedly.
        /// </summary>
        public override void PostUpdateWorld()
        {
            // Only track if server-side position storage is enabled
            if (!DuravoQOLModServerConfig.EnableWorldServerPersistentPosition)
                return;

            framesSinceLastPositionUpdate++;

            // Only update periodically to avoid unnecessary work
            if (framesSinceLastPositionUpdate < PositionUpdateIntervalFrames)
                return;

            framesSinceLastPositionUpdate = 0;

            // Update positions for all active, living players
            // Note: We're updating existing entries (no allocation) or adding new ones (one-time allocation per player)
            for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++) {
                Player player = Main.player[playerIndex];
                if (player != null && player.active && !player.dead) {
                    // Dictionary[key] = value replaces existing value without allocation for existing keys
                    playerExitPositions[player.name] = player.position;

                    // DEBUG: Show position saved
                    int tileX = (int)(player.position.X / 16);
                    int tileY = (int)(player.position.Y / 16);
                    string debugMessage = $"[World] Saved {player.name} @ tile ({tileX}, {tileY})";

                    if (Main.netMode == NetmodeID.SinglePlayer) {
                        Main.NewText(debugMessage, 150, 150, 255);
                    }
                    else if (Main.netMode == NetmodeID.Server) {
                        // Send to specific client
                        ChatHelper.SendChatMessageToClient(
                            NetworkText.FromLiteral(debugMessage),
                            new Color(150, 150, 255),
                            playerIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Before world saves, capture FINAL positions of all active players.
        /// This is a last-chance capture before the world file is written.
        /// </summary>
        public override void PreSaveAndQuit()
        {
            // Only save if WORLD/SERVER position storage is enabled
            if (!DuravoQOLModServerConfig.EnableWorldServerPersistentPosition)
                return;

            // Final capture of all active players' current positions
            for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++) {
                Player player = Main.player[playerIndex];
                if (player != null && player.active && !player.dead) {
                    playerExitPositions[player.name] = player.position;
                }
            }
        }

        /// <summary>
        /// Save position data to the world file.
        /// </summary>
        public override void SaveWorldData(TagCompound tag)
        {
            // DEBUG: Log save attempt
            if (Main.netMode == NetmodeID.SinglePlayer) {
                Main.NewText($"[DEBUG] SaveWorldData: {playerExitPositions.Count} positions to save", 255, 200, 100);
            }
            // Note: Can't send chat messages to clients during save - world is closing

            // Even if feature is disabled, save existing data so it's not lost
            if (playerExitPositions.Count == 0)
                return;

            // Serialize as parallel lists (TagCompound doesn't support Dictionary directly)
            var playerNames = new List<string>();
            var positionsX = new List<float>();
            var positionsY = new List<float>();

            foreach (var kvp in playerExitPositions) {
                playerNames.Add(kvp.Key);
                positionsX.Add(kvp.Value.X);
                positionsY.Add(kvp.Value.Y);
            }

            tag["persistentPos_names"] = playerNames;
            tag["persistentPos_x"] = positionsX;
            tag["persistentPos_y"] = positionsY;
        }

        /// <summary>
        /// Load position data from the world file.
        /// </summary>
        public override void LoadWorldData(TagCompound tag)
        {
            playerExitPositions.Clear();

            // DEBUG: Log load attempt - but we can't display to player yet, they haven't connected
            // We'll show count after load instead

            if (!tag.ContainsKey("persistentPos_names")) {
                // Will show debug later when player connects
                return;
            }

            var playerNames = tag.GetList<string>("persistentPos_names");
            var positionsX = tag.GetList<float>("persistentPos_x");
            var positionsY = tag.GetList<float>("persistentPos_y");

            // Validate list lengths match
            if (playerNames.Count != positionsX.Count || playerNames.Count != positionsY.Count) {
                return; // Silently ignore corrupted data
            }

            for (int i = 0; i < playerNames.Count; i++) {
                playerExitPositions[playerNames[i]] = new Vector2(positionsX[i], positionsY[i]);
            }

            // Store count for debug display when first player connects
            _loadedPositionCount = playerExitPositions.Count;
        }

        // DEBUG: Track how many positions were loaded
        private int _loadedPositionCount = 0;
        public int LoadedPositionCount => _loadedPositionCount;
    }
}