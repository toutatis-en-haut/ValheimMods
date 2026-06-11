using BepInEx.Configuration;
using UnityEngine;

namespace ExtendedStorage.Config
{
    internal static class CabinetConfig
    {
        public static ConfigEntry<int> WoodAmount;
        public static ConfigEntry<int> ResinAmount;
        public static ConfigEntry<int> WorkbenchLevel;
        public static ConfigEntry<float> HitPoints;
        public static ConfigEntry<KeyboardShortcut> EditLabelHotkey;
        public static ConfigEntry<int> HoverCellSize;
        public static ConfigEntry<bool> HoverIgnoreWard;

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

            EditLabelHotkey = cfg.Bind(
                "UI", "EditLabelHotkey",
                new KeyboardShortcut(KeyCode.E, KeyCode.LeftShift),
                "Hotkey to enter tab-label edit mode while hovering a tab. " +
                "Default Shift+E. Press Enter to commit, Esc to discard.");

            HoverCellSize = cfg.Bind(
                "Hover", "CellSize", 36,
                new ConfigDescription(
                    "Icon cell size (px) for the shift-hover content panel.",
                    new AcceptableValueRange<int>(24, 64)));
            HoverIgnoreWard = cfg.Bind(
                "Hover", "IgnoreWard", false,
                "When true, the shift-hover content panel shows contents even " +
                "if the cabinet is inside another player's ward. Useful in solo play.");
        }
    }
}
