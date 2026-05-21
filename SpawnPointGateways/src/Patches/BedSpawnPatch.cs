using HarmonyLib;
using SpawnPointGateways.SpawnPoints;
using UnityEngine;

namespace SpawnPointGateways.Patches
{
    /// <summary>
    /// Records every bed position the local player marks as their spawn.
    /// Hooks Player.SetCustomSpawnPoint, which is the choke point every "Set Spawn"
    /// path goes through (bed interact, debug commands, …).
    /// </summary>
    [HarmonyPatch(typeof(Player))]
    internal static class BedSpawnPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("SetCustomSpawnPoint")]
        private static void SetCustomSpawnPoint_Postfix(Player __instance, Vector3 point)
        {
            if (__instance == null) return;
            if (__instance != Player.m_localPlayer) return;
            SpawnPointRegistry.Record(__instance, point);
        }
    }
}
