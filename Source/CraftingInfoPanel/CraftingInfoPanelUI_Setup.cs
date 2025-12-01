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

    // Armor material header bar IDs (column headers)
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

    #region Hardmode Armor Tab Data

    // Hardmode header materials (6 columns: Cobalt/Palladium/Mythril/Orichalcum/Adamantite/Titanium)
    private static readonly int[] HardmodeArmorMaterialHeaderIds = {
        ItemID.CobaltBar, ItemID.PalladiumBar, ItemID.MythrilBar, ItemID.OrichalcumBar,
        ItemID.AdamantiteBar, ItemID.TitaniumBar
    };

    // Hardmode helmets (multiple variants per material - showing melee helmet for simplicity)
    private static readonly int[] HardmodeHelmetItemIds = {
        ItemID.CobaltHelmet, ItemID.PalladiumHelmet, ItemID.MythrilHelmet, ItemID.OrichalcumHelmet,
        ItemID.AdamantiteHelmet, ItemID.TitaniumHelmet
    };

    // Hardmode chestplates
    private static readonly int[] HardmodeChestplateItemIds = {
        ItemID.CobaltBreastplate, ItemID.PalladiumBreastplate, ItemID.MythrilChainmail, ItemID.OrichalcumBreastplate,
        ItemID.AdamantiteBreastplate, ItemID.TitaniumBreastplate
    };

    // Hardmode leggings
    private static readonly int[] HardmodeLeggingsItemIds = {
        ItemID.CobaltLeggings, ItemID.PalladiumLeggings, ItemID.MythrilGreaves, ItemID.OrichalcumLeggings,
        ItemID.AdamantiteLeggings, ItemID.TitaniumLeggings
    };

    // Secondary hardmode materials (Hallowed bars)
    private static readonly int[] HardmodeSecondaryMaterialHeaderIds = {
        ItemID.HallowedBar
    };

    // Hallowed armor variants (headgear/helmet/hood/mask + breastplate + greaves)
    private static readonly int[] HallowedArmorItemIds = {
        ItemID.HallowedHelmet, ItemID.HallowedPlateMail, ItemID.HallowedGreaves
    };

    #endregion

    #region Weapons Tab Data

    // Pre-hardmode weapon data organized by material type (9 materials: Wood + 8 ores)
    // Each material has 6 items arranged in 2-col × 3-row format:
    //   Row 0: [Material (header), Pickaxe (header)] + gap
    //   Row 1: [Sword, Bow]
    //   Row 2: [Axe, Hammer]
    // Array order: Material, Pickaxe, Sword, Bow, Axe, Hammer
    private static readonly int[][] PreHardmodeWeaponsByMaterial = {
        // Wood (no pickaxe, no axe)
        new int[] { ItemID.Wood, -1, ItemID.WoodenSword, ItemID.WoodenBow, -1, ItemID.WoodenHammer },
        // Copper
        new int[] { ItemID.CopperBar, ItemID.CopperPickaxe, ItemID.CopperBroadsword, ItemID.CopperBow, ItemID.CopperAxe, ItemID.CopperHammer },
        // Tin
        new int[] { ItemID.TinBar, ItemID.TinPickaxe, ItemID.TinBroadsword, ItemID.TinBow, ItemID.TinAxe, ItemID.TinHammer },
        // Iron
        new int[] { ItemID.IronBar, ItemID.IronPickaxe, ItemID.IronBroadsword, ItemID.IronBow, ItemID.IronAxe, ItemID.IronHammer },
        // Lead
        new int[] { ItemID.LeadBar, ItemID.LeadPickaxe, ItemID.LeadBroadsword, ItemID.LeadBow, ItemID.LeadAxe, ItemID.LeadHammer },
        // Silver
        new int[] { ItemID.SilverBar, ItemID.SilverPickaxe, ItemID.SilverBroadsword, ItemID.SilverBow, ItemID.SilverAxe, ItemID.SilverHammer },
        // Tungsten
        new int[] { ItemID.TungstenBar, ItemID.TungstenPickaxe, ItemID.TungstenBroadsword, ItemID.TungstenBow, ItemID.TungstenAxe, ItemID.TungstenHammer },
        // Gold
        new int[] { ItemID.GoldBar, ItemID.GoldPickaxe, ItemID.GoldBroadsword, ItemID.GoldBow, ItemID.GoldAxe, ItemID.GoldHammer },
        // Platinum
        new int[] { ItemID.PlatinumBar, ItemID.PlatinumPickaxe, ItemID.PlatinumBroadsword, ItemID.PlatinumBow, ItemID.PlatinumAxe, ItemID.PlatinumHammer }
    };

    #endregion

    #region Hardmode Weapons Tab Data

    // Hardmode weapon data organized by ore type (7 ores including Hallowed)
    // Each ore has 7 items arranged in 2-col × 4-row format:
    //   Row 0: [Bar, Pickaxe]
    //   Row 1: [Sword, Chainsaw]
    //   Row 2: [Waraxe, Repeater]
    //   Row 3: [Drill, (empty)]
    // Array order: Bar, Pickaxe, Sword, Chainsaw, Waraxe, Repeater, Drill
    private static readonly int[][] HardmodeWeaponsByOre = {
        // Cobalt
        new int[] { ItemID.CobaltBar, ItemID.CobaltPickaxe, ItemID.CobaltSword, ItemID.CobaltChainsaw,
                    ItemID.CobaltWaraxe, ItemID.CobaltRepeater, ItemID.CobaltDrill },
        // Palladium
        new int[] { ItemID.PalladiumBar, ItemID.PalladiumPickaxe, ItemID.PalladiumSword, ItemID.PalladiumChainsaw,
                    ItemID.PalladiumWaraxe, ItemID.PalladiumRepeater, ItemID.PalladiumDrill },
        // Mythril
        new int[] { ItemID.MythrilBar, ItemID.MythrilPickaxe, ItemID.MythrilSword, ItemID.MythrilChainsaw,
                    ItemID.MythrilWaraxe, ItemID.MythrilRepeater, ItemID.MythrilDrill },
        // Orichalcum
        new int[] { ItemID.OrichalcumBar, ItemID.OrichalcumPickaxe, ItemID.OrichalcumSword, ItemID.OrichalcumChainsaw,
                    ItemID.OrichalcumWaraxe, ItemID.OrichalcumRepeater, ItemID.OrichalcumDrill },
        // Adamantite
        new int[] { ItemID.AdamantiteBar, ItemID.AdamantitePickaxe, ItemID.AdamantiteSword, ItemID.AdamantiteChainsaw,
                    ItemID.AdamantiteWaraxe, ItemID.AdamantiteRepeater, ItemID.AdamantiteDrill },
        // Titanium
        new int[] { ItemID.TitaniumBar, ItemID.TitaniumPickaxe, ItemID.TitaniumSword, ItemID.TitaniumChainsaw,
                    ItemID.TitaniumWaraxe, ItemID.TitaniumRepeater, ItemID.TitaniumDrill },
        // Hallowed (hybrid items: PickaxeAxe = pickaxe+waraxe, Drax = drill+chainsaw)
        // Layout: Bar, PickaxeAxe, Excalibur, Drax, (empty), HallowedRepeater, (empty)
        new int[] { ItemID.HallowedBar, ItemID.PickaxeAxe, ItemID.Excalibur, ItemID.Drax,
                    -1, ItemID.HallowedRepeater, -1 }
    };

    #endregion

    #region Materials Tab Data

    // Metal bars row (pre-hardmode basic ores)
    private static readonly int[] BarItemIds = {
        ItemID.CopperBar, ItemID.TinBar, ItemID.IronBar, ItemID.LeadBar,
        ItemID.SilverBar, ItemID.TungstenBar, ItemID.GoldBar, ItemID.PlatinumBar
    };

    // Pre-hardmode special bars (evil biome + hell)
    private static readonly int[] SpecialBarItemIds = {
        ItemID.DemoniteBar, ItemID.CrimtaneBar, ItemID.MeteoriteBar, ItemID.HellstoneBar
    };

    // Hardmode bars row
    private static readonly int[] HardmodeBarItemIds = {
        ItemID.CobaltBar, ItemID.PalladiumBar, ItemID.MythrilBar, ItemID.OrichalcumBar,
        ItemID.AdamantiteBar, ItemID.TitaniumBar, ItemID.HallowedBar
    };

    // Bricks row (pre-hardmode basic ores)
    private static readonly int[] BrickItemIds = {
        ItemID.CopperBrick, ItemID.TinBrick, ItemID.IronBrick, ItemID.LeadBrick,
        ItemID.SilverBrick, ItemID.TungstenBrick, ItemID.GoldBrick, ItemID.PlatinumBrick
    };

    // Pre-hardmode special bricks (evil biome + hell)
    private static readonly int[] SpecialBrickItemIds = {
        ItemID.DemoniteBrick, ItemID.CrimtaneBrick, ItemID.MeteoriteBrick, ItemID.HellstoneBrick
    };

    // Hardmode bricks row (Cobalt, Mythril, Pearlstone for Hallowed)
    private static readonly int[] HardmodeBrickItemIds = {
        ItemID.CobaltBrick, ItemID.MythrilBrick, ItemID.PearlstoneBrick
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
        for (int col = 0; col < ArmorMaterialHeaderIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(ArmorMaterialHeaderIds[col], isHeader: true));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 4; // Extra gap after header

        // Armor rows (3 rows)
        for (int row = 0; row < 3; row++) {
            for (int col = 0; col < ArmorMaterialHeaderIds.Length; col++) {
                int slotX = col * (SLOT_SIZE + SLOT_SPACING);
                int itemId = ArmorGridItemIds[row, col];
                if (itemId > 0) {
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
        for (int col = 0; col < AccessoryMaterialHeaderIds.Length; col++) {
            int slotX = accessoryStartX + col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, accessoryY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(AccessoryMaterialHeaderIds[col], isHeader: true));
        }
        accessoryY += SLOT_SIZE + SLOT_SPACING + 4;

        // Watches row
        for (int col = 0; col < WatchItemIds.Length; col++) {
            int slotX = accessoryStartX + col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, accessoryY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(WatchItemIds[col], isHeader: false));
        }
        accessoryY += SLOT_SIZE + SLOT_SPACING;

        // Chandeliers row
        for (int col = 0; col < ChandelierItemIds.Length; col++) {
            int slotX = accessoryStartX + col * (SLOT_SIZE + SLOT_SPACING);
            armorTabLayout.AddElement(slotX, accessoryY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(ChandelierItemIds[col], isHeader: false));
        }
    }

    /// <summary>
    /// Build the Weapons tab layout using 2-column blocks per material type.
    /// Layout per material block (2 cols × 3 rows with header gap):
    ///   Row 0: [Material (header), Pickaxe (header)] + gap
    ///   Row 1: [Sword, Bow]
    ///   Row 2: [Axe, Hammer]
    /// 9 material blocks total: Wood, Copper, Tin, Iron, Lead, Silver, Tungsten, Gold, Platinum
    /// </summary>
    private void BuildWeaponsTabLayout()
    {
        weaponsTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int materialBlockWidth = 2 * (SLOT_SIZE + SLOT_SPACING);  // 2 columns per material block
        int gapBetweenBlocks = 8;  // Visual separation between material groups
        int headerGap = 4;  // Extra gap after header row

        // For each material type, place items in 2-col × 3-row format
        for (int materialIndex = 0; materialIndex < PreHardmodeWeaponsByMaterial.Length; materialIndex++) {
            int[] materialItems = PreHardmodeWeaponsByMaterial[materialIndex];
            int blockStartX = materialIndex * (materialBlockWidth + gapBetweenBlocks);

            // Item indices in the materialItems array:
            // 0=Material, 1=Pickaxe, 2=Sword, 3=Bow, 4=Axe, 5=Hammer
            // Layout positions (col, row):
            // (0,0)=Material, (1,0)=Pickaxe, (0,1)=Sword, (1,1)=Bow, (0,2)=Axe, (1,2)=Hammer

            for (int itemIndex = 0; itemIndex < materialItems.Length; itemIndex++) {
                int itemId = materialItems[itemIndex];
                if (itemId <= 0) {
                    continue;  // Skip empty slots (-1)
                }

                // Determine position based on item index
                int col, row;
                bool isHeader;
                switch (itemIndex) {
                    case 0: col = 0; row = 0; isHeader = true; break;   // Material at col 0, row 0 (header)
                    case 1: col = 1; row = 0; isHeader = true; break;   // Pickaxe at col 1, row 0 (header)
                    case 2: col = 0; row = 1; isHeader = false; break;  // Sword at col 0, row 1
                    case 3: col = 1; row = 2; isHeader = false; break;  // Bow at col 1, row 2 (swapped with Hammer)
                    case 4: col = 0; row = 2; isHeader = false; break;  // Axe at col 0, row 2
                    case 5: col = 1; row = 1; isHeader = false; break;  // Hammer at col 1, row 1 (swapped with Bow)
                    default: continue;
                }

                int slotX = blockStartX + col * (SLOT_SIZE + SLOT_SPACING);
                // Add header gap for rows after the header (row > 0)
                int slotY = row * (SLOT_SIZE + SLOT_SPACING) + (row > 0 ? headerGap : 0);

                weaponsTabLayout.AddElement(slotX, slotY, SLOT_SIZE, SLOT_SIZE,
                    new CraftingSlotInfo(itemId, isHeader));
            }
        }
    }

    /// <summary>
    /// Build the Materials tab layout (Bars, Bricks, Misc, Crafting Stations).
    /// Pre-hardmode bars/bricks on left, hardmode bars/bricks on right with horizontal gap.
    /// </summary>
    private void BuildMaterialsTabLayout()
    {
        materialsTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int currentY = 0;
        int hardmodeGap = 16;  // Horizontal gap between pre-hardmode and hardmode sections

        // === Row 1: Bars (pre-hardmode basic + special + gap + hardmode) ===
        // Pre-hardmode basic bars
        for (int col = 0; col < BarItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(BarItemIds[col], isHeader: false));
        }
        // Pre-hardmode special bars (Demonite, Crimtane, Meteorite, Hellstone)
        int specialBarsStartX = BarItemIds.Length * (SLOT_SIZE + SLOT_SPACING);
        for (int col = 0; col < SpecialBarItemIds.Length; col++) {
            int slotX = specialBarsStartX + col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(SpecialBarItemIds[col], isHeader: false));
        }
        // Hardmode bars (after gap)
        int hardmodeStartX = (BarItemIds.Length + SpecialBarItemIds.Length) * (SLOT_SIZE + SLOT_SPACING) + hardmodeGap;
        for (int col = 0; col < HardmodeBarItemIds.Length; col++) {
            int slotX = hardmodeStartX + col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(HardmodeBarItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING;

        // === Row 2: Bricks (pre-hardmode basic + special + gap + hardmode) ===
        // Pre-hardmode basic bricks
        for (int col = 0; col < BrickItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(BrickItemIds[col], isHeader: false));
        }
        // Pre-hardmode special bricks (Demonite, Crimtane, Meteorite, Hellstone)
        for (int col = 0; col < SpecialBrickItemIds.Length; col++) {
            int slotX = specialBarsStartX + col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(SpecialBrickItemIds[col], isHeader: false));
        }
        // Hardmode bricks (after gap, aligned with hardmode bars)
        for (int col = 0; col < HardmodeBrickItemIds.Length; col++) {
            int slotX = hardmodeStartX + col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(HardmodeBrickItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 10; // Gap before misc

        // === Row 3: Misc materials ===
        for (int col = 0; col < MiscMaterialItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(MiscMaterialItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 10; // Gap before stations

        // === Row 4: Crafting stations ===
        for (int col = 0; col < CraftingStationItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            materialsTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(CraftingStationItemIds[col], isHeader: false));
        }
    }

    /// <summary>
    /// Build the Furniture 1 tab layout (first 4 wood types: Wood, Boreal, Palm, Rich Mahogany).
    /// </summary>
    private void BuildFurniture1TabLayout()
    {
        furniture1TabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int colCount = 10;  // 10 furniture types per row

        // First 4 wood types (rows 0-3)
        for (int row = 0; row < 4; row++) {
            int rowY = row * (SLOT_SIZE + SLOT_SPACING);

            for (int col = 0; col < colCount; col++) {
                int slotX = col * (SLOT_SIZE + SLOT_SPACING);
                int itemId = FurnitureGridItemIds[row, col];

                if (itemId > 0) {
                    furniture1TabLayout.AddElement(slotX, rowY, SLOT_SIZE, SLOT_SIZE,
                        new CraftingSlotInfo(itemId, isHeader: false));
                }
            }
        }
    }

    /// <summary>
    /// Build the Furniture 2 tab layout (last 4 wood types: Ebonwood, Shadewood, Pearlwood, Spooky).
    /// </summary>
    private void BuildFurniture2TabLayout()
    {
        furniture2TabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int colCount = 10;  // 10 furniture types per row

        // Last 4 wood types (rows 4-7 of data, but positioned at 0-3 visually)
        for (int dataRow = 4; dataRow < 8; dataRow++) {
            int visualRow = dataRow - 4;
            int rowY = visualRow * (SLOT_SIZE + SLOT_SPACING);

            for (int col = 0; col < colCount; col++) {
                int slotX = col * (SLOT_SIZE + SLOT_SPACING);
                int itemId = FurnitureGridItemIds[dataRow, col];

                if (itemId > 0) {
                    furniture2TabLayout.AddElement(slotX, rowY, SLOT_SIZE, SLOT_SIZE,
                        new CraftingSlotInfo(itemId, isHeader: false));
                }
            }
        }
    }

    /// <summary>
    /// Build the Hardmode Armor tab layout (Cobalt through Titanium + Hallowed).
    /// </summary>
    private void BuildHardmodeArmorTabLayout()
    {
        hardmodeArmorTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int currentY = 0;
        int columnCount = HardmodeArmorMaterialHeaderIds.Length;

        // Header row (material bars)
        for (int col = 0; col < columnCount; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            hardmodeArmorTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(HardmodeArmorMaterialHeaderIds[col], isHeader: true));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 4;

        // Helmets row
        for (int col = 0; col < HardmodeHelmetItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            hardmodeArmorTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(HardmodeHelmetItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING;

        // Chestplates row
        for (int col = 0; col < HardmodeChestplateItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            hardmodeArmorTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(HardmodeChestplateItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING;

        // Leggings row
        for (int col = 0; col < HardmodeLeggingsItemIds.Length; col++) {
            int slotX = col * (SLOT_SIZE + SLOT_SPACING);
            hardmodeArmorTabLayout.AddElement(slotX, currentY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(HardmodeLeggingsItemIds[col], isHeader: false));
        }
        currentY += SLOT_SIZE + SLOT_SPACING + 10;

        // === Hallowed section ===
        int hallowedStartX = columnCount * (SLOT_SIZE + SLOT_SPACING) + 20;

        // Hallowed header
        hardmodeArmorTabLayout.AddElement(hallowedStartX, 0, SLOT_SIZE, SLOT_SIZE,
            new CraftingSlotInfo(HardmodeSecondaryMaterialHeaderIds[0], isHeader: true));

        // Hallowed armor (vertical: helmet, chestplate, greaves)
        int hallowedY = SLOT_SIZE + SLOT_SPACING + 4;
        for (int row = 0; row < HallowedArmorItemIds.Length; row++) {
            hardmodeArmorTabLayout.AddElement(hallowedStartX, hallowedY, SLOT_SIZE, SLOT_SIZE,
                new CraftingSlotInfo(HallowedArmorItemIds[row], isHeader: false));
            hallowedY += SLOT_SIZE + SLOT_SPACING;
        }
    }

    /// <summary>
    /// Build the Hardmode Weapons tab layout using 2-column blocks per ore type.
    /// Layout per ore block (2 cols × 4 rows with header gap):
    ///   Row 0: [Bar (header), Pickaxe (header)] + gap
    ///   Row 1: [Sword, Chainsaw]
    ///   Row 2: [Waraxe, Repeater]
    ///   Row 3: [Drill, (empty)]
    /// 7 ore blocks total: Cobalt, Palladium, Mythril, Orichalcum, Adamantite, Titanium, Hallowed
    /// </summary>
    private void BuildHardmodeWeaponsTabLayout()
    {
        hardmodeWeaponsTabLayout = new PanelPositionCalculator<CraftingSlotInfo>(padding: 8);

        int oreBlockWidth = 2 * (SLOT_SIZE + SLOT_SPACING);  // 2 columns per ore block
        int gapBetweenBlocks = 8;  // Visual separation between ore groups
        int headerGap = 4;  // Extra gap after header row (like armor tab)

        // For each ore type, place items in 2-col × 4-row format
        for (int oreIndex = 0; oreIndex < HardmodeWeaponsByOre.Length; oreIndex++) {
            int[] oreItems = HardmodeWeaponsByOre[oreIndex];
            int blockStartX = oreIndex * (oreBlockWidth + gapBetweenBlocks);

            // Item indices in the oreItems array:
            // 0=Bar, 1=Pickaxe, 2=Sword, 3=Chainsaw, 4=Waraxe, 5=Repeater, 6=Drill
            // Layout positions (col, row) - row values will be adjusted for header gap
            // Row 0 = header row, rows 1-3 = content rows (offset by headerGap)

            for (int itemIndex = 0; itemIndex < oreItems.Length; itemIndex++) {
                int itemId = oreItems[itemIndex];
                if (itemId <= 0) {
                    continue;  // Skip empty slots (-1)
                }

                // Determine position based on item index
                int col, row;
                bool isHeader;
                switch (itemIndex) {
                    case 0: col = 0; row = 0; isHeader = true; break;   // Bar at col 0, row 0 (header)
                    case 1: col = 1; row = 0; isHeader = true; break;   // Pickaxe at col 1, row 0 (header)
                    case 2: col = 0; row = 1; isHeader = false; break;  // Sword at col 0, row 1
                    case 3: col = 1; row = 1; isHeader = false; break;  // Chainsaw at col 1, row 1
                    case 4: col = 0; row = 2; isHeader = false; break;  // Waraxe at col 0, row 2
                    case 5: col = 1; row = 2; isHeader = false; break;  // Repeater at col 1, row 2
                    case 6: col = 0; row = 3; isHeader = false; break;  // Drill at col 0, row 3
                    default: continue;
                }

                int slotX = blockStartX + col * (SLOT_SIZE + SLOT_SPACING);
                // Add header gap for rows after the header (row > 0)
                int slotY = row * (SLOT_SIZE + SLOT_SPACING) + (row > 0 ? headerGap : 0);

                hardmodeWeaponsTabLayout.AddElement(slotX, slotY, SLOT_SIZE, SLOT_SIZE,
                    new CraftingSlotInfo(itemId, isHeader));
            }
        }
    }

    #endregion
}