using HarmonyLib;
using SpawnPointGateways.Config;
using SpawnPointGateways.Items;
using SpawnPointGateways.SpawnPoints;
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

            bool costEnabled = GatewayConfig.CostEnabled.Value;
            string costPrefab = GatewayConfig.CostItem.Value;
            int needed = costEnabled ? Mathf.Max(0, GatewayConfig.CostAmount.Value) : 0;
            int have = costEnabled ? CostItemHelper.Count(inv, costPrefab) : 0;

            if (costEnabled && have < needed)
            {
                string itemDisplay = CostItemHelper.ResolveDisplayName(costPrefab);
                SpawnPointGatewaysPlugin.Log?.LogInfo($"[SPG] Charm activated but only {have}/{needed} {costPrefab} available.");
                var template = Localization.instance != null
                    ? Localization.instance.Localize("$bifrost_charm_msg_no_cost")
                    : "You need {0} {1} to light the charm.";
                player.Message(MessageHud.MessageType.Center,
                    template.Replace("{0}", needed.ToString()).Replace("{1}", itemDisplay));
                return;
            }

            // Prune orphan/foreign-owned spawn points before showing the markers so the
            // map only ever offers valid destinations.
            SpawnPointReconciler.Run(player);

            GatewayState.ArmForDestination();
            SpawnPointGatewaysPlugin.Log?.LogInfo(
                costEnabled
                    ? $"[SPG] Charm armed (cost check passed: {have}/{needed} {costPrefab}). State -> AwaitingDestination."
                    : "[SPG] Charm armed (cost disabled). State -> AwaitingDestination.");
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
