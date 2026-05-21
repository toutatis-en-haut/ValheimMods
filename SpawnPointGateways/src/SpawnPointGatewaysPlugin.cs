using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using SpawnPointGateways.Config;
using SpawnPointGateways.Items;
using SpawnPointGateways.SpawnPoints;
using SpawnPointGateways.UI;
using UnityEngine;

namespace SpawnPointGateways
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class SpawnPointGatewaysPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "toutatis.spawnpointgateways";
        public const string ModName = "Spawn Point Gateways";
        public const string ModVersion = "0.1.4";

        internal static ManualLogSource Log;
        internal static Harmony Harmony;
        internal static SpawnPointGatewaysPlugin Instance;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            GatewayConfig.Bind(Config);

            LocalizationLoader.Register();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;

            Harmony = new Harmony(ModGuid);
            Harmony.PatchAll();

            Log.LogInfo($"{ModName} v{ModVersion} loaded.");
        }

        private void OnVanillaPrefabsAvailable()
        {
            try
            {
                BifrostCharm.Register();
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to register Bifröst Charm: {ex}");
            }
            finally
            {
                PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
            }
        }

        // Per-session one-shot reconciliation: when the local player loads in, give
        // ZDOMan a few seconds to settle (peer sync on multiplayer clients, world
        // load on single-player) and then prune any spawn point whose bed no longer
        // exists or no longer belongs to the player.
        private const float ReconcileDelaySeconds = 5f;
        private float _localPlayerSeenAt = -1f;
        private bool _reconciledThisSession;

        private void Update()
        {
            SpawnMarkerOverlay.Tick();
            TeleportCursor.Tick();
            Teleport.TeleportController.Tick();
            TryReconcileOnce();
        }

        private void TryReconcileOnce()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                _localPlayerSeenAt = -1f;
                _reconciledThisSession = false;
                return;
            }
            if (_reconciledThisSession) return;
            if (_localPlayerSeenAt < 0f)
            {
                _localPlayerSeenAt = Time.unscaledTime;
                return;
            }
            if (Time.unscaledTime - _localPlayerSeenAt < ReconcileDelaySeconds) return;

            SpawnPointReconciler.Run(player);
            _reconciledThisSession = true;
        }

        private void OnDestroy()
        {
            Harmony?.UnpatchSelf();
        }
    }
}
