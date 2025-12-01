// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System;
using Terraria;
using Terraria.ModLoader;

namespace DuravoQOLMod.ArmorRebalance
{
    /// <summary>
    /// Tracks unique item instances via GUID.
    /// When Terraria clones items for tooltips, the GUID is preserved,
    /// allowing us to identify if a cloned item came from an equipped slot.
    /// </summary>
    public class ItemInstanceTracker : GlobalItem
    {
        /// <summary>
        /// Critical: Each Item gets its own copy of this GlobalItem.
        /// </summary>
        public override bool InstancePerEntity => true;

        /// <summary>
        /// Unique identifier for this item instance.
        /// Generated when the item is created, persists through cloning.
        /// </summary>
        public Guid InstanceId { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// When Terraria clones an item (e.g. for tooltip display),
        /// copy the same GUID so we know it refers to the same logical item.
        /// </summary>
        public override GlobalItem Clone(Item fromItem, Item toItem)
        {
            var clonedTracker = (ItemInstanceTracker)base.Clone(fromItem, toItem);
            clonedTracker.InstanceId = this.InstanceId;
            return clonedTracker;
        }

        /// <summary>
        /// Check if two items represent the same instance (even if one is a clone).
        /// </summary>
        public static bool AreSameInstance(Item itemA, Item itemB)
        {
            if (itemA == null || itemB == null) return false;
            if (itemA.IsAir || itemB.IsAir) return false;

            // Fast reference check first
            if (ReferenceEquals(itemA, itemB)) return true;

            // Fallback to GUID comparison
            if (itemA.TryGetGlobalItem(out ItemInstanceTracker trackerA) &&
                itemB.TryGetGlobalItem(out ItemInstanceTracker trackerB)) {
                return trackerA.InstanceId == trackerB.InstanceId;
            }

            return false;
        }
    }
}