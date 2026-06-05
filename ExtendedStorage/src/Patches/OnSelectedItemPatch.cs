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
            if (mod != InventoryGrid.Modifier.Move) return true;
            if (item == null || grid == null) return true;

            // m_currentContainer is private to Valheim; track our own.
            var cab = InventoryGuiPatches.CurrentCabinet;
            if (cab == null || !cab.IsReady) return true;
            if (cab.Storage.ActiveTab == 0) return true; // vanilla already correct

            var player = Player.m_localPlayer;
            if (player == null) return true;
            var playerInv = player.GetInventory();
            if (playerInv == null) return true;

            var activeTabInv = cab.GetTab(cab.Storage.ActiveTab);
            if (activeTabInv == null) return true;

            var sourceInv = grid.GetInventory();
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
