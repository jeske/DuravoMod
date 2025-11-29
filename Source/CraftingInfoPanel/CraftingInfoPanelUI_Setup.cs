using Terraria.ID;
using DuravoQOLMod.Source.CraftingInfoPanel;

namespace DuravoQOLMod.CraftingInfoPanel;

/// <summary>
/// Partial class containing item data arrays and layout builder methods.
/// Separated from drawing/interaction logic for maintainability.
/// </summary>
public partial class CraftingInfoPanelUI
{
    #region Armor Tab Data

    // Header materials for main armor grid (8 columns)
    private static readonly int[] ArmorMaterialHeaderIds = {
        ItemID.CopperBar, ItemID.TinBar, ItemID.IronBar, ItemID.LeadBar,
        ItemID.SilverBar, ItemID.TungstenBar, ItemID.GoldBar, ItemID.PlatinumBar
    };

    // Armor items: [row][column] - row 0=Helmet, 1=Chest, 2=Greaves
    private static readonly int[,] ArmorGridItemIds = {
        { ItemID.CopperHelmet, ItemID.TinHelmet, ItemID.IronHelmet, ItemID.LeadHelmet,
          ItemID.SilverHelmet, ItemID.TungstenHelmet, ItemID.GoldHelmet, ItemID.PlatinumHelmet },
        { ItemID.CopperChainmail, ItemID.TinChainmail, ItemID.IronChainmail, ItemID.LeadChainmail,
          ItemID.SilverChainmail, ItemID.TungstenChainmail, ItemID.GoldChainmail, ItemID.PlatinumChainmail },
        { ItemID.CopperGreaves, ItemID.TinGreaves, ItemID.IronGreaves, ItemID.LeadGreaves,
          ItemID.SilverGreaves, ItemID.TungstenGreaves, ItemID.GoldGreaves, ItemID.PlatinumGreaves }
    };

    // Secondary section header materials (watches/chandeliers use subset of ores)
    private static readonly int[] AccessoryMaterialHeaderIds = {
        ItemID.CopperBar, ItemID.TinBar, ItemID.SilverBar, ItemID.GoldBar, ItemID.PlatinumBar
    };

    // Watches row
    private static readonly int[] WatchItemIds = {
        ItemID.CopperWatch, ItemID.TinWatch, ItemID.SilverWatch, ItemID.GoldWatch, ItemID.PlatinumWatch
    };

    // Chandeliers row
    private static readonly int[] ChandelierItemIds = {
        ItemID.CopperChandelier, ItemID.TinChandelier, ItemID.SilverChandelier, ItemID.GoldChandelier, ItemID.PlatinumChandelier
    };

    #endregion

    #region Weapons Tab Data

    // Header materials (Wood + 8 ore bars)
    private static readonly int[] WeaponMaterialHeaderIds = {
        ItemID.Wood, ItemID.CopperBar, ItemID.TinBar, ItemID.IronBar, ItemID.LeadBar,
        ItemID.SilverBar, ItemID.TungstenBar, ItemID.GoldBar, ItemID.PlatinumBar
    };

    // Swords row (all have wood+ore versions)
    private static readonly int[] SwordItemIds = {
        ItemID.WoodenSword, ItemID.CopperBroadsword, ItemID.TinBroadsword, ItemID.IronBroadsword, ItemID.LeadBroadsword,
        ItemID.SilverBroadsword, ItemID.TungstenBroadsword, ItemID.GoldBroadsword, ItemID.PlatinumBroadsword
    };

    // Bows row
    private static readonly int[] BowItemIds = {
        ItemID.WoodenBow, ItemID.CopperBow, ItemID.TinBow, ItemID.IronBow, ItemID.LeadBow,
        ItemID.SilverBow, ItemID.TungstenBow, ItemID.GoldBow, ItemID.PlatinumBow
    };

    // Pickaxes row (no wood version, first slot empty = -1)
    private static readonly int[] PickaxeItemIds = {
        -1, ItemID.CopperPickaxe, ItemID.TinPickaxe, ItemID.IronPickaxe, ItemID.LeadPickaxe,
        ItemID.SilverPickaxe, ItemID.TungstenPickaxe, ItemID.GoldPickaxe, ItemID.PlatinumPickaxe
    };

    // Axes row (no wood version)
    private static readonly int[] AxeItemIds = {
        -1, ItemID.CopperAxe, ItemID.TinAxe, ItemID.IronAxe, ItemID.LeadAxe,
        ItemID.SilverAxe, ItemID.TungstenAxe, ItemID.GoldAxe, ItemID.PlatinumAxe
    };

    // Hammers row
    private static readonly int[] HammerItemIds = {
        ItemID.WoodenHammer, ItemID.CopperHammer, ItemID.TinHammer, ItemID.IronHammer, ItemID.LeadHammer,
        ItemID.SilverHammer, ItemID.TungstenHammer, ItemID.GoldHammer, ItemID.PlatinumHammer
    };

    #endregion

    #region Materials Tab Data

    // Metal bars row
    private static readonly int[] BarItemIds = {
        ItemID.CopperBar, ItemID.TinBar, ItemID.IronBar, ItemID.LeadBar,
        ItemID.SilverBar, ItemID.TungstenBar, ItemID.GoldBar, ItemID.PlatinumBar
    };

    // Bricks row
    private static readonly int[] BrickItemIds = {
        ItemID.CopperBrick, ItemID.TinBrick, ItemID.IronBrick, ItemID.LeadBrick,
        ItemID.SilverBrick, ItemID.TungstenBrick, ItemID.GoldBrick, ItemID.PlatinumBrick
    };

    // Misc crafting materials
    private static readonly int[] MiscMaterialItemIds = {
        ItemID.Torch, ItemID.Rope, ItemID.Chain, ItemID.Glass, ItemID.Bottle
    };

    // Crafting stations
    private static readonly int[] CraftingStationItemIds = {
        ItemID.Furnace, ItemID.IronAnvil, ItemID.LeadAnvil, ItemID.Sawmill, ItemID.Loom,
        ItemID.Hellforge, ItemID.AlchemyTable, ItemID.TinkerersWorkshop, ItemID.ImbuingStation
    };

    #endregion

    #region Furniture Tab Data

    // Wood types for furniture (row headers conceptually, but shown as first furniture piece style)
    private static readonly int[] FurnitureWoodTypes = {
        ItemID.Wood, ItemID.BorealWood, ItemID.PalmWood, ItemID.RichMahogany,
        ItemID.Ebonwood, ItemID.Shadewood, ItemID.Pearlwood, ItemID.SpookyWood
    };

    // Furniture by wood type: [woodRow][furnitureType]
    // Columns: Workbench, Door, Table, Chair, Bed, Platform, Chest, Candle, Chandelier, Clock
    private static readonly int[,] FurnitureGridItemIds = {
        // Wood (regular) - Note: No wood chandelier exists, using -1 to skip
        { ItemID.WorkBench, ItemID.WoodenDoor, ItemID.WoodenTable, ItemID.WoodenChair, ItemID.Bed,
          ItemID.WoodPlatform, ItemID.Chest, ItemID.Candle, -1, ItemID.GrandfatherClock },
        // Boreal Wood
        { ItemID.BorealWoodWorkBench, ItemID.BorealWoodDoor, ItemID.BorealWoodTable, ItemID.BorealWoodChair, ItemID.BorealWoodBed,
          ItemID.BorealWoodPlatform, ItemID.BorealWoodChest, ItemID.BorealWoodCandle, ItemID.BorealWoodChandelier, ItemID.BorealWoodClock },
        // Palm Wood
        { ItemID.PalmWoodWorkBench, ItemID.PalmWoodDoor, ItemID.PalmWoodTable, ItemID.PalmWoodChair, ItemID.PalmWoodBed,
          ItemID.PalmWoodPlatform, ItemID.PalmWoodChest, ItemID.PalmWoodCandle, ItemID.PalmWoodChandelier, ItemID.PalmWoodClock },
        // Rich Mahogany
        { ItemID.RichMahoganyWorkBench, ItemID.RichMahoganyDoor, ItemID.RichMahoganyTable, ItemID.RichMahoganyChair, ItemID.RichMahoganyBed,
          ItemID.RichMahoganyPlatform, ItemID.RichMahoganyChest, ItemID.RichMahoganyCandle, ItemID.RichMahoganyChandelier, ItemID.RichMahoganyClock },
        // Ebonwood
        { ItemID.EbonwoodWorkBench, ItemID.EbonwoodDoor, ItemID.EbonwoodTable, ItemID.EbonwoodChair, ItemID.EbonwoodBed,
          ItemID.EbonwoodPlatform, ItemID.EbonwoodChest, ItemID.EbonwoodCandle, ItemID.EbonwoodChandelier, ItemID.EbonwoodClock },
        // Shadewood
        { ItemID.ShadewoodWorkBench, ItemID.ShadewoodDoor, ItemID.ShadewoodTable, ItemID.ShadewoodChair, ItemID.ShadewoodBed,
          ItemID.ShadewoodPlatform, ItemID.ShadewoodChest, ItemID.ShadewoodCandle, ItemID.ShadewoodChandelier, ItemID.ShadewoodClock },
        // Pearlwood
        { ItemID.PearlwoodWorkBench, ItemID.PearlwoodDoor, ItemID.PearlwoodTable, ItemID.PearlwoodChair, ItemID.PearlwoodBed,
          ItemID.PearlwoodPlatform, ItemID.PearlwoodChest, ItemID.PearlwoodCandle, ItemID.PearlwoodChandelier, ItemID.PearlwoodClock },
        // Spooky Wood
        { ItemID.SpookyWorkBench, ItemID.SpookyDoor, ItemID.SpookyTable, ItemID.SpookyChair, ItemID.SpookyBed,
          ItemID.SpookyPlatform, ItemID.SpookyChest, ItemID.SpookyCandle, ItemID.SpookyChandelier, ItemID.SpookyClock }
    };

    #endregion

    #region Layout Builders

    /// <summary>
    /// Build the Armor tab layout with main armor grid and accessory grid.
    /// </summary>
    private void BuildArmorTabLayout()
    {
        armorTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int currentY = 0;

        // === Main Armor Grid (8 columns × 4 rows) ===

        // Header row (material bars)
        for (int col = 0; col < ArmorMaterialHeaderIds.Length; col++)
        {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(ArmorMaterialHeaderIds[col], isHeader: true));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 4; // Extra gap after header

        // Armor rows (3 rows)
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < ArmorMaterialHeaderIds.Length; col++)
            {
                int slotX = col * (SLOT_SIZE + SLOT_SPACING);
                int itemId = ArmorGridItemIds[row, col];
                if (itemId > 0)
                {
                    armorTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                        new CraftingSlotInfo(itemId, isHeader: false));
                }
            }
            currentY += SLOT_SIZE + SLOT_SPACING;
        }

        // === Accessory Grid (5 columns × 3 rows) - positioned to the right ===
        int accessoryStartX = (ArmorMaterialHeaderIds.Length * (SLOT_SIZE + SLOT_SPACING)) + 20; // Gap between grids
        int accessoryY = 0;

        // Header row
        for (int col = 0; col < AccessoryMaterialHeaderIds.Length; col++)
        {
            int slotX = accessoryStartX + col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, accessoryY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(AccessoryMaterialHeaderIds[col], isHeader: true));
        }
        accessoryY += SLOT_SIZE + SLOT_SPACING + 4;

        // Watches row
        for (int col = 0; col < WatchItemIds.Length; col++)
        {
            int slotX = accessoryStartX + col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, accessoryY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(WatchItemIds[col], isHeader: false));
        }
        accessoryY += SLOT_SIZE + SLOT_SPACING;

        // Chandeliers row
        for (int col = 0; col < ChandelierItemIds.Length; col++)
        {
            int slotX = accessoryStartX + col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, accessoryY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(ChandelierItemIds[col], isHeader: false));
        }
    }

    /// <summary>
    /// Build the Weapons tab layout (9 columns × 6 rows including header).
    /// </summary>
    private void BuildWeaponsTabLayout()
    {
        weaponsTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int currentY = 0;
        int columnCount = WeaponMaterialHeaderIds.Length;

        // Header row
        for (int col = 0; col < columnCount; col++)
        {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            weaponsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(WeaponMaterialHeaderIds[col], isHeader: true));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 4;

        // Weapon rows
        int[][] weaponRows = { SwordItemIds, BowItemIds, PickaxeItemIds, AxeItemIds, HammerItemIds };

        foreach (int[] rowItems in weaponRows)
        {
            for (int col = 0; col < rowItems.Length; col++)
            {
                int itemId = rowItems[col];
                if (itemId > 0)
                {
                    int slotX = col * (SLOT_SIZE + SLOT_SPACING);
                    weaponsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                        new CraftingSlotInfo(itemId, isHeader: false));
                }
                // Empty slots (itemId = -1) are simply not added
            }
            currentY += SLOT_SIZE + SLOT_SPACING;
        }
    }

    /// <summary>
    /// Build the Materials tab layout (Bars, Bricks, Misc, Crafting Stations).
    /// </summary>
    private void BuildMaterialsTabLayout()
    {
        materialsTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int currentY = 0;

        // Bars row (all craftable, not headers)
        for (int col = 0; col < BarItemIds.Length; col++)
        {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(BarItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING;

        // Bricks row
        for (int col = 0; col < BrickItemIds.Length; col++)
        {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(BrickItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 10; // Gap before misc

        // Misc row
        for (int col = 0; col < MiscMaterialItemIds.Length; col++)
        {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(MiscMaterialItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 10; // Gap before stations

        // Crafting stations row
        for (int col = 0; col < CraftingStationItemIds.Length; col++)
        {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(CraftingStationItemIds[col], isHeader: false));
        }
    }

    /// <summary>
    /// Build the Furniture tab layout (8 wood types × 10 furniture pieces).
    /// </summary>
    private void BuildFurnitureTabLayout()
    {
        furnitureTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int rowCount = FurnitureWoodTypes.Length;  // 8 wood types
        int colCount = 10;  // 10 furniture types per row

        for (int row = 0; row < rowCount; row++)
        {
            int rowY = row * (SLOT_SIZE + SLOT_SPACING);

            for (int col = 0; col < colCount; col++)
            {
                int slotX = col * (SLOT_SIZE + SLOT_SPACING);
                int itemId = FurnitureGridItemIds[row, col];

                if (itemId > 0)
                {
                    furnitureTabLayout.AddElement(slotX, rowY, SLOT_SIZE, SLOT_SIZE,
                        new CraftingSlotInfo(itemId, isHeader: false));
                }
            }
        }
    }

    #endregion
}