// MIT Licensed - Copyright (c) 2025 David W. Jeske
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using System;

namespace DuravoQOLMod.ArmorRebalance
{
    /// <summary>
    /// Custom draw layer that renders the emergency shield visual effect around the player.
    /// Draws after the player so the shield appears as an overlay.
    /// </summary>
    public class ShieldVisualLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.LastVanillaLayer);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            // Only show if player has active shield
            EmergencyShieldPlayer shieldPlayer = drawInfo.drawPlayer.GetModPlayer<EmergencyShieldPlayer>();
            return shieldPlayer.HasActiveShield;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            EmergencyShieldPlayer shieldPlayer = player.GetModPlayer<EmergencyShieldPlayer>();

            if (!shieldPlayer.HasActiveShield)
                return;

            // Get shield state from the player
            float shieldRatio = shieldPlayer.ShieldRatio;
            int shieldHP = shieldPlayer.CurrentShieldHP;
            bool isGoldTier = shieldPlayer.IsGoldTierShield;

            // Draw the shield circle
            DrawShieldCircle(drawInfo, player, shieldRatio, isGoldTier);

            // Draw the shield HP indicator
            DrawShieldHPIndicator(player, shieldHP, isGoldTier);
        }

        /// <summary>
        /// Draw a semi-transparent circle around the player.
        /// </summary>
        private void DrawShieldCircle(PlayerDrawSet drawInfo, Player player, float fillRatio, bool isGoldTier)
        {
            // Use vanilla magic pixel texture for simple drawing
            Texture2D pixelTexture = TextureAssets.MagicPixel.Value;

            Vector2 playerScreenPos = player.Center - Main.screenPosition;
            float circleRadius = 27f;
            int segments = 32;

            // Color based on shield tier
            Color baseColor = isGoldTier ? Color.Gold : Color.Cyan;
            Color circleColor = baseColor * (0.2f + 0.3f * fillRatio);

            // Draw circle outline using line segments
            for (int i = 0; i < segments; i++) {
                float angle1 = (float)i / segments * MathHelper.TwoPi;
                float angle2 = (float)(i + 1) / segments * MathHelper.TwoPi;

                Vector2 point1 = playerScreenPos + new Vector2(
                    (float)Math.Cos(angle1) * circleRadius,
                    (float)Math.Sin(angle1) * circleRadius
                );
                Vector2 point2 = playerScreenPos + new Vector2(
                    (float)Math.Cos(angle2) * circleRadius,
                    (float)Math.Sin(angle2) * circleRadius
                );

                // Calculate line properties
                Vector2 direction = point2 - point1;
                float length = direction.Length();
                float rotation = (float)Math.Atan2(direction.Y, direction.X);

                // Line thickness proportional to shield remaining (1-4 pixels)
                float lineThickness = 1f + 3f * fillRatio;

                // Create draw data for the line segment
                DrawData lineSegment = new DrawData(
                    pixelTexture,
                    point1,
                    new Rectangle(0, 0, 1, 1), // Single pixel source
                    circleColor,
                    rotation,
                    Vector2.Zero,
                    new Vector2(length, lineThickness),
                    SpriteEffects.None,
                    0
                );

                drawInfo.DrawDataCache.Add(lineSegment);
            }

            // Also draw a filled semi-transparent circle for more visibility
            DrawFilledCircle(drawInfo, playerScreenPos, circleRadius, baseColor * 0.1f * fillRatio, fillRatio);
        }

        /// <summary>
        /// Draw a filled circle using multiple quads. Width scales with shield ratio.
        /// </summary>
        private void DrawFilledCircle(PlayerDrawSet drawInfo, Vector2 center, float radius, Color color, float fillRatio)
        {
            Texture2D pixelTexture = TextureAssets.MagicPixel.Value;
            int segments = 16;

            for (int i = 0; i < segments; i++) {
                float angle1 = (float)i / segments * MathHelper.TwoPi;
                float angle2 = (float)(i + 1) / segments * MathHelper.TwoPi;

                // Draw triangle from center to two edge points (approximated as line)
                Vector2 edge1 = center + new Vector2(
                    (float)Math.Cos(angle1) * radius,
                    (float)Math.Sin(angle1) * radius
                );
                Vector2 edge2 = center + new Vector2(
                    (float)Math.Cos(angle2) * radius,
                    (float)Math.Sin(angle2) * radius
                );

                // Draw line from center outward
                Vector2 midpoint = (edge1 + edge2) / 2f;
                Vector2 direction = midpoint - center;
                float length = direction.Length();
                float rotation = (float)Math.Atan2(direction.Y, direction.X);

                // Triangle width scales with shield remaining
                float triangleWidth = radius * 0.4f * fillRatio;

                DrawData segment = new DrawData(
                    pixelTexture,
                    center,
                    new Rectangle(0, 0, 1, 1),
                    color,
                    rotation,
                    Vector2.Zero,
                    new Vector2(length, triangleWidth),
                    SpriteEffects.None,
                    0
                );

                drawInfo.DrawDataCache.Add(segment);
            }
        }

        /// <summary>
        /// Draw shield HP text indicator near the player.
        /// Done via regular drawing since text needs special handling.
        /// </summary>
        private void DrawShieldHPIndicator(Player player, int shieldHP, bool isGoldTier)
        {
            // This needs to be drawn in a different way since DrawData doesn't support text easily
            // We'll use the DrawEffects approach in ArmorSetBonusPlayer instead
            // For now, the combat text showing absorbed damage serves as feedback
        }
    }
}