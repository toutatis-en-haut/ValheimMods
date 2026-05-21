using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using SpawnPointGateways.Config;
using SpawnPointGateways.Items;
using SpawnPointGateways.UI;

namespace SpawnPointGateways
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class SpawnPointGatewaysPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "studio.tribus.spawnpointgateways";
        public const string ModName = "Spawn Point Gateways";
        public const string ModVersion = "0.1.3";

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

        private void Update()
        {
            SpawnMarkerOverlay.Tick();
            TeleportCursor.Tick();
            Teleport.TeleportController.Tick();
        }

        private void OnDestroy()
        {
            Harmony?.UnpatchSelf();
        }
    }
}
