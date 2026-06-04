using BepInEx.Configuration;

namespace ExtendedStorage.Config
{
    internal static class CabinetConfig
    {
        public static ConfigEntry<int> WoodAmount;
        public static ConfigEntry<int> ResinAmount;
        public static ConfigEntry<int> WorkbenchLevel;
        public static ConfigEntry<float> HitPoints;

        public static void Bind(ConfigFile cfg)
        {
            WoodAmount = cfg.Bind(
                "WoodenCabinet", "WoodAmount", 100,
                "Wood required to craft the Wooden Cabinet.");
            ResinAmount = cfg.Bind(
                "WoodenCabinet", "ResinAmount", 20,
                "Resin required to craft the Wooden Cabinet.");
            WorkbenchLevel = cfg.Bind(
                "WoodenCabinet", "WorkbenchLevel", 3,
                "Workbench level required to craft the Wooden Cabinet. " +
                "Enforced once the workbench-level gate is wired (Phase 2 ships with proximity-only).");
            HitPoints = cfg.Bind(
                "WoodenCabinet", "HitPoints", 1000f,
                "Hit points of the Wooden Cabinet (durability before destruction).");
        }
    }
}
