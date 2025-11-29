
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using DuravoQOLMod.Source.CraftingInfoPanel;

namespace DuravoQOLMod.CraftingInfoPanel;

/// <summary>
/// Payload for each slot in the crafting panel.
/// </summary>
public struct CraftingSlotInfo
{
    public int ItemId;
    public bool IsHeader;

    public CraftingSlotInfo(int itemId, bool isHeader = false)
    {
        ItemId = itemId;
        IsHeader = isHeader;
    }
}

/// <summary>
/// The main Crafting Info Panel UI state.
/// Shows a fixed grid of craftable items organized by material tier.
/// Position: Bottom center of screen, below vanilla crafting grid.
/// </summary>
public partial class CraftingInfoPanelUI : UIState
{
    /// <summary>The main panel container element</summary>
    private UIElement panelContainer = null!;

    /// <summary>Tab area width (tabs hang off the left side)</summary>
    private const int TAB_AREA_WIDTH = 50;

    /// <summary>Slot size in pixels (Terraria standard is ~44)</summary>
    private const int SLOT_SIZE = 40;
    private const int SLOT_SPACING = 4;

    /// <summary>Currently selected tab index</summary>
    private int selectedTabIndex = 0;

    /// <summary>Tab labels localization keys (for tooltips)</summary>
    private readonly string[] tabNameKeys = { "Armor", "Weapons", "Materials", "Furniture1", "Furniture2" };
    
    /// <summary>Get localized tab name</summary>
    private string GetTabName(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabNameKeys.Length) {
            return "";
        }
        return Language.GetTextValue($"Mods.DuravoQOLMod.CraftingPanel.TabNames.{tabNameKeys[tabIndex]}");
    }
    
    /// <summary>Tab icon item IDs</summary>
    private readonly int[] tabIconItemIds = {
        ItemID.LeadChainmail,    // Armor tab
        ItemID.LeadBow,          // Weapons tab
        ItemID.Torch,            // Materials tab
        ItemID.WorkBench,        // Furniture 1 tab
        ItemID.SpookyWorkBench   // Furniture 2 tab
    };

    /// <summary>Position calculators for each tab</summary>
    private PanelPositionCalculator<CraftingSlotInfo> armorTabLayout = null!;
    private PanelPositionCalculator<CraftingSlotInfo> weaponsTabLayout = null!;
    private PanelPositionCalculator<CraftingSlotInfo> materialsTabLayout = null!;
    private PanelPositionCalculator<CraftingSlotInfo> furniture1TabLayout = null!;
    private PanelPositionCalculator<CraftingSlotInfo> furniture2TabLayout = null!;
    
    /// <summary>Hardmode layout calculators for Armor and Weapons tabs</summary>
    private PanelPositionCalculator<CraftingSlotInfo> hardmodeArmorTabLayout = null!;
    private PanelPositionCalculator<CraftingSlotInfo> hardmodeWeaponsTabLayout = null!;

    /// <summary>Fixed panel dimensions based on largest tab (prevents jumping when switching)</summary>
    private int maxContentWidth;
    private int maxContentHeight;

    /// <summary>Get the current tab's layout (respects hardmode toggle for tabs 0 and 1)</summary>
    private PanelPositionCalculator<CraftingSlotInfo> CurrentTabLayout {
        get {
            bool useHardmode = CraftingPanelPlayer.LocalPlayerHardmodeToggleActive;
            return selectedTabIndex switch {
                0 => useHardmode ? hardmodeArmorTabLayout : armorTabLayout,
                1 => useHardmode ? hardmodeWeaponsTabLayout : weaponsTabLayout,
                2 => materialsTabLayout,
                3 => furniture1TabLayout,
                4 => furniture2TabLayout,
                _ => armorTabLayout
            };
        }
    }

    public override void OnInitialize()
    {
        // Build all tab layouts (pre-hardmode)
        BuildArmorTabLayout();
        BuildWeaponsTabLayout();
        BuildMaterialsTabLayout();
        BuildFurniture1TabLayout();
        BuildFurniture2TabLayout();
        
        // Build hardmode layouts
        BuildHardmodeArmorTabLayout();
        BuildHardmodeWeaponsTabLayout();

        // Calculate maximum dimensions across all tabs for fixed positioning
        CalculateMaxPanelDimensions();

        // Create main panel container with fixed size
        panelContainer = new UIElement();
        int panelWidth = TAB_AREA_WIDTH + maxContentWidth;
        int panelHeight = maxContentHeight + 10;

        panelContainer.Width.Set(panelWidth, 0f);
        panelContainer.Height.Set(panelHeight, 0f);
        panelContainer.HAlign = 0.5f;
        panelContainer.VAlign = 1.0f;
        panelContainer.Top.Set(-20, 0f);

        Append(panelContainer);
    }

    /// <summary>
    /// Calculate the maximum width and height across all tab layouts (including hardmode).
    /// This ensures the panel stays in a fixed position regardless of which tab is selected.
    /// </summary>
    private void CalculateMaxPanelDimensions()
    {
        // Include hardmode layouts in the max calculation
        int[] allWidths = {
            armorTabLayout.CalculatedWidth, weaponsTabLayout.CalculatedWidth,
            materialsTabLayout.CalculatedWidth, furniture1TabLayout.CalculatedWidth,
            furniture2TabLayout.CalculatedWidth,
            hardmodeArmorTabLayout.CalculatedWidth, hardmodeWeaponsTabLayout.CalculatedWidth
        };
        
        int[] allHeights = {
            armorTabLayout.CalculatedHeight, weaponsTabLayout.CalculatedHeight,
            materialsTabLayout.CalculatedHeight, furniture1TabLayout.CalculatedHeight,
            furniture2TabLayout.CalculatedHeight,
            hardmodeArmorTabLayout.CalculatedHeight, hardmodeWeaponsTabLayout.CalculatedHeight
        };
        
        maxContentWidth = 0;
        maxContentHeight = 0;
        foreach (int width in allWidths) {
            if (width > maxContentWidth) maxContentWidth = width;
        }
        foreach (int height in allHeights) {
            if (height > maxContentHeight) maxContentHeight = height;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        // Get panel position in screen coordinates
        CalculatedStyle panelDimensions = panelContainer.GetDimensions();
        Vector2 panelTopLeft = new Vector2(panelDimensions.X, panelDimensions.Y);

        // Get current tab layout
        var currentLayout = CurrentTabLayout;

        // Calculate content area position (to the right of tabs)
        int contentScreenX = (int)(panelTopLeft.X + TAB_AREA_WIDTH);
        int contentScreenY = (int)(panelTopLeft.Y + 5);

        // Update layout screen position
        currentLayout.SetScreenPosition(contentScreenX, contentScreenY);

        // Block game input when mouse is over panel (use actual current tab dimensions, not max)
        int actualPanelWidth = TAB_AREA_WIDTH + currentLayout.CalculatedWidth;
        int actualPanelHeight = currentLayout.CalculatedHeight + 10;
        Rectangle panelHitArea = new Rectangle(
            (int)panelTopLeft.X,
            (int)panelTopLeft.Y,
            actualPanelWidth,
            actualPanelHeight
        );
        if (panelHitArea.Contains(Main.mouseX, Main.mouseY)) {
            Main.LocalPlayer.mouseInterface = true;
        }

        // Draw content area background (use current tab's dimensions for the visible border)
        DrawContentBackground(spriteBatch, contentScreenX, contentScreenY,
            currentLayout.CalculatedWidth, currentLayout.CalculatedHeight);

        // Draw vertical tabs on left side
        float tabY = panelTopLeft.Y + 10;
        for (int tabIndex = 0; tabIndex < tabNameKeys.Length; tabIndex++) {
            DrawTab(spriteBatch, panelTopLeft.X + 5, tabY, tabIndex);
            tabY += 44;
        }

        // Draw content for selected tab
        DrawTabContent(spriteBatch, currentLayout);
        
        // Draw hardmode toggle button (only for Armor and Weapons tabs when applicable)
        if (selectedTabIndex == 0 || selectedTabIndex == 1) {
            bool shouldShowHardmodeButton = ShouldShowHardmodeButton();
            
            // Safety check: if hardmode is active but button shouldn't show, turn it off
            if (CraftingPanelPlayer.LocalPlayerHardmodeToggleActive && !shouldShowHardmodeButton) {
                CraftingPanelPlayer.SetLocalPlayerHardmodeToggle(false);
            }
            
            if (shouldShowHardmodeButton) {
                DrawHardmodeToggleButton(spriteBatch, contentScreenX, contentScreenY);
            }
        }
    }

    private void DrawContentBackground(SpriteBatch spriteBatch, int x, int y, int width, int height)
    {
        Texture2D pixelTexture = TextureAssets.MagicPixel.Value;

        Color backgroundColor = new Color(20, 20, 40, 180);
        Rectangle backgroundRect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(pixelTexture, backgroundRect, backgroundColor);

        Color borderColor = new Color(60, 60, 100, 200);
        int borderWidth = 2;

        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, width, borderWidth), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y + height - borderWidth, width, borderWidth), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, borderWidth, height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(x + width - borderWidth, y, borderWidth, height), borderColor);
    }

    private void DrawTab(SpriteBatch spriteBatch, float x, float y, int tabIndex)
    {
        bool isSelected = tabIndex == selectedTabIndex;

        // Active tab: lighter (current inactive style), Inactive: much darker
        Color tabBgColor = isSelected
            ? new Color(39, 39, 62, 255)     // Active: lighter blue-purple (toned down ~35% from original)
            : new Color(25, 25, 40, 255);    // Inactive: much darker
        Color tabBorderColor = isSelected
            ? new Color(90, 90, 122)         // Active: visible border
            : new Color(40, 40, 60);         // Inactive: subdued border

        Texture2D pixel = TextureAssets.MagicPixel.Value;

        int tabWidth = 40;
        int tabHeight = 40;

        Rectangle tabRect = new Rectangle((int)x, (int)y, tabWidth, tabHeight);
        spriteBatch.Draw(pixel, tabRect, tabBgColor);

        spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, tabWidth, 2), tabBorderColor);
        spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y + tabHeight - 2, tabWidth, 2), tabBorderColor);
        spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, 2, tabHeight), tabBorderColor);

        // Draw item icon instead of letter
        int iconItemId = tabIconItemIds[tabIndex];
        Main.instance.LoadItem(iconItemId);
        Texture2D iconTexture = TextureAssets.Item[iconItemId].Value;
        
        // Scale to fit within tab (with padding)
        float maxIconSize = tabWidth - 8;
        float iconScale = 1f;
        if (iconTexture.Width > maxIconSize || iconTexture.Height > maxIconSize) {
            float scaleX = maxIconSize / iconTexture.Width;
            float scaleY = maxIconSize / iconTexture.Height;
            iconScale = System.Math.Min(scaleX, scaleY);
        }
        
        Vector2 iconCenter = new Vector2(x + tabWidth / 2f, y + tabHeight / 2f);
        Vector2 iconOrigin = new Vector2(iconTexture.Width / 2f, iconTexture.Height / 2f);
        // Active: full brightness, Inactive: dimmed
        Color iconTint = isSelected ? Color.White : new Color(100, 100, 100);
        spriteBatch.Draw(iconTexture, iconCenter, null, iconTint, 0f, iconOrigin, iconScale, SpriteEffects.None, 0f);

        // Handle click and hover tooltip
        bool isHovering = tabRect.Contains(Main.mouseX, Main.mouseY);
        if (isHovering) {
            Main.hoverItemName = GetTabName(tabIndex);
        }
        
        if (isHovering && Main.mouseLeft && Main.mouseLeftRelease) {
            selectedTabIndex = tabIndex;
            Main.mouseLeftRelease = false;
        }
    }

    private void DrawTabContent(SpriteBatch spriteBatch, PanelPositionCalculator<CraftingSlotInfo> layout)
    {
        Texture2D pixelTexture = TextureAssets.MagicPixel.Value;
        CraftingSlotInfo? hoveredSlot = null;
        Rectangle hoveredScreenBounds = Rectangle.Empty;

        // Draw all slots
        foreach (var element in layout.Elements) {
            Rectangle screenBounds = layout.GetElementScreenBounds(element.RelativeBounds);
            CraftingSlotInfo slotInfo = element.Payload;

            bool canCraft = slotInfo.IsHeader || CanCraftItem(slotInfo.ItemId);
            DrawItemSlot(spriteBatch, screenBounds, slotInfo.ItemId, slotInfo.IsHeader, canCraft);

            // Check if item is hidden (setting handled in tracker)
            bool isHidden = !slotInfo.IsHeader && !SeenItemsTracker.ShouldShowItem(slotInfo.ItemId);

            // Only allow hover on visible items
            if (!isHidden && screenBounds.Contains(Main.mouseX, Main.mouseY)) {
                hoveredSlot = slotInfo;
                hoveredScreenBounds = screenBounds;
            }
        }

        // Handle hover and click
        if (hoveredSlot.HasValue) {
            CraftingSlotInfo slot = hoveredSlot.Value;

            spriteBatch.Draw(pixelTexture, hoveredScreenBounds, Color.White * 0.2f);
            
            // Check if shift is held for native tooltip mode
            bool shiftHeld = Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift);
            
            if (shiftHeld) {
                // Shift held: Show native item tooltip only (full stats, set bonuses, etc.)
                Main.HoverItem = new Item();
                Main.HoverItem.SetDefaults(slot.ItemId);
                Main.hoverItemName = Main.HoverItem.Name;
            }
            else {
                // Default: Show crafting recipe panel only at mouse position
                DrawRecipeTooltipPanel(spriteBatch, slot.ItemId);
            }
            
            // Store the hovered item ID (for potential future use)
            CraftingPanelTooltipGlobalItem.HoveredCraftingPanelItemId = slot.ItemId;

            if (Main.mouseLeft && Main.mouseLeftRelease) {
                FocusRecipeForItem(slot.ItemId);
                Main.mouseLeftRelease = false;
            }
        }
        else {
            // Clear when not hovering our panel
            CraftingPanelTooltipGlobalItem.HoveredCraftingPanelItemId = -1;
        }
    }
    private void DrawItemSlot(SpriteBatch spriteBatch, Rectangle screenBounds, int itemId, bool isHeader, bool canCraft)
    {
        Texture2D pixelTexture = TextureAssets.MagicPixel.Value;
        float opacity = 1f;
        
        // Check if item should be hidden (setting handled in tracker)
        bool shouldHideItem = !isHeader && !SeenItemsTracker.ShouldShowItem(itemId);

        Texture2D slotTexture;
        Color slotTint;

        if (isHeader) {
            slotTexture = TextureAssets.InventoryBack5.Value;
            slotTint = new Color(150, 150, 180);
        }
        else if (shouldHideItem) {
            // Hidden item - draw empty slot
            slotTexture = TextureAssets.InventoryBack.Value;
            slotTint = new Color(60, 60, 80);
            opacity = 0.5f;
        }
        else if (canCraft) {
            slotTexture = TextureAssets.InventoryBack10.Value;
            slotTint = Color.White;
        }
        else {
            slotTexture = TextureAssets.InventoryBack.Value;
            slotTint = Color.White;
            opacity = 0.4f;
        }

        spriteBatch.Draw(slotTexture, screenBounds, slotTint * opacity);

        // Yellow border for craftable items (only if not hidden)
        if (!isHeader && !shouldHideItem && canCraft) {
            Color highlightColor = Color.Yellow;
            int borderWidth = 2;
            spriteBatch.Draw(pixelTexture, new Rectangle(screenBounds.X, screenBounds.Y, screenBounds.Width, borderWidth), highlightColor);
            spriteBatch.Draw(pixelTexture, new Rectangle(screenBounds.X, screenBounds.Bottom - borderWidth, screenBounds.Width, borderWidth), highlightColor);
            spriteBatch.Draw(pixelTexture, new Rectangle(screenBounds.X, screenBounds.Y, borderWidth, screenBounds.Height), highlightColor);
            spriteBatch.Draw(pixelTexture, new Rectangle(screenBounds.Right - borderWidth, screenBounds.Y, borderWidth, screenBounds.Height), highlightColor);
        }

        // Draw item (skip if hidden)
        if (shouldHideItem) {
            // Draw question mark or nothing for hidden items
            Utils.DrawBorderString(spriteBatch, "?",
                new Vector2(screenBounds.X + SLOT_SIZE / 2f, screenBounds.Y + SLOT_SIZE / 2f),
                new Color(100, 100, 120), 1f, 0.5f, 0.5f);
            return;
        }
        
        Main.instance.LoadItem(itemId);
        Texture2D itemTexture = TextureAssets.Item[itemId].Value;

        float maxItemSize = SLOT_SIZE - 8;
        float scale = 1f;
        if (itemTexture.Width > maxItemSize || itemTexture.Height > maxItemSize) {
            float scaleX = maxItemSize / itemTexture.Width;
            float scaleY = maxItemSize / itemTexture.Height;
            scale = System.Math.Min(scaleX, scaleY);
        }

        Vector2 itemCenter = new Vector2(screenBounds.X + SLOT_SIZE / 2, screenBounds.Y + SLOT_SIZE / 2);
        Vector2 itemOrigin = new Vector2(itemTexture.Width / 2, itemTexture.Height / 2);
        Color itemTint = (canCraft || isHeader) ? Color.White : Color.White * opacity;
        spriteBatch.Draw(itemTexture, itemCenter, null, itemTint, 0f, itemOrigin, scale, SpriteEffects.None, 0f);
    }


    /// <summary>
    /// Draws the crafting recipe info panel above where the native tooltip will appear.
    /// Native tooltip appears at mouseY + 16, so we draw above the mouse.
    /// Dense format: "Iron Bar (3), Wood (10)" and "Requires: Anvil, Loom"
    /// </summary>
    private void DrawRecipeTooltipPanel(SpriteBatch spriteBatch, int itemId)
    {
        Texture2D pixelTexture = TextureAssets.MagicPixel.Value;
        
        // Find recipe for this item
        Recipe? foundRecipe = null;
        for (int recipeIndex = 0; recipeIndex < Recipe.numRecipes; recipeIndex++) {
            if (Main.recipe[recipeIndex].createItem.type == itemId) {
                foundRecipe = Main.recipe[recipeIndex];
                break;
            }
        }
        
        if (foundRecipe == null) {
            return; // No recipe to display
        }
        
        // Build tooltip lines
        List<string> tooltipLines = new List<string>();
        
        // Get item name for header
        Item displayItem = new Item();
        displayItem.SetDefaults(itemId);
        tooltipLines.Add(displayItem.Name);
        
        // Build ingredients line: "Iron Bar (3), Wood (10)"
        List<string> ingredientParts = new List<string>();
        foreach (Item ingredient in foundRecipe.requiredItem) {
            if (ingredient.type <= ItemID.None) {
                break;
            }
            ingredientParts.Add($"{ingredient.Name} ({ingredient.stack})");
        }
        if (ingredientParts.Count > 0) {
            tooltipLines.Add(string.Join(", ", ingredientParts));
        }
        
        // Build stations line: "Requires: Anvil, Loom"
        if (foundRecipe.requiredTile.Count > 0 && foundRecipe.requiredTile[0] >= 0) {
            List<string> stationNames = new List<string>();
            foreach (int tileId in foundRecipe.requiredTile) {
                if (tileId < 0) {
                    break;
                }
                stationNames.Add(GetCraftingStationName(tileId));
            }
            if (stationNames.Count > 0) {
                string requiresFormat = Language.GetTextValue("Mods.DuravoQOLMod.CraftingPanel.Requires");
                tooltipLines.Add(string.Format(requiresFormat, string.Join(", ", stationNames)));
            }
        }
        
        if (tooltipLines.Count == 0) {
            return;
        }
        
        // Calculate panel dimensions
        float maxLineWidth = 0f;
        foreach (string line in tooltipLines) {
            // Strip color codes for width calculation
            string cleanLine = System.Text.RegularExpressions.Regex.Replace(line, @"\[c/[0-9A-Fa-f]+:([^\]]+)\]", "$1");
            Vector2 lineSize = FontAssets.MouseText.Value.MeasureString(cleanLine);
            if (lineSize.X > maxLineWidth) {
                maxLineWidth = lineSize.X;
            }
        }
        
        int panelPadding = 8;
        int lineHeight = 22;
        int panelWidth = (int)maxLineWidth + panelPadding * 2;
        int panelHeight = tooltipLines.Count * lineHeight + panelPadding * 2;
        
        // Position: same as native tooltip (upper-left at mouse + 16,16)
        int panelX = Main.mouseX + 16;
        int panelY = Main.mouseY + 16;
        
        // Keep on screen
        if (panelX + panelWidth > Main.screenWidth) {
            panelX = Main.screenWidth - panelWidth - 5;
        }
        if (panelY + panelHeight > Main.screenHeight) {
            panelY = Main.screenHeight - panelHeight - 5;
        }
        
        // Draw background
        Rectangle panelBgRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(pixelTexture, panelBgRect, new Color(20, 20, 40, 220));
        
        // Draw border
        Color borderColor = new Color(80, 80, 120);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelX, panelY, panelWidth, 2), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelX, panelY + panelHeight - 2, panelWidth, 2), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelX, panelY, 2, panelHeight), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelX + panelWidth - 2, panelY, 2, panelHeight), borderColor);
        
        // Draw text lines - all standard light grey
        Color standardGrey = new Color(190, 190, 190);
        float textY = panelY + panelPadding;
        foreach (string line in tooltipLines) {
            Utils.DrawBorderString(spriteBatch, line, new Vector2(panelX + panelPadding, textY), standardGrey);
            textY += lineHeight;
        }
    }

    private string GetCraftingStationName(int tileId)
    {
        return tileId switch {
            TileID.WorkBenches => "Work Bench",
            TileID.Furnaces => "Furnace",
            TileID.Anvils => "Anvil",
            TileID.MythrilAnvil => "Mythril Anvil",
            TileID.Bottles => "Bottle",
            TileID.Sawmill => "Sawmill",
            TileID.Loom => "Loom",
            TileID.Chairs => "Chair",
            TileID.Tables => "Table",
            TileID.CookingPots => "Cooking Pot",
            TileID.TinkerersWorkbench => "Tinkerer's Workshop",
            TileID.DemonAltar => "Demon Altar",
            TileID.Hellforge => "Hellforge",
            _ => $"Station #{tileId}"
        };
    }

    private bool CanCraftItem(int itemId)
    {
        for (int availableIndex = 0; availableIndex < Main.numAvailableRecipes; availableIndex++) {
            int globalRecipeIndex = Main.availableRecipe[availableIndex];
            Recipe recipe = Main.recipe[globalRecipeIndex];
            if (recipe.createItem.type == itemId) {
                return true;
            }
        }
        return false;
    }

    private int CountPlayerItems(int itemType)
    {
        int count = 0;
        Player player = Main.LocalPlayer;

        for (int slotIndex = 0; slotIndex < player.inventory.Length; slotIndex++) {
            if (player.inventory[slotIndex].type == itemType) {
                count += player.inventory[slotIndex].stack;
            }
        }

        return count;
    }

    private void FocusRecipeForItem(int itemId)
    {
        for (int availableIndex = 0; availableIndex < Main.numAvailableRecipes; availableIndex++) {
            int globalRecipeIndex = Main.availableRecipe[availableIndex];
            Recipe recipe = Main.recipe[globalRecipeIndex];
            if (recipe.createItem.type == itemId) {
                Main.focusRecipe = availableIndex;
                Main.recFastScroll = true;
                break;
            }
        }
    }

    /// <summary>
    /// Draw the hardmode toggle button at the top-left corner of the content area.
    /// Only shown when Armor (tab 0) or Weapons (tab 1) are selected.
    /// </summary>
    private void DrawHardmodeToggleButton(SpriteBatch spriteBatch, int contentScreenX, int contentScreenY)
    {
        Texture2D pixelTexture = TextureAssets.MagicPixel.Value;
        
        // Button dimensions and position (top-left corner, slightly offset inside the panel)
        const int buttonPadding = 4;
        const int buttonHeight = 24; // Increased from 20 for better text fit
        string buttonText = Language.GetTextValue("Mods.DuravoQOLMod.CraftingPanel.Hardmode");
        
        // Measure text width
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(buttonText);
        int buttonWidth = (int)textSize.X + buttonPadding * 2;
        
        // Position: top-left corner of content area, offset by panel padding
        int buttonX = contentScreenX + 4;
        int buttonY = contentScreenY - buttonHeight - 4; // Above the content area
        
        Rectangle buttonRect = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);
        
        // Button background color based on active state
        bool isActive = CraftingPanelPlayer.LocalPlayerHardmodeToggleActive;
        Color buttonBgColor = isActive
            ? new Color(100, 60, 60, 220)    // Active: reddish tint (hardmode)
            : new Color(40, 40, 60, 220);    // Inactive: dark blue-purple
        Color buttonBorderColor = isActive
            ? new Color(180, 80, 80)         // Active: red border
            : new Color(60, 60, 100);        // Inactive: standard border
        Color textColor = isActive
            ? new Color(255, 180, 180)       // Active: light red text
            : new Color(150, 150, 180);      // Inactive: grey text
        
        // Draw button background
        spriteBatch.Draw(pixelTexture, buttonRect, buttonBgColor);
        
        // Draw button border
        int borderWidth = 1;
        spriteBatch.Draw(pixelTexture, new Rectangle(buttonX, buttonY, buttonWidth, borderWidth), buttonBorderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(buttonX, buttonY + buttonHeight - borderWidth, buttonWidth, borderWidth), buttonBorderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(buttonX, buttonY, borderWidth, buttonHeight), buttonBorderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(buttonX + buttonWidth - borderWidth, buttonY, borderWidth, buttonHeight), buttonBorderColor);
        
        // Draw button text (centered horizontally, pushed down a bit vertically)
        // The 0.8f scale affects the actual drawn size, so we need to account for that
        float textScale = 0.8f;
        float scaledTextHeight = textSize.Y * textScale;
        Vector2 textPosition = new Vector2(
            buttonX + (buttonWidth - textSize.X * textScale) / 2f,
            buttonY + (buttonHeight - scaledTextHeight) / 2f + 2 // +2 to push down a bit
        );
        Utils.DrawBorderString(spriteBatch, buttonText, textPosition, textColor, textScale);
        
        // Handle hover and click
        bool isHovering = buttonRect.Contains(Main.mouseX, Main.mouseY);
        if (isHovering) {
            // Block game input when hovering the button
            Main.LocalPlayer.mouseInterface = true;
            
            // Highlight on hover
            spriteBatch.Draw(pixelTexture, buttonRect, Color.White * 0.1f);
            Main.hoverItemName = isActive
                ? Language.GetTextValue("Mods.DuravoQOLMod.CraftingPanel.HardmodeTooltipOn")
                : Language.GetTextValue("Mods.DuravoQOLMod.CraftingPanel.HardmodeTooltipOff");
            
            if (Main.mouseLeft && Main.mouseLeftRelease) {
                CraftingPanelPlayer.SetLocalPlayerHardmodeToggle(!isActive);
                Main.mouseLeftRelease = false;
            }
        }
    }
    
    /// <summary>
    /// Determine if the hardmode button should be shown.
    /// Shows if: setting disabled (show all items) OR any hardmode item has been seen.
    /// </summary>
    private bool ShouldShowHardmodeButton()
    {
        // If hiding items is disabled, always show the button
        if (!DuravoQOLModConfig.EnableCraftingPanelOnlyShowSeenItems) {
            return true;
        }
        
        // Otherwise, only show if player has seen any hardmode item
        return SeenItemsTracker.HasSeenAnyHardmodeItem;
    }
}
