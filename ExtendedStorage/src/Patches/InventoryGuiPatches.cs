using ExtendedStorage.Storage;
using ExtendedStorage.UI;
using HarmonyLib;
using UnityEngine;

namespace ExtendedStorage.Patches
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class InventoryGuiPatches
    {
        private static CabinetTabStrip s_strip;
        private static CabinetContainer s_currentCabinet;
        private static RectTransform s_droppedPanel;
        private static Vector2 s_droppedPanelOriginalPos;

        internal static CabinetContainer CurrentCabinet => s_currentCabinet;

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

        // Q/E navigation is handled inside CabinetTabStrip.Update so its
        // lifecycle is bounded by the strip's own existence.

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

            s_strip = CabinetTabStrip.Build(panel, cab, stripWidth);
            s_strip.OnTabActivated = idx => RebindGrid(gui, cab, idx);

            // Strip is parented to the chest panel; anchor it to the panel's
            // top edge and let it extend upward (pivot at strip bottom).
            var rect = s_strip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(0f, 4f);

            // Drop the whole chest panel down by the strip's visual height so
            // the strip occupies what used to be empty space above the panel
            // — keeping the player inventory clear of the tabs.
            float dropAmount = s_strip.TotalHeight + 4f;
            s_droppedPanel = panel;
            s_droppedPanelOriginalPos = panel.anchoredPosition;
            panel.anchoredPosition = new Vector2(
                s_droppedPanelOriginalPos.x,
                s_droppedPanelOriginalPos.y - dropAmount);
        }

        private static void RebindGrid(InventoryGui gui, CabinetContainer cab, int idx)
        {
            cab.Storage.ActiveTab = idx;
            var inv = cab.GetTab(idx);
            if (inv == null || gui.m_container == null) return;

            var grid = gui.m_container.GetComponentInChildren<InventoryGrid>(includeInactive: true);
            if (grid == null) return;
            grid.UpdateInventory(inv, Player.m_localPlayer, (ItemDrop.ItemData)null);
        }

        private static void TeardownStrip()
        {
            if (s_strip != null)
            {
                Object.Destroy(s_strip.gameObject);
                s_strip = null;
            }
            if (s_droppedPanel != null)
            {
                s_droppedPanel.anchoredPosition = s_droppedPanelOriginalPos;
                s_droppedPanel = null;
            }
            s_currentCabinet = null;
        }

    }
}
