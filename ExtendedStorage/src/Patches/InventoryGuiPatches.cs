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

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void Update_Postfix(InventoryGui __instance)
        {
            if (s_currentCabinet == null || s_strip == null) return;
            if (!InventoryGui.IsVisible()) return;

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
            // In current Valheim, m_container is the chest panel's RectTransform;
            // the InventoryGrid is a child component.
            var panel = gui.m_container;
            if (panel == null)
            {
                ExtendedStoragePlugin.Log.LogWarning("InventoryGui.m_container is null; cannot place tab strip.");
                return;
            }

            float stripWidth = panel.rect.width;
            var font = ResolveFont(gui);

            s_strip = CabinetTabStrip.Build(panel, cab, stripWidth, font);
            s_strip.OnTabActivated = idx => RebindGrid(gui, cab, idx);

            // Anchor to the top of the panel; pivot at bottom so the strip
            // extends upward, above the panel header.
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

            var grid = gui.m_container.GetComponentInChildren<InventoryGrid>(includeInactive: true);
            if (grid == null) return;
            grid.UpdateInventory(inv, Player.m_localPlayer, "");
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
