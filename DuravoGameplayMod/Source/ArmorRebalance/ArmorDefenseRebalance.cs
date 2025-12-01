// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DuravoQOLMod.ArmorRebalance
{
    /// <summary>
    /// Rebalances vanilla armor defense values.
    /// Issue in Vanilla: Upgrading individual pieces does not feel meaningful because
    ///           of breaking set bonuses, and by the time you have enough for the full
    ///           next tier, you may have enough to skip 1-2 tiers of armor entirely.
    /// Goal: Make all upgrades feel meaningful. Move all defense to the pieces and buff
    ///       defense so there is more reason for piecemeal upgrades.
    ///       Multi-piece buffs replace set bonuses (handled in ArmorSetBonusPlayer),
    ///       giving the player more of a reason to make and maintain the kits they like.
    /// </summary>
    public class ArmorDefenseRebalance : GlobalItem
    {
        // FEATURE FLAG: Heavy armor knockback bonus (resistance + reflection)
        private const bool EnableHeavyKnockbackBonus = true;

        // Tooltip color hex codes (pre-computed to avoid allocations)
        private const string HexColorActiveBuffName = "00FF00";   // Green
        private const string HexColorInactiveText = "666666";     // Dark grey
        private const string HexColorInactiveCount = "FF0000";    // Red
        private const string HexColorChestBonus = "00BFFF";       // Cyan

        // Pre-computed localization keys (avoid runtime string interpolation)
        private const string LocKeyChestShield30HP = "Mods.DuravoQOLMod.ArmorRebalance.ChestBonuses.Shield30HP";
        private const string LocKeyChestShield15Pct = "Mods.DuravoQOLMod.ArmorRebalance.ChestBonuses.Shield15Pct";
        private const string LocKeyChestSpeed15Pct = "Mods.DuravoQOLMod.ArmorRebalance.ChestBonuses.Speed15Pct";
        private const string LocKeyBuffNameShiny = "Mods.DuravoQOLMod.ArmorRebalance.BuffNames.Shiny";
        private const string LocKeyBuffNameSuperShiny = "Mods.DuravoQOLMod.ArmorRebalance.BuffNames.SuperShiny";
        private const string LocKeyBuffNameHeavy = "Mods.DuravoQOLMod.ArmorRebalance.BuffNames.Heavy";
        private const string LocKeyBuffDescShiny = "Mods.DuravoQOLMod.ArmorRebalance.BuffDescriptions.Shiny";
        private const string LocKeyBuffDescSuperShiny = "Mods.DuravoQOLMod.ArmorRebalance.BuffDescriptions.SuperShiny";
        private const string LocKeyBuffDescHeavy = "Mods.DuravoQOLMod.ArmorRebalance.BuffDescriptions.Heavy";

        public override void SetDefaults(Item item)
        {
            // Check if feature is enabled (static accessor)
            if (!DuravoQOLModConfig.EnableArmorRebalance)
                return;

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
        /// Add custom tooltip lines describing chestplate bonuses and multi-piece buffs.
        /// Also removes vanilla set bonus tooltip since we replace it with our own system.
        /// Tooltips show static format for inventory, dynamic format for equipped items.
        /// </summary>
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            // Check if feature is enabled (static accessor)
            if (!DuravoQOLModConfig.EnableArmorRebalance)
                return;

            // Remove vanilla "Set bonus: X defense" tooltip line
            tooltips.RemoveAll(line => line.Name == "SetBonus");

            var armorTag = ArmorSetBonusPlayer.GetArmorTagForItem(item.type);
            bool isEquipped = IsThisItemEquipped(item);

            // Add chestplate-specific bonus (if applicable)
            AddChestplateBonusTooltip(item.type, tooltips);

            // Add multi-piece buff tooltip (Shiny/Super Shiny system)
            if (armorTag != ArmorSetBonusPlayer.ArmorTag.None) {
                AddMultiPieceBuffTooltip(armorTag, isEquipped, tooltips);
            }

            // Add Heavy chestplate tooltip (chestplate-only effect)
            AddHeavyChestplateTooltip(item.type, isEquipped, tooltips);
        }

        /// <summary>
        /// Check if THIS specific item instance is currently equipped by the local player.
        /// Uses GUID-based instance tracking since Terraria clones items for tooltip display.
        /// The clone preserves the same GUID, allowing us to match it to the equipped original.
        /// </summary>
        private static bool IsThisItemEquipped(Item item)
        {
            Player player = Main.LocalPlayer;
            for (int armorSlot = 0; armorSlot < 3; armorSlot++) {
                // Use GUID comparison - works even when Terraria clones items for tooltips
                if (ItemInstanceTracker.AreSameInstance(player.armor[armorSlot], item)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Add tooltip for chestplate-specific bonuses (shields, speed).
        /// </summary>
        private void AddChestplateBonusTooltip(int itemType, List<TooltipLine> tooltips)
        {
            string locKey = itemType switch {
                // Shield (30HP) - Copper/Silver chestplates
                ItemID.CopperChainmail => LocKeyChestShield30HP,
                ItemID.SilverChainmail => LocKeyChestShield30HP,

                // Speed - Tin chestplate only
                ItemID.TinChainmail => LocKeyChestSpeed15Pct,

                // Shield (15% HP) - Gold/Platinum chestplates
                ItemID.GoldChainmail => LocKeyChestShield15Pct,
                ItemID.PlatinumChainmail => LocKeyChestShield15Pct,

                _ => null
            };

            if (locKey != null) {
                string bonusText = Language.GetTextValue(locKey);
                var tooltipLine = new TooltipLine(Mod, "ChestplateBonus", $"[c/{HexColorChestBonus}:{bonusText}]");
                tooltips.Add(tooltipLine);
            }
        }

        /// <summary>
        /// Add tooltip for multi-piece buff (Shiny/Super Shiny).
        /// Shows static format for inventory, dynamic format with colors for equipped.
        /// </summary>
        private void AddMultiPieceBuffTooltip(ArmorSetBonusPlayer.ArmorTag armorTag, bool isEquipped, List<TooltipLine> tooltips)
        {
            bool isSuperShiny = armorTag == ArmorSetBonusPlayer.ArmorTag.SuperShiny;
            string buffName = Language.GetTextValue(isSuperShiny ? LocKeyBuffNameSuperShiny : LocKeyBuffNameShiny);
            string buffDescription = Language.GetTextValue(isSuperShiny ? LocKeyBuffDescSuperShiny : LocKeyBuffDescShiny);

            string tooltipText;
            if (!isEquipped) {
                // Inventory: static format without current count
                tooltipText = $"{buffName} (2pc) {buffDescription}";
            }
            else {
                // Equipped: show current count with colors
                var player = Main.LocalPlayer.GetModPlayer<ArmorSetBonusPlayer>();
                var (shinyCount, superShinyCount, _) = player.GetCurrentBuffStatus();

                int currentCount;
                bool isActive;

                if (armorTag == ArmorSetBonusPlayer.ArmorTag.SuperShiny) {
                    currentCount = superShinyCount;
                    isActive = superShinyCount >= 2;
                }
                else {
                    // For Shiny, count includes SuperShiny pieces
                    currentCount = shinyCount + superShinyCount;
                    isActive = currentCount >= 2;
                }

                if (isActive) {
                    // Active: teal/cyan buff name
                    tooltipText = $"[c/{HexColorChestBonus}:{buffName}] ({currentCount}/2pc) {buffDescription}";
                }
                else {
                    // Inactive: dimmed buff name, red current count
                    tooltipText = $"[c/{HexColorInactiveText}:{buffName}] ([c/{HexColorInactiveCount}:{currentCount}]/2pc) {buffDescription}";
                }
            }

            var tooltipLine = new TooltipLine(Mod, "MultiPieceBuff", tooltipText);
            tooltips.Add(tooltipLine);
        }

        /// <summary>
        /// Add tooltip for Heavy chestplate effect (Iron/Lead/Tungsten only).
        /// Only shown when EnableHeavyKnockbackBonus is true.
        /// </summary>
        private void AddHeavyChestplateTooltip(int itemType, bool isEquipped, List<TooltipLine> tooltips)
        {
            // Don't show Heavy tooltip until knockback feature is implemented
            if (!EnableHeavyKnockbackBonus) return;

            bool isHeavyChestplate = itemType switch {
                ItemID.IronChainmail => true,
                ItemID.LeadChainmail => true,
                ItemID.TungstenChainmail => true,
                _ => false
            };

            if (!isHeavyChestplate) return;

            string buffName = Language.GetTextValue(LocKeyBuffNameHeavy);
            string buffDescription = Language.GetTextValue(LocKeyBuffDescHeavy);

            // Heavy is always cyan (like chestplate bonuses), regardless of equipped state
            string tooltipText = $"[c/{HexColorChestBonus}:{buffName} {buffDescription}]";

            var tooltipLine = new TooltipLine(Mod, "HeavyEffect", tooltipText);
            tooltips.Add(tooltipLine);
        }


    }
}