using BepInEx;
using BepInEx.Logging;
using ExtendedStorage.Config;
using ExtendedStorage.Pieces;
using ExtendedStorage.State;
using ExtendedStorage.UI;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

namespace ExtendedStorage
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class ExtendedStoragePlugin : BaseUnityPlugin
    {
        public const string ModGuid = "toutatis.extended_storage";
        public const string ModName = "Extended Storage";
        public const string ModVersion = "0.1.3";

        internal static ManualLogSource Log;
        internal static Harmony Harmony;
        internal static ExtendedStoragePlugin Instance;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            CabinetConfig.Bind(Config);
            LocalizationLoader.Register();
            CabinetDebugCommands.Register();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;

            Harmony = new Harmony(ModGuid);
            Harmony.PatchAll();

            Log.LogInfo($"{ModName} v{ModVersion} loaded.");
        }

        private void OnVanillaPrefabsAvailable()
        {
            try
            {
                WoodenCabinetPiece.Register();
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to register Wooden Cabinet: {ex}");
            }
            finally
            {
                PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
            }
        }

        private void OnDestroy()
        {
            Harmony?.UnpatchSelf();
        }
    }
}
