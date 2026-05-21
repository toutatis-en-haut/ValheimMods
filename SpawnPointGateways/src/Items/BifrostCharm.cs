using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using SpawnPointGateways.Config;
using UnityEngine;

namespace SpawnPointGateways.Items
{
    internal static class BifrostCharm
    {
        public const string PrefabName = "BifrostCharm";
        public const string PreferredClone = "YagluthDrop";
        public const string FallbackClone = "BoneFragments";
        public const string LocalizedNameToken = "$bifrost_charm_name";
        public const string LocalizedDescToken = "$bifrost_charm_desc";

        public static GameObject Prefab { get; private set; }

        public static void Register()
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, PreferredClone);
            string usedSource = PreferredClone;
            if (prefab == null || prefab.GetComponent<ItemDrop>() == null)
            {
                if (prefab != null) UnityEngine.Object.Destroy(prefab);
                SpawnPointGatewaysPlugin.Log.LogInfo($"Could not clone '{PreferredClone}'; falling back to '{FallbackClone}'.");
                prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, FallbackClone);
                usedSource = FallbackClone;
            }

            if (prefab == null)
            {
                SpawnPointGatewaysPlugin.Log.LogError($"Could not clone any base prefab for the Bifröst Charm.");
                return;
            }

            var itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null || itemDrop.m_itemData == null || itemDrop.m_itemData.m_shared == null)
            {
                SpawnPointGatewaysPlugin.Log.LogError($"Cloned prefab '{usedSource}' missing ItemDrop/SharedData.");
                return;
            }
            SpawnPointGatewaysPlugin.Log.LogInfo($"Cloned Bifröst Charm from '{usedSource}'.");

            var shared = itemDrop.m_itemData.m_shared;
            var sprite = AssetLoader.TryLoadSprite("assets/icons/bifrost_charm.png", keyWhiteToAlpha: true)
                         ?? StarIconBuilder.CreateStarSprite(128);
            shared.m_name = LocalizedNameToken;
            shared.m_description = LocalizedDescToken;
            shared.m_icons = new[] { sprite };
            shared.m_itemType = ItemDrop.ItemData.ItemType.Misc;
            shared.m_maxStackSize = 1;
            shared.m_weight = 0.5f;
            shared.m_teleportable = true;
            shared.m_questItem = false;

            var item = new CustomItem(prefab, fixReference: false, new ItemConfig
            {
                Name = LocalizedNameToken,
                Description = LocalizedDescToken,
                CraftingStation = CraftingStations.Workbench,
                MinStationLevel = GatewayConfig.CraftingStationLevel.Value,
                Amount = 1,
                Requirements = new[]
                {
                    new RequirementConfig { Item = "GreydwarfEye", Amount = GatewayConfig.IngredientGreydwarfEye.Value },
                    new RequirementConfig { Item = "FineWood", Amount = GatewayConfig.IngredientFineWood.Value },
                    new RequirementConfig { Item = "Thistle", Amount = GatewayConfig.IngredientThistle.Value },
                    new RequirementConfig { Item = "Blueberries", Amount = GatewayConfig.IngredientBlueberries.Value }
                }
            });

            ItemManager.Instance.AddItem(item);
            Prefab = prefab;
            SpawnPointGatewaysPlugin.Log.LogInfo($"Registered item '{PrefabName}'.");
        }

        public static bool IsCharm(ItemDrop.ItemData item)
        {
            if (item == null || item.m_dropPrefab == null) return false;
            return item.m_dropPrefab.name == PrefabName;
        }

        public static bool PlayerHasCharm(Player player)
        {
            if (player == null || player.GetInventory() == null) return false;
            foreach (var i in player.GetInventory().GetAllItems())
            {
                if (IsCharm(i)) return true;
            }
            return false;
        }
    }
}
