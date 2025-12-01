// MIT Licensed - Copyright (c) 2025 David W. Jeske
// ╔════════════════════════════════════════════════════════════════════════════════╗
// ║  PACKET HANDLER - Handles incoming packets for Persistent Position feature      ║
// ║                                                                                 ║
// ║  All packet handling code for this feature lives here.                          ║
// ║  DuravoQOLMod.HandlePacket() dispatches to this class.                          ║
// ╚════════════════════════════════════════════════════════════════════════════════╝
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DuravoQOLMod.PersistentPosition
{
    /// <summary>
    /// Handles packet processing for the Persistent Position feature.
    /// Called by DuravoQOLMod.HandlePacket() when a relevant packet is received.
    /// </summary>
    public static class PersistentPositionPacketHandler
    {
        /// <summary>
        /// Handle the RestoreSavedPosition packet (received by CLIENT from server).
        /// Teleports the local player to the saved position and grants spawn immunity.
        /// </summary>
        /// <param name="reader">Binary reader for packet data</param>
        /// <param name="senderWhoAmI">Who sent the packet (server in this case)</param>
        public static void HandleRestoreSavedPositionPacket(BinaryReader reader, int senderWhoAmI)
        {
            // Read position data
            float positionX = reader.ReadSingle();
            float positionY = reader.ReadSingle();
            Vector2 savedPosition = new Vector2(positionX, positionY);

            // Only apply on client side
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            Player localPlayer = Main.LocalPlayer;

            // Apply position with small upward nudge (same as client-side restore)
            const float PositionNudgeUpPixels = 16f / 5f;
            localPlayer.position = savedPosition - new Vector2(0, PositionNudgeUpPixels);
            localPlayer.velocity = Vector2.Zero;

            // Grant spawn immunity via shared system
            int immunityDurationTicks = TemporarySpawnImmunityPlayer.DefaultImmunityDurationTicks;
            TemporarySpawnImmunityPlayer.GrantImmunityToLocalPlayer(immunityDurationTicks);

            float immunitySeconds = immunityDurationTicks / 60f;
            Main.NewText($"[DuravoQOL] Position restored from world data. Immune for {immunitySeconds}s.", 100, 255, 100);
        }
    }
}