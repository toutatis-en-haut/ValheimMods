using System.Collections.Generic;
using UnityEngine;

namespace SpawnPointGateways.Items
{
    /// <summary>
    /// Counts and removes Resin by prefab name. Vanilla
    /// <see cref="Inventory.CountItems(string)"/> matches against m_shared.m_name
    /// (the localization token like "$item_resin"), not the prefab name, so passing
    /// "Resin" silently returns 0 — which is exactly the bug we hit in v0.1.x.
    /// </summary>
    internal static class ResinHelper
    {
        public const string ResinPrefab = "Resin";

        public static int Count(Inventory inv)
        {
            if (inv == null) return 0;
            int total = 0;
            foreach (var item in inv.GetAllItems())
            {
                if (item?.m_dropPrefab != null && item.m_dropPrefab.name == ResinPrefab)
                {
                    total += item.m_stack;
                }
            }
            return total;
        }

        public static bool Remove(Inventory inv, int amount)
        {
            if (amount <= 0) return true;
            if (inv == null) return false;

            var stacks = new List<ItemDrop.ItemData>();
            foreach (var item in inv.GetAllItems())
            {
                if (item?.m_dropPrefab != null && item.m_dropPrefab.name == ResinPrefab)
                {
                    stacks.Add(item);
                }
            }

            int remaining = amount;
            foreach (var stack in stacks)
            {
                if (remaining <= 0) break;
                int take = Mathf.Min(remaining, stack.m_stack);
                inv.RemoveItem(stack, take);
                remaining -= take;
            }
            return remaining <= 0;
        }
    }
}
