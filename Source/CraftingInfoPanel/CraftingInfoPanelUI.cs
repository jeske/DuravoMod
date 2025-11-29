
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
public class CraftingInfoPanelUI : UIState
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

    /// <summary>Get the current tab's layout</summary>
    private PanelPositionCalculator<CraftingSlotInfo> CurrentTabLayout => selectedTabIndex switch {
        0 => armorTabLayout,
        1 => weaponsTabLayout,
        2 => materialsTabLayout,
        3 => furnitureTabLayout,
        _ => armorTabLayout
    };

    // ===========================================
    // ARMOR TAB DATA
    // ===========================================

    private readonly int[] armorMaterialTierItemIds = {
        ItemID.CopperBar, ItemID.TinBar, ItemID.IronBar, ItemID.LeadBar,
        ItemID.SilverBar, ItemID.TungstenBar, ItemID.GoldBar, ItemID.PlatinumBar
    };

    private readonly int[,] armorItemGrid = {
        // Helmets
        { ItemID.CopperHelmet, ItemID.TinHelmet, ItemID.IronHelmet, ItemID.LeadHelmet,
          ItemID.SilverHelmet, ItemID.TungstenHelmet, ItemID.GoldHelmet, ItemID.PlatinumHelmet },
        // Chestplates
        { ItemID.CopperChainmail, ItemID.TinChainmail, ItemID.IronChainmail, ItemID.LeadChainmail,
          ItemID.SilverChainmail, ItemID.TungstenChainmail, ItemID.GoldChainmail, ItemID.PlatinumChainmail },
        // Greaves
        { ItemID.CopperGreaves, ItemID.TinGreaves, ItemID.IronGreaves, ItemID.LeadGreaves,
          ItemID.SilverGreaves, ItemID.TungstenGreaves, ItemID.GoldGreaves, ItemID.PlatinumGreaves }
    };

    // Accessories section (right side of armor tab)
    private readonly int[] accessoryMaterialTierItemIds = {
        ItemID.CopperBar, ItemID.TinBar, ItemID.SilverBar, ItemID.GoldBar, ItemID.PlatinumBar
    };

    private readonly int[] watchItemIds = {
        ItemID.CopperWatch, ItemID.TinWatch, ItemID.SilverWatch, ItemID.GoldWatch, ItemID.PlatinumWatch
    };

    private readonly int[] chandelierFromBarItemIds = {
        ItemID.CopperChandelier, ItemID.TinChandelier, ItemID.SilverChandelier,
        ItemID.GoldChandelier, ItemID.PlatinumChandelier
    };

    // ===========================================
    // WEAPONS TAB DATA
    // ===========================================

    private readonly int[] weaponMaterialTierItemIds = {
        ItemID.Wood, ItemID.CopperBar, ItemID.TinBar, ItemID.IronBar, ItemID.LeadBar,
        ItemID.SilverBar, ItemID.TungstenBar, ItemID.GoldBar, ItemID.PlatinumBar
    };

    private readonly int[] swordItemIds = {
        ItemID.WoodenSword, ItemID.CopperBroadsword, ItemID.TinBroadsword,
        ItemID.IronBroadsword, ItemID.LeadBroadsword, ItemID.SilverBroadsword,
        ItemID.TungstenBroadsword, ItemID.GoldBroadsword, ItemID.PlatinumBroadsword
    };

    private readonly int[] bowItemIds = {
        ItemID.WoodenBow, ItemID.CopperBow, ItemID.TinBow, ItemID.IronBow, ItemID.LeadBow,
        ItemID.SilverBow, ItemID.TungstenBow, ItemID.GoldBow, ItemID.PlatinumBow
    };

    private readonly int[] pickaxeItemIds = {
        -1, ItemID.CopperPickaxe, ItemID.TinPickaxe, ItemID.IronPickaxe, ItemID.LeadPickaxe,
        ItemID.SilverPickaxe, ItemID.TungstenPickaxe, ItemID.GoldPickaxe, ItemID.PlatinumPickaxe
    };

    private readonly int[] axeItemIds = {
        -1, ItemID.CopperAxe, ItemID.TinAxe, ItemID.IronAxe, ItemID.LeadAxe,
        ItemID.SilverAxe, ItemID.TungstenAxe, ItemID.GoldAxe, ItemID.PlatinumAxe
    };

    private readonly int[] hammerItemIds = {
        ItemID.WoodenHammer, ItemID.CopperHammer, ItemID.TinHammer, ItemID.IronHammer,
        ItemID.LeadHammer, ItemID.SilverHammer, ItemID.TungstenHammer, ItemID.GoldHammer,
        ItemID.PlatinumHammer
    };

    // ===========================================
    // MATERIALS TAB DATA
    // ===========================================

    private readonly int[] barItemIds = {
        ItemID.CopperBar, ItemID.TinBar, ItemID.IronBar, ItemID.LeadBar,
        ItemID.SilverBar, ItemID.TungstenBar, ItemID.GoldBar, ItemID.PlatinumBar
    };

    private readonly int[] brickItemIds = {
        ItemID.CopperBrick, ItemID.TinBrick, ItemID.IronBrick, ItemID.LeadBrick,
        ItemID.SilverBrick, ItemID.TungstenBrick, ItemID.GoldBrick, ItemID.PlatinumBrick
    };

    private readonly int[] miscMaterialItemIds = {
        ItemID.Torch, ItemID.Rope, ItemID.Chain, ItemID.Glass, ItemID.Bottle
    };

    private readonly int[] craftingStationItemIds = {
        ItemID.Furnace, ItemID.IronAnvil, ItemID.LeadAnvil, ItemID.Sawmill,
        ItemID.Loom, ItemID.Hellforge, ItemID.AlchemyTable, ItemID.TinkerersWorkshop,
        ItemID.ImbuingStation
    };

    // ===========================================
    // FURNITURE TAB DATA
    // ===========================================

    // Wood types (rows): Wood, Boreal, Palm, Rich Mahogany, Ebonwood, Shadewood, Pearlwood, Spooky
    // Furniture pieces (columns): Work Bench, Door, Table, Chair, Bed, Platform, Chest, Candle, Chandelier, Clock

    private readonly int[,] furnitureItemGrid = {
        // Wood
        { ItemID.WorkBench, ItemID.WoodenDoor, ItemID.WoodenTable, ItemID.WoodenChair,
          ItemID.Bed, ItemID.WoodPlatform, ItemID.Chest, ItemID.Candle,
          ItemID.Chandelier, ItemID.GrandfatherClock },
        // Boreal
        { ItemID.BorealWoodWorkBench, ItemID.BorealWoodDoor, ItemID.BorealWoodTable, ItemID.BorealWoodChair,
          ItemID.BorealWoodBed, ItemID.BorealWoodPlatform, ItemID.BorealWoodChest, ItemID.BorealWoodCandle,
          ItemID.BorealWoodChandelier, ItemID.BorealWoodClock },
        // Palm
        { ItemID.PalmWoodWorkBench, ItemID.PalmWoodDoor, ItemID.PalmWoodTable, ItemID.PalmWoodChair,
          ItemID.PalmWoodBed, ItemID.PalmWoodPlatform, ItemID.PalmWoodChest, ItemID.PalmWoodCandle,
          ItemID.PalmWoodChandelier, ItemID.PalmWoodClock },
        // Rich Mahogany
        { ItemID.RichMahoganyWorkBench, ItemID.RichMahoganyDoor, ItemID.RichMahoganyTable, ItemID.RichMahoganyChair,
          ItemID.RichMahoganyBed, ItemID.RichMahoganyPlatform, ItemID.RichMahoganyChest, ItemID.RichMahoganyCandle,
          ItemID.RichMahoganyChandelier, ItemID.RichMahoganyClock },
        // Ebonwood
        { ItemID.EbonwoodWorkBench, ItemID.EbonwoodDoor, ItemID.EbonwoodTable, ItemID.EbonwoodChair,
          ItemID.EbonwoodBed, ItemID.EbonwoodPlatform, ItemID.EbonwoodChest, ItemID.EbonwoodCandle,
          ItemID.EbonwoodChandelier, ItemID.EbonwoodClock },
        // Shadewood
        { ItemID.ShadewoodWorkBench, ItemID.ShadewoodDoor, ItemID.ShadewoodTable, ItemID.ShadewoodChair,
          ItemID.ShadewoodBed, ItemID.ShadewoodPlatform, ItemID.ShadewoodChest, ItemID.ShadewoodCandle,
          ItemID.ShadewoodChandelier, ItemID.ShadewoodClock },
        // Pearlwood
        { ItemID.PearlwoodWorkBench, ItemID.PearlwoodDoor, ItemID.PearlwoodTable, ItemID.PearlwoodChair,
          ItemID.PearlwoodBed, ItemID.PearlwoodPlatform, ItemID.PearlwoodChest, ItemID.PearlwoodCandle,
          ItemID.PearlwoodChandelier, ItemID.PearlwoodClock },
        // Spooky
        { ItemID.SpookyWorkBench, ItemID.SpookyDoor, ItemID.SpookyTable, ItemID.SpookyChair,
          ItemID.SpookyBed, ItemID.SpookyPlatform, ItemID.SpookyChest, ItemID.SpookyCandle,
          ItemID.SpookyChandelier, ItemID.SpookyClock }
    };

    // Wood type header items (for row labels)
    private readonly int[] woodTypeItemIds = {
        ItemID.Wood, ItemID.BorealWood, ItemID.PalmWood, ItemID.RichMahogany,
        ItemID.Ebonwood, ItemID.Shadewood, ItemID.Pearlwood, ItemID.SpookyWood
    };

    public override void OnInitialize()
    {
        // Build all tab layouts
        BuildArmorTabLayout();
        BuildWeaponsTabLayout();
        BuildMaterialsTabLayout();
        BuildFurnitureTabLayout();

        // Create main panel container - will be updated in Draw based on selected tab
        panelContainer = new UIElement();
        UpdatePanelSize();

        Append(panelContainer);
    }

    private void UpdatePanelSize()
    {
        var currentLayout = CurrentTabLayout;
        int contentWidth = currentLayout.CalculatedWidth;
        int contentHeight = currentLayout.CalculatedHeight;
        int panelWidth = TAB_AREA_WIDTH + contentWidth;
        int panelHeight = contentHeight + 10;

        panelContainer.Width.Set(panelWidth, 0f);
        panelContainer.Height.Set(panelHeight, 0f);
        panelContainer.HAlign = 0.5f;
        panelContainer.VAlign = 1.0f;
        panelContainer.Top.Set(-20, 0f);
    }

    /// <summary>
    /// Build the armor tab layout.
    /// Left section: 8 ore tiers × (header + 3 armor pieces)
    /// Right section: 5 tiers × (header + watches + chandeliers)
    /// </summary>
    private void BuildArmorTabLayout()
    {
        armorTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        // ===== LEFT SECTION: Armor =====
        int leftColumnCount = armorMaterialTierItemIds.Length;  // 8 columns

        // Header row
        for (int col = 0; col < leftColumnCount; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, 0, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(armorMaterialTierItemIds[col], isHeader: true));
        }

        // Armor rows
        for (int row = 0; row < 3; row++) {
            int slotY = (SLOT_SIZE + SLOT_SPACING) + 4 + row * (SLOT_SIZE + SLOT_SPACING);
            for (int col = 0; col < leftColumnCount; col++) {
                int slotX = col * (SLOT_SIZE + SLOT_SPACING);
                armorTabLayout.AddElement(slotX, slotY, SLOT_SIZE, SLOT_SIZE,
                    new CraftingSlotInfo(armorItemGrid[row, col], isHeader: false));
            }
        }

        // ===== RIGHT SECTION: Accessories =====
        int rightColumnCount = accessoryMaterialTierItemIds.Length;  // 5 columns
        int rightSectionX = leftColumnCount * (SLOT_SIZE + SLOT_SPACING) + 20;  // Gap between sections

        // Header row
        for (int col = 0; col < rightColumnCount; col++) {
            int slotX = rightSectionX + col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, 0, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(accessoryMaterialTierItemIds[col], isHeader: true));
        }

        // Watches row
        int watchRowY = SLOT_SIZE + SLOT_SPACING + 4;
        for (int col = 0; col < rightColumnCount; col++) {
            int slotX = rightSectionX + col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, watchRowY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(watchItemIds[col], isHeader: false));
        }

        // Chandeliers row
        int chandelierRowY = watchRowY + SLOT_SIZE + SLOT_SPACING;
        for (int col = 0; col < rightColumnCount; col++) {
            int slotX = rightSectionX + col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, chandelierRowY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(chandelierFromBarItemIds[col], isHeader: false));
        }
    }

    /// <summary>
    /// Build the weapons tab layout.
    /// 9 columns (Wood + 8 ores) × 6 rows (header + 5 weapon types)
    /// </summary>
    private void BuildWeaponsTabLayout()
    {
        weaponsTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int columnCount = weaponMaterialTierItemIds.Length;  // 9 columns

        // Header row
        for (int col = 0; col < columnCount; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            weaponsTabLayout.AddElement(slotX, 0, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(weaponMaterialTierItemIds[col], isHeader: true));
        }

        int[][] weaponRows = { swordItemIds, bowItemIds, pickaxeItemIds, axeItemIds, hammerItemIds };

        for (int row = 0; row < weaponRows.Length; row++) {
            int slotY = (SLOT_SIZE + SLOT_SPACING) + 4 + row * (SLOT_SIZE + SLOT_SPACING);
            for (int col = 0; col < columnCount; col++) {
                int itemId = weaponRows[row][col];
                if (itemId > 0) {  // Skip -1 entries (empty slots)
                    int slotX = col * (SLOT_SIZE + SLOT_SPACING);
                    weaponsTabLayout.AddElement(slotX, slotY, SLOT_SIZE, SLOT_SIZE,
                        new CraftingSlotInfo(itemId, isHeader: false));
                }
            }
        }
    }

    /// <summary>
    /// Build the materials tab layout.
    /// Bars, Bricks, misc items, crafting stations
    /// </summary>
    private void BuildMaterialsTabLayout()
    {
        materialsTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int currentY = 0;

        // Bars row
        for (int col = 0; col < barItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(barItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING;

        // Bricks row
        for (int col = 0; col < brickItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(brickItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 10;  // Extra gap

        // Misc materials row
        for (int col = 0; col < miscMaterialItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(miscMaterialItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 10;  // Extra gap

        // Crafting stations row
        for (int col = 0; col < craftingStationItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(craftingStationItemIds[col], isHeader: false));
        }
    }

    /// <summary>
    /// Build the furniture tab layout.
    /// 8 rows (wood types) × 10 columns (furniture pieces)
    /// </summary>
    private void BuildFurnitureTabLayout()
    {
        furnitureTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int rowCount = woodTypeItemIds.Length;  // 8 rows
        int columnCount = furnitureItemGrid.GetLength(1);  // 10 columns

        for (int row = 0; row < rowCount; row++) {
            int slotY = row * (SLOT_SIZE + SLOT_SPACING);
            for (int col = 0; col < columnCount; col++) {
                int slotX = col * (SLOT_SIZE + SLOT_SPACING);
                int itemId = furnitureItemGrid[row, col];
                if (itemId > 0) {
                    furnitureTabLayout.AddElement(slotX, slotY, SLOT_SIZE, SLOT_SIZE,
                        new CraftingSlotInfo(itemId, isHeader: false));
                }
            }
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

        // Block game input when mouse is over panel (including tabs)
        Rectangle panelHitArea = new Rectangle(
            (int)panelTopLeft.X,
            (int)panelTopLeft.Y,
            (int)panelDimensions.Width,
            (int)panelDimensions.Height
        );
        if (panelHitArea.Contains(Main.mouseX, Main.mouseY)) {
            Main.LocalPlayer.mouseInterface = true;
        }

        // Draw content area background
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
            if (selectedTabIndex != tabIndex) {
                selectedTabIndex = tabIndex;
                UpdatePanelSize();
            }
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

