using ExtendedStorage.Config;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ExtendedStorage.Pieces
{
    internal static class WoodenCabinetPiece
    {
        public const string PrefabName = "ExtendedStorage_WoodenCabinet";
        public const string SourcePrefab = "piece_chest_wood";
        public const string NameToken = "$piece_woodencabinet";
        public const string DescriptionToken = "$piece_woodencabinet_description";

        public static GameObject Prefab { get; private set; }

        public static void Register()
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, SourcePrefab);
            if (prefab == null)
            {
                ExtendedStoragePlugin.Log.LogError(
                    $"Could not clone '{SourcePrefab}'; WoodenCabinet not registered.");
                return;
            }

            var wnt = prefab.GetComponent<WearNTear>();
            if (wnt != null)
            {
                wnt.m_health = CabinetConfig.HitPoints.Value;
            }

            var container = prefab.GetComponent<Container>();
            if (container != null)
            {
                container.m_name = NameToken;
            }

            var piece = prefab.GetComponent<Piece>();
            if (piece != null)
            {
                piece.m_name = NameToken;
                piece.m_description = DescriptionToken;
            }

            var customPiece = new CustomPiece(prefab, fixReference: false, new PieceConfig
            {
                Name = NameToken,
                Description = DescriptionToken,
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Furniture,
                CraftingStation = CraftingStations.Workbench,
                AllowedInDungeons = false,
                Requirements = new[]
                {
                    new RequirementConfig { Item = "Wood",  Amount = CabinetConfig.WoodAmount.Value,  Recover = true },
                    new RequirementConfig { Item = "Resin", Amount = CabinetConfig.ResinAmount.Value, Recover = true }
                }
            });

            PieceManager.Instance.AddPiece(customPiece);
            Prefab = prefab;

            ExtendedStoragePlugin.Log.LogInfo(
                $"Registered piece '{PrefabName}' (HP={(wnt != null ? wnt.m_health : 0f)}).");
        }
    }
}
