// MIT Licensed - Copyright (c) 2025 David W. Jeske
// ╔════════════════════════════════════════════════════════════════════════════════╗
// ║  World-side position restore trigger                                            ║
// ║                                                                                 ║
// ║  This ModPlayer triggers world position restore when a player enters:           ║
// ║  - Single-player: Directly applies position from world data                     ║
// ║  - Multiplayer: Server sends packet to client via SyncPlayer hook               ║
// ║                                                                                 ║
// ║  This is SEPARATE from PersistentPositionPlayer (client-side storage).          ║
// ╚════════════════════════════════════════════════════════════════════════════════╝
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DuravoQOLMod.PersistentPosition
{
    /// <summary>
    /// Triggers world position restore when player enters world.
    /// In single-player: directly applies position.
    /// In multiplayer: server sends packet to client.
    /// </summary>
    public class WorldPositionSyncPlayer : ModPlayer
    {
        /// <summary>
        /// Called when player enters world. In SINGLE-PLAYER, restore directly here
        /// since there's no client/server separation.
        /// </summary>
        public override void OnEnterWorld()
        {
            // Only handle single-player mode here
            // Multiplayer is handled via SyncPlayer
            if (Main.netMode != NetmodeID.SinglePlayer)
                return;

            // Check if world-side position storage is enabled
            if (!DuravoQOLModServerConfig.EnableWorldServerPersistentPosition)
                return;

            // Try to get saved position from world data
            var worldInstance = PersistentPositionWorld.Instance;
            if (worldInstance == null)
                return;

            if (!worldInstance.TryGetSavedPosition(Player.name, out Vector2 savedPosition))
                return;

            // Apply bump logic on the saved position (in case world changed)
            Vector2 safePosition = PersistentPositionWorld.FindBestSpawnPositionPublic(savedPosition);

            // Apply position directly (no packet needed in single-player)
            const float PositionNudgeUpPixels = 16f / 5f;
            Player.position = safePosition - new Vector2(0, PositionNudgeUpPixels);
            Player.velocity = Vector2.Zero;

            // Grant spawn immunity via shared system
            int immunityDurationTicks = TemporarySpawnImmunityPlayer.DefaultImmunityDurationTicks;
            TemporarySpawnImmunityPlayer.GrantImmunityToLocalPlayer(immunityDurationTicks);

            float immunitySeconds = immunityDurationTicks / 60f;
            Main.NewText($"[DuravoQOL] Position restored. Immune for {immunitySeconds}s.", 100, 255, 100);
        }

        /// <summary>
        /// Called when player data is synced. On SERVER in MULTIPLAYER, when newPlayer=true,
        /// send the saved position to the joining client via packet.
        /// </summary>
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            // Only run on dedicated server (not single-player)
            if (Main.netMode != NetmodeID.Server)
                return;

            // Always try to restore position - the saved position check will handle
            // cases where this is a brand new player (no saved position exists).
            // newPlayer=true means first time in this world, newPlayer=false means returning.
            // We want to restore for RETURNING players (newPlayer=false), but the
            // TryGetSavedPosition check naturally handles new players too.

            // Send saved position from world data to the client
            // fromWho is the player index who just joined
            PersistentPositionWorld.Instance?.SendSavedPositionToClient(fromWho, Player.name);
        }
    }
}