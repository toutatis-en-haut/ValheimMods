using ExtendedStorage.Storage;
using ExtendedStorage.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedStorage.Patches
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class InventoryGuiPatches
    {
        private static CabinetTabStrip s_strip;
        private static CabinetContainer s_currentCabinet;

        [HarmonyPatch(nameof(InventoryGui.Show))]
        [HarmonyPostfix]
        private static void Show_Postfix(InventoryGui __instance, Container container)
        {
            TeardownStrip();

            if (container == null) return;
            var cab = container.GetComponent<CabinetContainer>();
            if (cab == null || !cab.IsReady) return;

            // Reset to tab 0 on each open — keeps view predictable.
            cab.Storage.ActiveTab = 0;
            RebindGrid(__instance, cab, 0);

            BuildStrip(__instance, cab);
            s_currentCabinet = cab;
        }

        [HarmonyPatch(nameof(InventoryGui.Hide))]
        [HarmonyPostfix]
        private static void Hide_Postfix()
        {
            TeardownStrip();
        }

        [HarmonyPatch(nameof(InventoryGui.OnDestroy))]
        [HarmonyPostfix]
        private static void OnDestroy_Postfix()
        {
            TeardownStrip();
        }

        [HarmonyPatch(nameof(InventoryGui.Update))]
        [HarmonyPostfix]
        private static void Update_Postfix(InventoryGui __instance)
        {
            if (s_currentCabinet == null || s_strip == null) return;
            if (!__instance.IsVisible()) return;

            for (int i = 0; i < CabinetStorage.TabCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    ActivateTab(__instance, i);
                    break;
                }
            }
        }

        private static void BuildStrip(InventoryGui gui, CabinetContainer cab)
        {
            var containerTransform = gui.m_container != null ? gui.m_container.transform : null;
            if (containerTransform == null)
            {
                ExtendedStoragePlugin.Log.LogWarning("InventoryGui.m_container has no transform; cannot place tab strip.");
                return;
            }

            // Strip lives one layer up so it can extend above the chest panel
            // without being clipped by the grid's own mask.
            var stripParent = containerTransform.parent;
            if (stripParent == null) stripParent = containerTransform;

            var panelRect = stripParent as RectTransform ?? containerTransform as RectTransform;
            float stripWidth = panelRect != null ? panelRect.rect.width : 360f;

            var font = ResolveFont(gui);

            s_strip = CabinetTabStrip.Build(stripParent, cab, stripWidth, font);
            s_strip.OnTabActivated = idx => RebindGrid(gui, cab, idx);

            // Position the strip just above the chest panel.
            var rect = s_strip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(0f, 4f);
        }

        private static void ActivateTab(InventoryGui gui, int idx)
        {
            if (s_currentCabinet == null) return;
            s_strip?.SetActive(idx);
            RebindGrid(gui, s_currentCabinet, idx);
        }

        private static void RebindGrid(InventoryGui gui, CabinetContainer cab, int idx)
        {
            cab.Storage.ActiveTab = idx;
            var inv = cab.GetTab(idx);
            if (inv == null || gui.m_container == null) return;
            var player = Player.m_localPlayer;
            gui.m_container.UpdateInventory(inv, player, "");
        }

        private static void TeardownStrip()
        {
            if (s_strip != null)
            {
                Object.Destroy(s_strip.gameObject);
                s_strip = null;
            }
            s_currentCabinet = null;
        }

        private static Font ResolveFont(InventoryGui gui)
        {
            // Borrow a font from any Text component under the inventory GUI.
            var anyText = gui.GetComponentInChildren<Text>(includeInactive: true);
            return anyText != null ? anyText.font : Font.CreateDynamicFontFromOSFont("Arial", 14);
        }
    }
}
