using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SpawnPointGateways.SpawnPoints
{
    /// <summary>
    /// Scans ZDOMan for every bed ZDO owned by the local player in the current world
    /// and prunes any recorded spawn point that no longer has a matching bed. A bed
    /// the player no longer owns (owner field changed) is treated as gone — the
    /// marker is removed.
    /// </summary>
    internal static class SpawnPointReconciler
    {
        // Beds spawn the player slightly above their root transform (m_spawnPoint
        // child), so the stored spawn position can be 1–2m off the ZDO position.
        // 5m is generous but small enough not to false-match a neighbouring bed.
        private const float MatchRadiusMeters = 5f;

        public static void Run(Player player)
        {
            if (player == null) return;
            if (ZDOMan.instance == null || ZNetScene.instance == null || Game.instance == null)
            {
                SpawnPointGatewaysPlugin.Log?.LogInfo("[SPG] Reconciliation skipped: world not ready.");
                return;
            }

            long playerId = TryGetLocalPlayerId();
            if (playerId == 0L)
            {
                SpawnPointGatewaysPlugin.Log?.LogInfo("[SPG] Reconciliation skipped: player ID not available.");
                return;
            }

            var bedHashes = CollectBedPrefabHashes();
            if (bedHashes.Count == 0)
            {
                SpawnPointGatewaysPlugin.Log?.LogWarning("[SPG] Reconciliation skipped: no Bed prefabs found in ZNetScene.");
                return;
            }

            var ownedBedPositions = CollectOwnedBedPositions(bedHashes, playerId);
            ulong worldUid = TryGetCurrentWorldUid();

            SpawnPointGatewaysPlugin.Log?.LogInfo(
                $"[SPG] Reconciliation: world={worldUid}, playerId={playerId}, {bedHashes.Count} bed prefab(s) known, {ownedBedPositions.Count} owned bed(s) in world.");

            SpawnPointRegistry.PruneToMatches(player, worldUid, ownedBedPositions, MatchRadiusMeters);
        }

        private static long TryGetLocalPlayerId()
        {
            try
            {
                var profile = Game.instance?.GetPlayerProfile();
                if (profile == null) return 0L;
                return profile.GetPlayerID();
            }
            catch (System.Exception ex)
            {
                SpawnPointGatewaysPlugin.Log?.LogWarning($"[SPG] Could not read local player ID: {ex.Message}");
                return 0L;
            }
        }

        private static HashSet<int> CollectBedPrefabHashes()
        {
            var hashes = new HashSet<int>();
            var zns = ZNetScene.instance;
            if (zns == null) return hashes;
            var prefabs = zns.m_prefabs;
            if (prefabs == null) return hashes;
            foreach (var go in prefabs)
            {
                if (go == null) continue;
                if (go.GetComponent<Bed>() == null) continue;
                hashes.Add(go.name.GetStableHashCode());
            }
            return hashes;
        }

        private static List<Vector3> CollectOwnedBedPositions(HashSet<int> bedPrefabHashes, long playerId)
        {
            var result = new List<Vector3>();
            var zdoman = ZDOMan.instance;
            if (zdoman == null) return result;

            // Prefer the prefab-indexed lookup — much cheaper than walking every ZDO.
            var getAllByPrefab = AccessTools.Method(typeof(ZDOMan), "GetAllZDOsWithPrefab",
                new[] { typeof(int), typeof(List<ZDO>) });

            if (getAllByPrefab != null)
            {
                var temp = new List<ZDO>();
                foreach (var hash in bedPrefabHashes)
                {
                    temp.Clear();
                    try
                    {
                        getAllByPrefab.Invoke(zdoman, new object[] { hash, temp });
                    }
                    catch (System.Exception ex)
                    {
                        SpawnPointGatewaysPlugin.Log?.LogWarning(
                            $"[SPG] GetAllZDOsWithPrefab failed for hash {hash}: {ex.Message}");
                        continue;
                    }
                    foreach (var zdo in temp)
                    {
                        if (zdo == null) continue;
                        long owner = zdo.GetLong("owner", 0L);
                        if (owner != playerId) continue;
                        result.Add(zdo.GetPosition());
                    }
                }
                return result;
            }

            // Fallback: iterate every ZDO in the manager.
            var byIdField = AccessTools.Field(typeof(ZDOMan), "m_objectsByID");
            if (byIdField == null)
            {
                SpawnPointGatewaysPlugin.Log?.LogWarning(
                    "[SPG] ZDOMan has neither GetAllZDOsWithPrefab nor m_objectsByID — cannot reconcile.");
                return result;
            }
            if (!(byIdField.GetValue(zdoman) is IDictionary dict)) return result;
            foreach (var v in dict.Values)
            {
                if (!(v is ZDO zdo)) continue;
                if (!bedPrefabHashes.Contains(zdo.GetPrefab())) continue;
                long owner = zdo.GetLong("owner", 0L);
                if (owner != playerId) continue;
                result.Add(zdo.GetPosition());
            }
            return result;
        }

        private static ulong TryGetCurrentWorldUid()
        {
            try
            {
                if (ZNet.instance == null) return 0UL;
                var method = AccessTools.Method(typeof(ZNet), "GetWorldUID");
                if (method == null) return 0UL;
                var result = method.Invoke(ZNet.instance, System.Array.Empty<object>());
                if (result is long l) return unchecked((ulong)l);
                if (result is ulong u) return u;
                if (result is int i) return unchecked((ulong)i);
                if (result is uint ui) return ui;
            }
            catch { }
            return 0UL;
        }
    }
}
