
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

    /// <summary>Tab area height (tabs are now horizontal at top, flush with content)</summary>
    private const int TAB_AREA_HEIGHT = 40;

    /// <summary>Slot size in pixels (Terraria standard is ~44)</summary>
    private const int SLOT_SIZE = 40;
    private const int SLOT_SPACING = 4;

    /// <summary>Tab size for horizontal tabs</summary>
    private const int TAB_WIDTH = 40;
    private const int TAB_HEIGHT = 40;
    private const int TAB_SPACING = 4;

    /// <summary>Currently selected tab index (index into visible tabs list)</summary>
    private int selectedTabIndex = 0;

    /// <summary>Pre-built tab lists - one for pre-hardmode, one for hardmode</summary>
    private static readonly List<int> preHardmodeVisibleTabIds = new List<int> {
        TAB_ID_ARMOR, TAB_ID_WEAPONS, TAB_ID_MATERIALS, TAB_ID_FURNITURE1, TAB_ID_FURNITURE2
    };
    private static readonly List<int> hardmodeVisibleTabIds = new List<int> {
        TAB_ID_HARDMODE_ARMOR, TAB_ID_HARDMODE_WEAPONS,
        TAB_ID_ARMOR, TAB_ID_WEAPONS, TAB_ID_MATERIALS, TAB_ID_FURNITURE1, TAB_ID_FURNITURE2
    };

    /// <summary>Internal tab IDs (fixed, never change)</summary>
    private const int TAB_ID_HARDMODE_ARMOR = 0;
    private const int TAB_ID_HARDMODE_WEAPONS = 1;
    private const int TAB_ID_ARMOR = 2;
    private const int TAB_ID_WEAPONS = 3;
    private const int TAB_ID_MATERIALS = 4;
    private const int TAB_ID_FURNITURE1 = 5;
    private const int TAB_ID_FURNITURE2 = 6;

    /// <summary>Tab labels localization keys indexed by internal tab ID</summary>
    private static readonly string[] allTabNameKeys = {
        "HardmodeArmor", "HardmodeWeapons", "Armor", "Weapons", "Materials", "Furniture1", "Furniture2"
    };

    /// <summary>Tab icon item IDs indexed by internal tab ID</summary>
    private static readonly int[] allTabIconItemIds = {
        ItemID.CobaltBreastplate,  // HM Armor tab (Cobalt chestplate)
        ItemID.CobaltWaraxe,       // HM Weapons tab (Cobalt waraxe)
        ItemID.LeadChainmail,      // Armor tab
        ItemID.LeadAxe,            // Weapons tab (Lead axe)
        ItemID.Torch,              // Materials tab
        ItemID.WorkBench,          // Furniture 1 tab
        ItemID.SpookyWorkBench     // Furniture 2 tab
    };

    /// <summary>Get the list of currently visible tab IDs based on hardmode discovery</summary>
    private List<int> GetVisibleTabIds()
    {
        bool showHardmodeTabs = SeenItemsTracker.HasSeenAnyHardmodeItem || !DuravoQOLModConfig.EnableCraftingPanelOnlyShowSeenItems;
        return showHardmodeTabs ? hardmodeVisibleTabIds : preHardmodeVisibleTabIds;
    }

    /// <summary>Get the internal tab ID for the currently selected tab</summary>
    private int GetSelectedTabId()
    {
        List<int> visibleTabs = GetVisibleTabIds();
        if (selectedTabIndex >= 0 && selectedTabIndex < visibleTabs.Count) {
            return visibleTabs[selectedTabIndex];
        }
        return visibleTabs[0]; // Fallback to first tab
    }

    /// <summary>Get localized tab name by internal tab ID</summary>
    private string GetTabNameById(int tabId)
    {
        if (tabId < 0 || tabId >= allTabNameKeys.Length) {
            return "";
        }
        return Language.GetTextValue($"Mods.DuravoQOLMod.CraftingPanel.TabNames.{allTabNameKeys[tabId]}");
    }

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

    /// <summary>Get the current tab's layout based on selected tab ID</summary>
    private PanelPositionCalculator<CraftingSlotInfo> CurrentTabLayout {
        get {
            int tabId = GetSelectedTabId();
            return tabId switch {
                TAB_ID_HARDMODE_ARMOR => hardmodeArmorTabLayout,
                TAB_ID_HARDMODE_WEAPONS => hardmodeWeaponsTabLayout,
                TAB_ID_ARMOR => armorTabLayout,
                TAB_ID_WEAPONS => weaponsTabLayout,
                TAB_ID_MATERIALS => materialsTabLayout,
                TAB_ID_FURNITURE1 => furniture1TabLayout,
                TAB_ID_FURNITURE2 => furniture2TabLayout,
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
        // Note: Position is set in Draw() using Main.screenHeight for accurate bottom alignment
        panelContainer = new UIElement();
        int panelWidth = maxContentWidth;
        int panelHeight = TAB_AREA_HEIGHT + maxContentHeight + 10;

        panelContainer.Width.Set(panelWidth, 0f);
        panelContainer.Height.Set(panelHeight, 0f);
        panelContainer.HAlign = 0.5f;  // Center horizontally only
        // VAlign removed - we position vertically in Draw() using Main.screenHeight directly

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

        // Get visible tabs and current layout
        List<int> visibleTabs = GetVisibleTabIds();
        var currentLayout = CurrentTabLayout;

        // Calculate panel position directly from screen dimensions
        // This avoids issues with UIState parent bounds not matching screen size
        const int PANEL_BOTTOM_MARGIN = 25;  // Extra padding from screen bottom

        int panelWidth = maxContentWidth;
        int panelHeight = TAB_AREA_HEIGHT + maxContentHeight + 10;

        // Center horizontally, position from bottom of screen
        int panelScreenX = (Main.screenWidth - panelWidth) / 2;
        int panelScreenY = Main.screenHeight - PANEL_BOTTOM_MARGIN - panelHeight;

        Vector2 panelTopLeft = new Vector2(panelScreenX, panelScreenY);

        // Calculate content area position (below tabs)
        int contentScreenX = panelScreenX;
        int contentScreenY = panelScreenY + TAB_AREA_HEIGHT;

        // Update layout screen position
        currentLayout.SetScreenPosition(contentScreenX, contentScreenY);

        // Block game input when mouse is over panel (use actual current tab dimensions, not max)
        int actualPanelWidth = currentLayout.CalculatedWidth;
        int actualPanelHeight = TAB_AREA_HEIGHT + currentLayout.CalculatedHeight + 10;
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
        // Pass selectedTabIndex and tab position so we can skip drawing border where active tab connects
        float selectedTabLeftX = panelTopLeft.X + selectedTabIndex * (TAB_WIDTH + TAB_SPACING);
        DrawContentBackground(spriteBatch, contentScreenX, contentScreenY,
            currentLayout.CalculatedWidth, currentLayout.CalculatedHeight,
            selectedTabLeftX, TAB_WIDTH);

        // Draw horizontal tabs at top
        float tabX = panelTopLeft.X;
        for (int visibleIndex = 0; visibleIndex < visibleTabs.Count; visibleIndex++) {
            int tabId = visibleTabs[visibleIndex];
            DrawTab(spriteBatch, tabX, panelTopLeft.Y, visibleIndex, tabId, contentScreenY);
            tabX += TAB_WIDTH + TAB_SPACING;
        }

        // Draw content for selected tab
        DrawTabContent(spriteBatch, currentLayout);
    }

    private void DrawContentBackground(SpriteBatch spriteBatch, int x, int y, int width, int height,
        float selectedTabLeftX, int selectedTabWidth)
    {
        Texture2D pixelTexture = TextureAssets.MagicPixel.Value;

        Color backgroundColor = new Color(20, 20, 40, 180);
        Rectangle backgroundRect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(pixelTexture, backgroundRect, backgroundColor);

        Color borderColor = new Color(60, 60, 100, 200);
        int borderWidth = 2;

        // Top border - draw in two segments, skipping where the active tab connects
        int tabConnectLeft = (int)selectedTabLeftX;
        int tabConnectRight = tabConnectLeft + selectedTabWidth;

        // Left segment of top border (before active tab)
        if (tabConnectLeft > x) {
            spriteBatch.Draw(pixelTexture, new Rectangle(x, y, tabConnectLeft - x, borderWidth), borderColor);
        }
        // Right segment of top border (after active tab)
        if (tabConnectRight < x + width) {
            spriteBatch.Draw(pixelTexture, new Rectangle(tabConnectRight, y, (x + width) - tabConnectRight, borderWidth), borderColor);
        }

        // Bottom, left, right borders
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y + height - borderWidth, width, borderWidth), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(x, y, borderWidth, height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(x + width - borderWidth, y, borderWidth, height), borderColor);
    }

    private void DrawTab(SpriteBatch spriteBatch, float x, float y, int visibleIndex, int tabId, int contentTopY)
    {
        bool isSelected = visibleIndex == selectedTabIndex;

        // Active tab: same as panel background, Inactive: darker
        Color tabBgColor = isSelected
            ? new Color(20, 20, 40, 180)     // Active: same as panel background for seamless connection
            : new Color(25, 25, 40, 255);    // Inactive: dark
        Color tabBorderColor = isSelected
            ? new Color(60, 60, 100, 200)    // Active: same as panel border
            : new Color(40, 40, 60);         // Inactive: subdued border

        Texture2D pixel = TextureAssets.MagicPixel.Value;

        Rectangle tabRect = new Rectangle((int)x, (int)y, TAB_WIDTH, TAB_HEIGHT);
        spriteBatch.Draw(pixel, tabRect, tabBgColor);

        // Draw border (horizontal tabs: no bottom border for selected tab to merge with content)
        spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, TAB_WIDTH, 2), tabBorderColor);  // Top
        if (!isSelected) {
            spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y + TAB_HEIGHT - 2, TAB_WIDTH, 2), tabBorderColor);  // Bottom (inactive only)
        }
        spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, 2, TAB_HEIGHT), tabBorderColor);  // Left
        spriteBatch.Draw(pixel, new Rectangle((int)x + TAB_WIDTH - 2, (int)y, 2, TAB_HEIGHT), tabBorderColor);  // Right

        // Draw item icon instead of letter
        int iconItemId = allTabIconItemIds[tabId];
        Main.instance.LoadItem(iconItemId);
        Texture2D iconTexture = TextureAssets.Item[iconItemId].Value;

        // Scale to fit within tab (with padding)
        float maxIconSize = TAB_WIDTH - 8;
        float iconScale = 1f;
        if (iconTexture.Width > maxIconSize || iconTexture.Height > maxIconSize) {
            float scaleX = maxIconSize / iconTexture.Width;
            float scaleY = maxIconSize / iconTexture.Height;
            iconScale = System.Math.Min(scaleX, scaleY);
        }

        Vector2 iconCenter = new Vector2(x + TAB_WIDTH / 2f, y + TAB_HEIGHT / 2f);
        Vector2 iconOrigin = new Vector2(iconTexture.Width / 2f, iconTexture.Height / 2f);
        // Active: full brightness, Inactive: dimmed
        Color iconTint = isSelected ? Color.White : new Color(100, 100, 100);
        spriteBatch.Draw(iconTexture, iconCenter, null, iconTint, 0f, iconOrigin, iconScale, SpriteEffects.None, 0f);

        // Handle click and hover tooltip
        bool isHovering = tabRect.Contains(Main.mouseX, Main.mouseY);
        if (isHovering) {
            Main.hoverItemName = GetTabNameById(tabId);
        }

        if (isHovering && Main.mouseLeft && Main.mouseLeftRelease) {
            selectedTabIndex = visibleIndex;
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

            bool canCraft = CanCraftItem(slotInfo.ItemId);
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

            // Check if alt is held for native tooltip mode
            bool altHeld = Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt);

            if (altHeld) {
                // Alt held: Show native item tooltip only (full stats, set bonuses, etc.)
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

        // Yellow border for craftable items (including headers, but not hidden items)
        if (!shouldHideItem && canCraft) {
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

        // Get item name for header
        Item displayItem = new Item();
        displayItem.SetDefaults(itemId);

        // Find recipe for this item
        Recipe? foundRecipe = null;
        for (int recipeIndex = 0; recipeIndex < Recipe.numRecipes; recipeIndex++) {
            if (Main.recipe[recipeIndex].createItem.type == itemId) {
                foundRecipe = Main.recipe[recipeIndex];
                break;
            }
        }

        // Build tooltip lines
        List<string> tooltipLines = new List<string>();
        tooltipLines.Add(displayItem.Name);

        if (foundRecipe != null) {
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
        }
        else {
            // No recipe - item cannot be crafted (boss drop, chest loot, etc.)
            string cannotCraftText = Language.GetTextValue("Mods.DuravoQOLMod.CraftingPanel.CannotBeCrafted");
            tooltipLines.Add(cannotCraftText);
        }

        // Add hint for alt-key item tooltip
        string altHintText = Language.GetTextValue("Mods.DuravoQOLMod.CraftingPanel.AltItemTooltipHint");
        tooltipLines.Add(altHintText);

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

    // Hardmode toggle button has been removed - now using separate HM Armor and HM Weapons tabs
}
