using HarmonyLib;
using PersonalGateway.Items;
using PersonalGateway.State;
using UnityEngine;

namespace PersonalGateway.Patches
{
    [HarmonyPatch(typeof(InventoryGrid))]
    internal static class InventoryHighlightPatch
    {
        private static readonly Color HighlightColor = new Color(1f, 0.82f, 0.18f, 1f);

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGrid.UpdateGui))]
        private static void UpdateGui_Postfix(InventoryGrid __instance)
        {
            if (__instance == null || __instance.m_elements == null) return;

            var inventoryField = Traverse.Create(__instance).Field("m_inventory");
            var inventory = inventoryField.GetValue<Inventory>();
            if (inventory == null) return;

            bool highlight = GatewayState.Phase == ArmingPhase.AwaitingTrophy;
            int width = inventory.GetWidth();
            if (width <= 0) return;

            var elements = __instance.m_elements;
            float pulse = highlight ? (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f)) : 1f;
            var pulseColor = new Color(HighlightColor.r, HighlightColor.g * pulse, HighlightColor.b * pulse, 1f);

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null || element.m_icon == null) continue;

                int x = i % width;
                int y = i / width;
                var item = inventory.GetItemAt(x, y);

                if (highlight && item != null && TrophyRegistry.IsTrophy(item))
                {
                    element.m_icon.color = pulseColor;
                }
                else if (element.m_icon.color != Color.white && (item == null || !IsLowDurability(item)))
                {
                    element.m_icon.color = Color.white;
                }
            }
        }

        private static bool IsLowDurability(ItemDrop.ItemData item)
        {
            return item.m_shared != null && item.m_shared.m_useDurability && item.m_durability < 1f;
        }
    }
}
