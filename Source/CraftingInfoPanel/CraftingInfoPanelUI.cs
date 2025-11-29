
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    /// <summary>Tab labels</summary>
    private readonly string[] tabNames = { "Armor", "Weapons", "Materials", "Furniture" };

    /// <summary>Position calculators for each tab</summary>
    private PanelPositionCalculator<CraftingSlotInfo> armorTabLayout = null!;
    private PanelPositionCalculator<CraftingSlotInfo> weaponsTabLayout = null!;
    private PanelPositionCalculator<CraftingSlotInfo> materialsTabLayout = null!;
    private PanelPositionCalculator<CraftingSlotInfo> furnitureTabLayout = null!;

    /// <summary>Fixed panel dimensions based on largest tab (prevents jumping when switching)</summary>
    private int maxContentWidth;
    private int maxContentHeight;

    /// <summary>Get the current tab's layout</summary>
    private PanelPositionCalculator<CraftingSlotInfo> CurrentTabLayout => selectedTabIndex switch {
        0 => armorTabLayout,
        1 => weaponsTabLayout,
        2 => materialsTabLayout,
        3 => furnitureTabLayout,
        _ => armorTabLayout
    };

    public override void OnInitialize()
    {
        // Build all tab layouts
        BuildArmorTabLayout();
        BuildWeaponsTabLayout();
        BuildMaterialsTabLayout();
        BuildFurnitureTabLayout();

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
            System.Math.Max(materialsTabLayout.CalculatedWidth, furnitureTabLayout.CalculatedWidth)));

        maxContentHeight = System.Math.Max(armorTabLayout.CalculatedHeight,
            System.Math.Max(weaponsTabLayout.CalculatedHeight,
            System.Math.Max(materialsTabLayout.CalculatedHeight, furnitureTabLayout.CalculatedHeight)));
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

        Color tabBgColor = isSelected
            ? new Color(74, 58, 42, 255)
            : new Color(58, 58, 90, 255);
        Color tabBorderColor = isSelected
            ? new Color(138, 106, 74)
            : new Color(90, 90, 122);

        Texture2D pixel = TextureAssets.MagicPixel.Value;

        int tabWidth = 40;
        int tabHeight = 40;

        Rectangle tabRect = new Rectangle((int)x, (int)y, tabWidth, tabHeight);
        spriteBatch.Draw(pixel, tabRect, tabBgColor);

        spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, tabWidth, 2), tabBorderColor);
        spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y + tabHeight - 2, tabWidth, 2), tabBorderColor);
        spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, 2, tabHeight), tabBorderColor);

        string tabLabel = tabNames[tabIndex].Substring(0, 1);
        Vector2 textPos = new Vector2(x + tabWidth / 2, y + tabHeight / 2);
        Color textColor = isSelected ? new Color(240, 192, 96) : new Color(170, 170, 170);
        Utils.DrawBorderString(spriteBatch, tabLabel, textPos, textColor, 1f, 0.5f, 0.5f);

        // Handle click
        if (tabRect.Contains(Main.mouseX, Main.mouseY) && Main.mouseLeft && Main.mouseLeftRelease) {
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

            Main.hoverItemName = BuildItemTooltip(slot.ItemId, slot.IsHeader);

            if (Main.mouseLeft && Main.mouseLeftRelease) {
                FocusRecipeForItem(slot.ItemId);
                Main.mouseLeftRelease = false;
            }
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

    private string BuildItemTooltip(int itemId, bool isHeader)
    {
        System.Text.StringBuilder tooltipBuilder = new System.Text.StringBuilder();

        string itemName = Lang.GetItemNameValue(itemId);
        tooltipBuilder.AppendLine(itemName);

        Recipe? foundRecipe = null;
        for (int recipeIndex = 0; recipeIndex < Recipe.numRecipes; recipeIndex++) {
            Recipe recipe = Main.recipe[recipeIndex];
            if (recipe.createItem.type == itemId) {
                foundRecipe = recipe;
                break;
            }
        }

        if (foundRecipe != null) {
            tooltipBuilder.AppendLine("");
            tooltipBuilder.AppendLine("Recipe:");

            foreach (Item requiredItem in foundRecipe.requiredItem) {
                if (requiredItem.type == ItemID.None) {
                    break;
                }
                string ingredientName = Lang.GetItemNameValue(requiredItem.type);
                int playerHas = CountPlayerItems(requiredItem.type);
                tooltipBuilder.AppendLine($"  {ingredientName}: {playerHas}/{requiredItem.stack}");
            }

            if (foundRecipe.requiredTile.Count > 0 && foundRecipe.requiredTile[0] != -1) {
                tooltipBuilder.AppendLine("");
                tooltipBuilder.Append("Requires: ");
                bool firstTile = true;
                foreach (int tileId in foundRecipe.requiredTile) {
                    if (tileId == -1) {
                        break;
                    }
                    if (!firstTile) {
                        tooltipBuilder.Append(", ");
                    }
                    tooltipBuilder.Append(GetCraftingStationName(tileId));
                    firstTile = false;
                }
            }
        }

        return tooltipBuilder.ToString().TrimEnd();
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
