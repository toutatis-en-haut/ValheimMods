using HarmonyLib;
using SpawnPointGateways.Config;
using SpawnPointGateways.Items;
using SpawnPointGateways.State;
using UnityEngine;

namespace SpawnPointGateways.Patches
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

            if (BifrostCharm.IsCharm(item))
            {
                HandleCharmActivation(player);
                return false;
            }

            return true;
        }

        private static void HandleCharmActivation(Player player)
        {
            if (GatewayState.Phase != ArmingPhase.Idle)
            {
                SpawnPointGatewaysPlugin.Log?.LogInfo($"[SPG] Re-clicked charm while in '{GatewayState.Phase}'; cancelling.");
                GatewayState.Reset();
                player.Message(MessageHud.MessageType.Center, "$bifrost_charm_msg_cancelled");
                return;
            }

            var inv = player.GetInventory();
            if (inv == null) return;

            int needed = Mathf.Max(0, GatewayConfig.ResinCost.Value);
            int have = ResinHelper.Count(inv);
            if (have < needed)
            {
                SpawnPointGatewaysPlugin.Log?.LogInfo($"[SPG] Charm activated but only {have}/{needed} Resin available.");
                var template = Localization.instance != null
                    ? Localization.instance.Localize("$bifrost_charm_msg_no_resin")
                    : "Not enough resin ({0} needed).";
                player.Message(MessageHud.MessageType.Center, template.Replace("{0}", needed.ToString()));
                return;
            }

            GatewayState.ArmForDestination();
            SpawnPointGatewaysPlugin.Log?.LogInfo($"[SPG] Charm armed (resin check passed: {have}/{needed}). State -> AwaitingDestination.");
            player.Message(MessageHud.MessageType.Center, "$bifrost_charm_msg_channel");

            if (InventoryGui.instance != null)
            {
                InventoryGui.instance.Hide();
            }
            if (Minimap.instance != null)
            {
                Minimap.instance.SetMapMode(Minimap.MapMode.Large);
            }
        }
    }
}
