using System.Collections.Generic;
using UnityEngine;

namespace SpawnPointGateways.Items
{
    /// <summary>
    /// Counts and removes items by prefab name. Vanilla
    /// <see cref="Inventory.CountItems(string)"/> matches against m_shared.m_name
    /// (a localization token like "$item_resin"), not the prefab name, so passing
    /// "Resin" silently returns 0. We compare against m_dropPrefab.name to fix that.
    /// </summary>
    internal static class CostItemHelper
    {
        public static int Count(Inventory inv, string prefabName)
        {
            if (inv == null || string.IsNullOrEmpty(prefabName)) return 0;
            int total = 0;
            foreach (var item in inv.GetAllItems())
            {
                if (item?.m_dropPrefab != null && item.m_dropPrefab.name == prefabName)
                {
                    total += item.m_stack;
                }
            }
            return total;
        }

        public static bool Remove(Inventory inv, string prefabName, int amount)
        {
            if (amount <= 0) return true;
            if (inv == null || string.IsNullOrEmpty(prefabName)) return false;

            var stacks = new List<ItemDrop.ItemData>();
            foreach (var item in inv.GetAllItems())
            {
                if (item?.m_dropPrefab != null && item.m_dropPrefab.name == prefabName)
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

        public static string ResolveDisplayName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return "?";
            var db = ObjectDB.instance;
            if (db == null) return prefabName;
            var prefab = db.GetItemPrefab(prefabName);
            if (prefab == null) return prefabName;
            var drop = prefab.GetComponent<ItemDrop>();
            if (drop == null || drop.m_itemData?.m_shared == null) return prefabName;
            var token = drop.m_itemData.m_shared.m_name;
            if (string.IsNullOrEmpty(token)) return prefabName;
            return Localization.instance != null ? Localization.instance.Localize(token) : token;
        }
    }
}
