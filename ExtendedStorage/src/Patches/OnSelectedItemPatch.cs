using ExtendedStorage.Storage;
using HarmonyLib;
using UnityEngine;

namespace ExtendedStorage.Patches
{
    // Vanilla Valheim's InventoryGui code accesses Container.m_inventory
    // directly (same-assembly access bypasses our GetInventory postfix), so
    // shift-click move-to-chest always lands in tab 0. This prefix intercepts
    // the player-side Move action and routes it through the cabinet's active
    // tab instead.
    //
    // We key off InventoryGrid.Modifier.Move — Valheim derives this from the
    // user's configured key binding, so any rebinding of the move shortcut
    // continues to work.
    [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
    internal static class OnSelectedItemPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            InventoryGui __instance,
            InventoryGrid grid,
            ItemDrop.ItemData item,
            Vector2i pos,
            InventoryGrid.Modifier mod)
        {
            if (item == null || grid == null) return true;

            // m_currentContainer is private to Valheim; track our own.
            var cab = InventoryGuiPatches.CurrentCabinet;
            if (cab == null || !cab.IsReady) return true;

            // Diagnostic — emitted only when a cabinet is open so we don't
            // spam the log during normal inventory use. Remove once the
            // routing is confirmed to work end-to-end.
            var sourceInv = grid.GetInventory();
            var player = Player.m_localPlayer;
            var playerInv = player?.GetInventory();
            ExtendedStoragePlugin.Log.LogInfo(
                $"[OnSelectedItem] mod={mod}, ActiveTab={cab.Storage.ActiveTab}, " +
                $"sourceInv='{sourceInv?.GetName() ?? "null"}' (slots={sourceInv?.GetWidth()}x{sourceInv?.GetHeight()}), " +
                $"playerInv match={(sourceInv == playerInv)}, item={item.m_dropPrefab?.name}");

            if (mod != InventoryGrid.Modifier.Move) return true;
            if (cab.Storage.ActiveTab == 0) return true; // vanilla already correct

            if (player == null || playerInv == null) return true;

            var activeTabInv = cab.GetTab(cab.Storage.ActiveTab);
            if (activeTabInv == null) return true;

            if (sourceInv == playerInv)
            {
                // Player -> chest active tab
                activeTabInv.MoveItemToThis(playerInv, item);
                return false;
            }
            if (sourceInv == activeTabInv)
            {
                // Active tab -> player
                playerInv.MoveItemToThis(activeTabInv, item);
                return false;
            }

            return true;
        }
    }
}
