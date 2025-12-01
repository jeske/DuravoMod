// MIT Licensed - Copyright (c) 2025 David W. Jeske
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using DuravoQOLMod.ArmorRebalance;

namespace DuravoQOLMod.ArmorRebalance
{
    /// <summary>
    /// Handles rendering of ore sparkles after tiles are drawn.
    /// This ensures sparkles appear in world space correctly.
    /// </summary>
    public class SparkleRendererSystem : ModSystem
    {
        public override void PostDrawTiles()
        {
            // Get the spriteBatch and begin drawing in screen coordinates
            SpriteBatch spriteBatch = Main.spriteBatch;

            // Begin spriteBatch with additive blending for glow effect
            // Using TransformationMatrix to handle zoom and camera properly
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.LinearClamp, // Smooth scaling for small sparkles
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            // Draw all active sparkles
            ArmorSetBonusPlayer.DrawSparkles(spriteBatch);

            spriteBatch.End();
        }
    }
}