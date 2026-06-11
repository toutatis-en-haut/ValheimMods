using ExtendedStorage.Config;
using ExtendedStorage.Storage;
using ExtendedStorage.UI;
using HarmonyLib;
using UnityEngine;

namespace ExtendedStorage.Patches
{
    [HarmonyPatch(typeof(Hud))]
    internal static class HudCrosshairPatch
    {
        // Hud.UpdateCrosshair is private in vanilla Valheim — match by string
        // literal so a future visibility change doesn't break the patch.
        [HarmonyPatch("UpdateCrosshair")]
        [HarmonyPostfix]
        private static void UpdateCrosshair_Postfix()
        {
            try { Tick(); }
            catch (System.Exception ex)
            {
                ExtendedStoragePlugin.Log.LogError($"Cabinet hover tick failed: {ex.Message}");
                CabinetHoverPanel.Hide();
            }
        }

        private static void Tick()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                CabinetHoverPanel.Hide();
                return;
            }

            // Suppress while another full-screen UI is competing for attention.
            if (InventoryGui.IsVisible() || IsBuildMenuOpen() || Minimap.IsOpen())
            {
                CabinetHoverPanel.Hide();
                return;
            }

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!shift)
            {
                CabinetHoverPanel.Hide();
                return;
            }

            var hovering = player.m_hovering;
            if (hovering == null)
            {
                CabinetHoverPanel.Hide();
                return;
            }

            var cab = hovering.GetComponentInParent<CabinetContainer>();
            if (cab == null || !cab.IsReady)
            {
                CabinetHoverPanel.Hide();
                return;
            }

            if (!CabinetConfig.HoverIgnoreWard.Value &&
                !PrivateArea.CheckAccess(cab.transform.position, 0f, false))
            {
                CabinetHoverPanel.Hide();
                return;
            }

            CabinetHoverPanel.ShowFor(cab);
        }

        private static bool IsBuildMenuOpen()
        {
            var hud = Hud.instance;
            return hud != null
                && hud.m_pieceSelectionWindow != null
                && hud.m_pieceSelectionWindow.activeSelf;
        }
    }
}
