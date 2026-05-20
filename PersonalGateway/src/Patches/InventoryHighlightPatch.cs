using System.Collections;
using HarmonyLib;
using PersonalGateway.Items;
using PersonalGateway.State;
using UnityEngine;
using UnityEngine.UI;

namespace PersonalGateway.Patches
{
    [HarmonyPatch(typeof(InventoryGrid))]
    internal static class InventoryHighlightPatch
    {
        private static readonly Color HighlightColor = new Color(1f, 0.82f, 0.18f, 1f);

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGrid.UpdateInventory))]
        private static void UpdateInventory_Postfix(InventoryGrid __instance)
        {
            if (__instance == null) return;

            var elements = Traverse.Create(__instance).Field("m_elements").GetValue() as IList;
            var inventory = Traverse.Create(__instance).Field("m_inventory").GetValue<Inventory>();
            if (elements == null || inventory == null) return;

            bool highlight = GatewayState.Phase == ArmingPhase.AwaitingTrophy;
            int width = inventory.GetWidth();
            if (width <= 0) return;

            float pulse = highlight ? (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f)) : 1f;
            var pulseColor = new Color(HighlightColor.r, HighlightColor.g * pulse, HighlightColor.b * pulse, 1f);

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null) continue;
                var icon = Traverse.Create(element).Field("m_icon").GetValue<Image>();
                if (icon == null) continue;

                int x = i % width;
                int y = i / width;
                var item = inventory.GetItemAt(x, y);

                if (highlight && item != null && TrophyRegistry.IsTrophy(item))
                {
                    icon.color = pulseColor;
                }
                else if (icon.color != Color.white && (item == null || !IsLowDurability(item)))
                {
                    icon.color = Color.white;
                }
            }
        }

        private static bool IsLowDurability(ItemDrop.ItemData item)
        {
            return item.m_shared != null && item.m_shared.m_useDurability && item.m_durability < 1f;
        }
    }
}
