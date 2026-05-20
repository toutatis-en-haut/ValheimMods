using HarmonyLib;
using PersonalGateway.Items;
using PersonalGateway.State;
using UnityEngine;

namespace PersonalGateway.Patches
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class InventoryGuiPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnRightClickItem")]
        private static bool OnRightClickItem_Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos)
        {
            if (item == null) return true;
            var player = Player.m_localPlayer;
            if (player == null) return true;

            if (BifrostTotem.IsTotem(item))
            {
                HandleTotemActivation(player);
                return false;
            }

            if (GatewayState.Phase == ArmingPhase.AwaitingTrophy && TrophyRegistry.IsTrophy(item))
            {
                HandleTrophySelection(player, item);
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void Hide_Postfix()
        {
            if (GatewayState.Phase == ArmingPhase.AwaitingTrophy)
            {
                var player = Player.m_localPlayer;
                if (player != null)
                {
                    player.Message(MessageHud.MessageType.Center, "$bifrost_msg_cancelled");
                }
                GatewayState.Reset();
            }
        }

        private static void HandleTotemActivation(Player player)
        {
            if (GatewayState.Phase != ArmingPhase.Idle)
            {
                PersonalGatewayPlugin.Log?.LogInfo($"[Bifrost] Re-clicked totem while in '{GatewayState.Phase}'; cancelling.");
                GatewayState.Reset();
                player.Message(MessageHud.MessageType.Center, "$bifrost_msg_cancelled");
                return;
            }

            if (!HasAnyTrophy(player))
            {
                PersonalGatewayPlugin.Log?.LogInfo("[Bifrost] Totem activated but no trophy carried.");
                player.Message(MessageHud.MessageType.Center, "$bifrost_msg_no_trophies");
                return;
            }

            GatewayState.Arm();
            PersonalGatewayPlugin.Log?.LogInfo("[Bifrost] State -> AwaitingTrophy.");
            player.Message(MessageHud.MessageType.Center, "$bifrost_msg_armed");
        }

        private static void HandleTrophySelection(Player player, ItemDrop.ItemData trophy)
        {
            var trophyName = trophy?.m_dropPrefab != null ? trophy.m_dropPrefab.name : "?";
            GatewayState.SelectTrophy(trophy);
            PersonalGatewayPlugin.Log?.LogInfo($"[Bifrost] State -> AwaitingDestination (trophy: {trophyName}).");
            player.Message(MessageHud.MessageType.Center, "$bifrost_msg_channel");
            if (InventoryGui.instance != null)
            {
                InventoryGui.instance.Hide();
            }
            if (Minimap.instance != null)
            {
                Minimap.instance.SetMapMode(Minimap.MapMode.Large);
            }
        }

        private static bool HasAnyTrophy(Player player)
        {
            if (player == null || player.GetInventory() == null) return false;
            foreach (var i in player.GetInventory().GetAllItems())
            {
                if (TrophyRegistry.IsTrophy(i)) return true;
            }
            return false;
        }
    }
}
