using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace DuravoQOLMod.CraftingInfoPanel
{
    /// <summary>
    /// Static class that draws the Crafting Panel toggle button.
    /// Positioned in lower-left area of screen, near vanilla crafting button.
    /// Visible whenever inventory is open.
    /// </summary>
    public static class CraftingPanelButton
    {
        /// <summary>Button dimensions</summary>
        private const int BUTTON_SIZE = 32;

        /// <summary>
        /// Position offset from lower-left corner.
        /// Button sits in the very corner of the screen.
        /// </summary>
        private const int BUTTON_LEFT_MARGIN = 10;  // Near left edge
        private const int BUTTON_BOTTOM_MARGIN = 10;  // Near bottom edge

        /// <summary>
        /// Draw the toggle button. Called each frame when inventory is open.
        /// </summary>
        public static void Draw(SpriteBatch spriteBatch)
        {
            // Calculate button position (lower-left corner)
            // Y needs to account for button size so the entire button is on screen
            float buttonX = BUTTON_LEFT_MARGIN;
            float buttonY = Main.screenHeight - BUTTON_BOTTOM_MARGIN - BUTTON_SIZE;

            Rectangle buttonRect = new Rectangle((int)buttonX, (int)buttonY, BUTTON_SIZE, BUTTON_SIZE);

            // Check if panel is currently visible
            bool isPanelVisible = CraftingPanelSystem.Instance?.IsPanelVisible ?? false;

            // Check if near a crafting station (changes button appearance)
            bool isNearStation = CraftingPanelSystem.IsNearCraftingStation();

            // Button colors - show different states
            Color buttonBgColor;
            Color buttonBorderColor;

            if (isPanelVisible) {
                // Panel open - highlighted golden
                buttonBgColor = new Color(90, 74, 42, 230);
                buttonBorderColor = new Color(240, 192, 96);
            }
            else if (isNearStation) {
                // Near station - available, slightly highlighted
                buttonBgColor = new Color(58, 58, 90, 230);
                buttonBorderColor = new Color(138, 106, 74);
            }
            else {
                // No station nearby - dimmed but still clickable
                buttonBgColor = new Color(42, 42, 58, 200);
                buttonBorderColor = new Color(90, 90, 106);
            }

            // Check hover
            bool isHovering = buttonRect.Contains(Main.mouseX, Main.mouseY);
            if (isHovering) {
                buttonBorderColor = new Color(240, 192, 96);  // Golden hover highlight
                // Block game input when mouse is over button
                Main.LocalPlayer.mouseInterface = true;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // Draw button border
            int borderWidth = 2;
            Rectangle borderRect = new Rectangle(
                (int)buttonX - borderWidth,
                (int)buttonY - borderWidth,
                BUTTON_SIZE + borderWidth * 2,
                BUTTON_SIZE + borderWidth * 2
            );
            spriteBatch.Draw(pixel, borderRect, buttonBorderColor);

            // Draw button background
            spriteBatch.Draw(pixel, buttonRect, buttonBgColor);

            // Draw button icon - "C" for Crafting or a simple hammer icon representation
            // Using text for now, could be replaced with actual texture
            string buttonLabel = "C";
            Vector2 textPosition = new Vector2(buttonX + BUTTON_SIZE / 2, buttonY + BUTTON_SIZE / 2);
            Color textColor = isPanelVisible
                ? new Color(240, 192, 96)  // Golden when active
                : (isNearStation ? Color.White : new Color(150, 150, 150));  // Dimmed when no station

            Utils.DrawBorderString(spriteBatch, buttonLabel, textPosition, textColor, 1f, 0.5f, 0.5f);

            // Draw tooltip on hover
            if (isHovering) {
                string tooltip = isPanelVisible
                    ? "Hide Crafting Panel"
                    : "Show Crafting Panel";

                if (!isNearStation) {
                    tooltip += "\n(No crafting station nearby)";
                }

                Main.hoverItemName = tooltip;
            }

            // Handle click
            if (isHovering && Main.mouseLeft && Main.mouseLeftRelease) {
                CraftingPanelSystem.Instance?.TogglePanel();
                Main.mouseLeftRelease = false;  // Consume click
            }
        }
    }
}