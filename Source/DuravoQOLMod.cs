// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using DuravoQOLMod.PersistentPosition;

namespace DuravoQOLMod
{
    /// <summary>
    /// Packet types for mod networking (server <-> client communication)
    /// </summary>
    public enum ModPacketType : byte
    {
        /// <summary>Server sends saved position to client for restore</summary>
        RestoreSavedPosition = 1,
    }

    public class DuravoQOLMod : Mod
    {
        // Main mod class - tModLoader entry point
        // Features are implemented via ModPlayer, GlobalNPC, etc. classes

        /// <summary>
        /// Handle incoming packets from server or client.
        /// Called by tModLoader when a ModPacket is received.
        /// </summary>
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ModPacketType packetType = (ModPacketType)reader.ReadByte();

            switch (packetType) {
                case ModPacketType.RestoreSavedPosition:
                    HandleRestoreSavedPositionPacket(reader, whoAmI);
                    break;
                default:
                    // Unknown packet type - silently ignore
                    break;
            }
        }

        /// <summary>
        /// Handle the RestoreSavedPosition packet (received by CLIENT from server).
        /// Teleports the local player to the saved position.
        /// </summary>
        private void HandleRestoreSavedPositionPacket(BinaryReader reader, int whoAmI)
        {
            // DEBUG: Show packet received
            Main.NewText($"[DEBUG] CLIENT received RestoreSavedPosition packet! whoAmI={whoAmI}", 255, 255, 100);

            // Read position data
            float positionX = reader.ReadSingle();
            float positionY = reader.ReadSingle();
            Vector2 savedPosition = new Vector2(positionX, positionY);

            // DEBUG: Show position from packet
            int tileX = (int)(savedPosition.X / 16);
            int tileY = (int)(savedPosition.Y / 16);
            Main.NewText($"[DEBUG] Packet position: tile ({tileX}, {tileY})", 255, 255, 100);

            // Only apply on client side
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                Player localPlayer = Main.LocalPlayer;

                // Apply position with small upward nudge (same as client-side restore)
                const float PositionNudgeUpPixels = 16f / 5f;
                localPlayer.position = savedPosition - new Vector2(0, PositionNudgeUpPixels);
                localPlayer.velocity = Vector2.Zero;

                Main.NewText($"[Server] Position restored from world data. Immune for 3s.", 100, 255, 100);
            }
            else {
                Main.NewText($"[DEBUG] NOT applying - netMode={Main.netMode} (not MultiplayerClient)", 255, 100, 100);
            }
        }
    }
}