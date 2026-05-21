using HarmonyLib;
using SpawnPointGateways.SpawnPoints;
using UnityEngine;

namespace SpawnPointGateways.Patches
{
    /// <summary>
    /// Records every bed position the local player marks as their spawn.
    /// Hooks <see cref="PlayerProfile.SetCustomSpawnPoint"/>, which Bed.Interact
    /// calls in both the first-claim and re-claim branches — every other vanilla
    /// path writes the profile's spawn fields directly, so this is the single
    /// chokepoint for interactive "Set Spawn" events on a bed.
    /// </summary>
    [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.SetCustomSpawnPoint), new[] { typeof(Vector3) })]
    internal static class BedSpawnPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Vector3 point)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            SpawnPointRegistry.Record(player, point);
        }
    }
}
