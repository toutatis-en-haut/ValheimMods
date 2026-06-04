using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
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
        public const string ModVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static Harmony Harmony;
        internal static ExtendedStoragePlugin Instance;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Harmony = new Harmony(ModGuid);
            Harmony.PatchAll();

            Log.LogInfo($"{ModName} v{ModVersion} loaded.");
        }

        private void OnDestroy()
        {
            Harmony?.UnpatchSelf();
        }
    }
}
