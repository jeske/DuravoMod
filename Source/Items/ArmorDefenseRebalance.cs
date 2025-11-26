using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaSurvivalMod.Items
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
            // Tin Armor: 6 vanilla -> 10 proposed
            switch (item.type)
            {
                // === TIN ARMOR ===
                case ItemID.TinHelmet:
                    item.defense = 3; // was 2
                    break;
                case ItemID.TinChainmail:
                    item.defense = 4; // was 2
                    break;
                case ItemID.TinGreaves:
                    item.defense = 3; // was 2
                    break;

                // === COPPER ARMOR ===
                case ItemID.CopperHelmet:
                    item.defense = 3; // was 1
                    break;
                case ItemID.CopperChainmail:
                    item.defense = 4; // was 2
                    break;
                case ItemID.CopperGreaves:
                    item.defense = 3; // was 1
                    break;

                // === IRON ARMOR ===
                case ItemID.IronHelmet:
                    item.defense = 4; // was 3
                    break;
                case ItemID.IronChainmail:
                    item.defense = 6; // was 4
                    break;
                case ItemID.IronGreaves:
                    item.defense = 4; // was 2
                    break;

                // === LEAD ARMOR ===
                case ItemID.LeadHelmet:
                    item.defense = 4; // was 3
                    break;
                case ItemID.LeadChainmail:
                    item.defense = 6; // was 4
                    break;
                case ItemID.LeadGreaves:
                    item.defense = 4; // was 2
                    break;

                // === SILVER ARMOR ===
                case ItemID.SilverHelmet:
                    item.defense = 5; // was 4
                    break;
                case ItemID.SilverChainmail:
                    item.defense = 7; // was 5
                    break;
                case ItemID.SilverGreaves:
                    item.defense = 5; // was 3
                    break;

                // === TUNGSTEN ARMOR ===
                case ItemID.TungstenHelmet:
                    item.defense = 5; // was 4
                    break;
                case ItemID.TungstenChainmail:
                    item.defense = 7; // was 5
                    break;
                case ItemID.TungstenGreaves:
                    item.defense = 5; // was 3
                    break;

                // === GOLD ARMOR ===
                case ItemID.GoldHelmet:
                    item.defense = 6; // was 5
                    break;
                case ItemID.GoldChainmail:
                    item.defense = 8; // was 6
                    break;
                case ItemID.GoldGreaves:
                    item.defense = 6; // was 4
                    break;

                // === PLATINUM ARMOR ===
                case ItemID.PlatinumHelmet:
                    item.defense = 6; // was 5
                    break;
                case ItemID.PlatinumChainmail:
                    item.defense = 8; // was 6
                    break;
                case ItemID.PlatinumGreaves:
                    item.defense = 6; // was 4
                    break;
            }
        }
    }
}