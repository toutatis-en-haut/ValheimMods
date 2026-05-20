using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using PersonalGateway.Config;
using PersonalGateway.Items;
using PersonalGateway.Skills;
using PersonalGateway.UI;
using UnityEngine;

namespace PersonalGateway
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class PersonalGatewayPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "studio.tribus.personalgateway";
        public const string ModName = "Personal Gateway";
        public const string ModVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static Harmony Harmony;
        internal static PersonalGatewayPlugin Instance;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            GatewayConfig.Bind(Config);

            BifrostSkill.Register();
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
                BifrostTotem.Register();
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to register Bifröst Totem: {ex}");
            }
            finally
            {
                PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
            }
        }

        private void Update()
        {
            MapRangeOverlay.Tick();
            Teleport.TeleportController.Tick();
        }

        private void OnDestroy()
        {
            Harmony?.UnpatchSelf();
        }
    }
}
