
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
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

    /// <summary>Tab labels (for tooltips)</summary>
    private readonly string[] tabNames = { "Armor", "Weapons", "Materials", "Furniture 1", "Furniture 2" };
    
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

    /// <summary>Fixed panel dimensions based on largest tab (prevents jumping when switching)</summary>
    private int maxContentWidth;
    private int maxContentHeight;

    /// <summary>Get the current tab's layout</summary>
    private PanelPositionCalculator<CraftingSlotInfo> CurrentTabLayout => selectedTabIndex switch {
        0 => armorTabLayout,
        1 => weaponsTabLayout,
        2 => materialsTabLayout,
        3 => furniture1TabLayout,
        4 => furniture2TabLayout,
        _ => armorTabLayout
    };

    public override void OnInitialize()
    {
        // Build all tab layouts
        BuildArmorTabLayout();
        BuildWeaponsTabLayout();
        BuildMaterialsTabLayout();
        BuildFurniture1TabLayout();
        BuildFurniture2TabLayout();

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
    /// Calculate the maximum width and height across all tab layouts.
    /// This ensures the panel stays in a fixed position regardless of which tab is selected.
    /// </summary>
    private void CalculateMaxPanelDimensions()
    {
        maxContentWidth = System.Math.Max(armorTabLayout.CalculatedWidth,
            System.Math.Max(weaponsTabLayout.CalculatedWidth,
            System.Math.Max(materialsTabLayout.CalculatedWidth,
            System.Math.Max(furniture1TabLayout.CalculatedWidth, furniture2TabLayout.CalculatedWidth))));

        maxContentHeight = System.Math.Max(armorTabLayout.CalculatedHeight,
            System.Math.Max(weaponsTabLayout.CalculatedHeight,
            System.Math.Max(materialsTabLayout.CalculatedHeight,
            System.Math.Max(furniture1TabLayout.CalculatedHeight, furniture2TabLayout.CalculatedHeight))));
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
        for (int tabIndex = 0; tabIndex < tabNames.Length; tabIndex++) {
            DrawTab(spriteBatch, panelTopLeft.X + 5, tabY, tabIndex);
            tabY += 44;
        }

        // Draw content for selected tab
        DrawTabContent(spriteBatch, currentLayout);
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
            ? new Color(58, 58, 90, 255)     // Active: lighter blue-purple
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
            Main.hoverItemName = tabNames[tabIndex];
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

            if (screenBounds.Contains(Main.mouseX, Main.mouseY)) {
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

        Texture2D slotTexture;
        Color slotTint;

        if (isHeader) {
            slotTexture = TextureAssets.InventoryBack5.Value;
            slotTint = new Color(150, 150, 180);
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

        // Yellow border for craftable items
        if (!isHeader && canCraft) {
            Color highlightColor = Color.Yellow;
            int borderWidth = 2;
            spriteBatch.Draw(pixelTexture, new Rectangle(screenBounds.X, screenBounds.Y, screenBounds.Width, borderWidth), highlightColor);
            spriteBatch.Draw(pixelTexture, new Rectangle(screenBounds.X, screenBounds.Bottom - borderWidth, screenBounds.Width, borderWidth), highlightColor);
            spriteBatch.Draw(pixelTexture, new Rectangle(screenBounds.X, screenBounds.Y, borderWidth, screenBounds.Height), highlightColor);
            spriteBatch.Draw(pixelTexture, new Rectangle(screenBounds.Right - borderWidth, screenBounds.Y, borderWidth, screenBounds.Height), highlightColor);
        }

        // Draw item
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
                tooltipLines.Add($"Requires: {string.Join(", ", stationNames)}");
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
}
