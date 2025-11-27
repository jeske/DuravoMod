using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TerrariaSurvivalMod.ArmorRebalance
{
    /// <summary>
    /// Rebalances vanilla armor defense values.
    /// Issue in Vanilla: Upgrading indivudal pieces does not feel meaningful because
    ///           of breaking set bonuses, and by the time you have enough for the full
    ///           next tier, you may have enough to skip 1-2 tiers of armor entirely.
    /// Goal: Make all upgrades feel meaningful. Move all defense to the pieces and buff
    ///       defense so there is more reason for piecemeal upgrades. 
    ///       Set bonuses become utility (handled in ArmorSetBonusPlayer), giving the
    ///       player more of a reason to make and maintain the kits they like.
    /// </summary>
    public class ArmorDefenseRebalance : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            // Goal: Redistribute set bonus defense into pieces, keeping same total
            switch (item.type) {
                // === COPPER ARMOR === (vanilla 1+2+1+2set = 6, proposed 1+3+2 = 6)
                case ItemID.CopperHelmet:
                    item.defense = 1; // unchanged
                    break;
                case ItemID.CopperChainmail:
                    item.defense = 3; // was 2, +1 from set bonus
                    break;
                case ItemID.CopperGreaves:
                    item.defense = 2; // was 1, +1 from set bonus
                    break;

                // === TIN ARMOR === (vanilla 2+2+2+1set = 7, proposed 2+3+2 = 7)
                case ItemID.TinHelmet:
                    item.defense = 2; // unchanged
                    break;
                case ItemID.TinChainmail:
                    item.defense = 3; // was 2, +1 from set bonus
                    break;
                case ItemID.TinGreaves:
                    item.defense = 2; // unchanged
                    break;

                // === IRON ARMOR === (vanilla 2+3+2+2set = 9, proposed 2+4+3 = 9)
                case ItemID.IronHelmet:
                    item.defense = 2; // unchanged
                    break;
                case ItemID.IronChainmail:
                    item.defense = 4; // was 3, +1 from set bonus
                    break;
                case ItemID.IronGreaves:
                    item.defense = 3; // was 2, +1 from set bonus
                    break;

                // === LEAD ARMOR === (vanilla 3+3+3+1set = 10, proposed 3+4+3 = 10)
                case ItemID.LeadHelmet:
                    item.defense = 3; // unchanged
                    break;
                case ItemID.LeadChainmail:
                    item.defense = 4; // was 3, +1 from set bonus
                    break;
                case ItemID.LeadGreaves:
                    item.defense = 3; // unchanged
                    break;

                // === SILVER ARMOR === (vanilla 3+4+3+2set = 12, proposed 3+5+4 = 12)
                case ItemID.SilverHelmet:
                    item.defense = 3; // unchanged
                    break;
                case ItemID.SilverChainmail:
                    item.defense = 5; // was 4, +1 from set bonus
                    break;
                case ItemID.SilverGreaves:
                    item.defense = 4; // was 3, +1 from set bonus
                    break;

                // === TUNGSTEN ARMOR === (vanilla 4+4+3+2set = 13, proposed 4+5+4 = 13)
                case ItemID.TungstenHelmet:
                    item.defense = 4; // unchanged
                    break;
                case ItemID.TungstenChainmail:
                    item.defense = 5; // was 4, +1 from set bonus
                    break;
                case ItemID.TungstenGreaves:
                    item.defense = 4; // was 3, +1 from set bonus
                    break;

                // === GOLD ARMOR === (vanilla 4+5+4+3set = 16, proposed 4+6+6 = 16)
                case ItemID.GoldHelmet:
                    item.defense = 4; // unchanged
                    break;
                case ItemID.GoldChainmail:
                    item.defense = 6; // was 5, +1 from set bonus
                    break;
                case ItemID.GoldGreaves:
                    item.defense = 6; // was 4, +2 from set bonus
                    break;

                // === PLATINUM ARMOR === (vanilla 5+5+4+4set = 18, proposed 5+7+6 = 18)
                case ItemID.PlatinumHelmet:
                    item.defense = 5; // unchanged
                    break;
                case ItemID.PlatinumChainmail:
                    item.defense = 7; // was 5, +2 from set bonus
                    break;
                case ItemID.PlatinumGreaves:
                    item.defense = 6; // was 4, +2 from set bonus
                    break;
            }
        }

        /// <summary>
        /// Add custom tooltip lines describing chestplate and set bonuses.
        /// Also removes vanilla set bonus tooltip since we replace it with our own system.
        /// </summary>
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            // Remove vanilla "Set bonus: X defense" tooltip line
            tooltips.RemoveAll(line => line.Name == "SetBonus");

            string chestplateBonusKey = GetChestplateBonusKey(item.type);
            bool isOreArmorPiece = IsOreArmorPiece(item.type);

            // Add chestplate-specific bonus first (if applicable)
            if (chestplateBonusKey != null) {
                string chestplateText = Language.GetTextValue($"Mods.TerrariaSurvivalMod.ArmorRebalance.Tooltips.{chestplateBonusKey}");
                tooltips.Add(new TooltipLine(Mod, "ChestplateBonus", chestplateText));
            }

            // Add set bonus for any armor piece
            if (isOreArmorPiece) {
                string setTooltip = Language.GetTextValue("Mods.TerrariaSurvivalMod.ArmorRebalance.Tooltips.SetBonusShiny");
                tooltips.Add(new TooltipLine(Mod, "FullSetBonus", setTooltip));
            }
        }

        /// <summary>
        /// Get the localization key for chestplate-specific bonus (null if not a chestplate).
        /// </summary>
        private static string GetChestplateBonusKey(int itemType)
        {
            return itemType switch {
                // Tin/Copper tier - Emergency Shield
                ItemID.TinChainmail or ItemID.CopperChainmail => "ShieldTinCopper",

                // Iron/Lead tier - Crit bonus
                ItemID.IronChainmail or ItemID.LeadChainmail => "CritIronLead",

                // Silver/Tungsten tier - Speed bonus
                ItemID.SilverChainmail or ItemID.TungstenChainmail => "SpeedSilverTungsten",

                // Gold/Platinum tier - Enhanced Emergency Shield
                ItemID.GoldChainmail or ItemID.PlatinumChainmail => "ShieldGoldPlatinum",

                _ => null
            };
        }

        /// <summary>
        /// Check if this item is part of an ore armor set.
        /// </summary>
        private static bool IsOreArmorPiece(int itemType)
        {
            return itemType switch {
                // Tin set
                ItemID.TinHelmet or ItemID.TinChainmail or ItemID.TinGreaves => true,

                // Copper set
                ItemID.CopperHelmet or ItemID.CopperChainmail or ItemID.CopperGreaves => true,

                // Iron set
                ItemID.IronHelmet or ItemID.IronChainmail or ItemID.IronGreaves => true,

                // Lead set
                ItemID.LeadHelmet or ItemID.LeadChainmail or ItemID.LeadGreaves => true,

                // Silver set
                ItemID.SilverHelmet or ItemID.SilverChainmail or ItemID.SilverGreaves => true,

                // Tungsten set
                ItemID.TungstenHelmet or ItemID.TungstenChainmail or ItemID.TungstenGreaves => true,

                // Gold set
                ItemID.GoldHelmet or ItemID.GoldChainmail or ItemID.GoldGreaves => true,

                // Platinum set
                ItemID.PlatinumHelmet or ItemID.PlatinumChainmail or ItemID.PlatinumGreaves => true,

                _ => false
            };
        }
    }
}