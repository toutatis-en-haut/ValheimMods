using BepInEx.Configuration;
using UnityEngine;

namespace SpawnPointGateways.Config
{
    internal static class GatewayConfig
    {
        public static ConfigEntry<int> ResinCost;

        public static ConfigEntry<int> IngredientGreydwarfEye;
        public static ConfigEntry<int> IngredientFineWood;
        public static ConfigEntry<int> IngredientThistle;
        public static ConfigEntry<int> IngredientBlueberries;
        public static ConfigEntry<int> CraftingStationLevel;

        public static ConfigEntry<Color> MarkerColor;
        public static ConfigEntry<int> MarkerRadiusPixels;
        public static ConfigEntry<float> MarkerRingThickness;

        public static void Bind(ConfigFile cfg)
        {
            ResinCost = cfg.Bind(
                "Activation", "ResinCost", 20,
                "Resin consumed each time the Bifröst Charm is activated.");

            IngredientGreydwarfEye = cfg.Bind("Recipe", "GreydwarfEye", 10, "Greydwarf Eyes required to craft the Bifröst Charm.");
            IngredientFineWood = cfg.Bind("Recipe", "FineWood", 1, "Fine Wood required to craft the Bifröst Charm.");
            IngredientThistle = cfg.Bind("Recipe", "Thistle", 20, "Thistle required to craft the Bifröst Charm.");
            IngredientBlueberries = cfg.Bind("Recipe", "Blueberries", 10, "Blueberries required to craft the Bifröst Charm.");
            CraftingStationLevel = cfg.Bind("Recipe", "WorkbenchLevel", 2, "Workbench level required to craft the Bifröst Charm.");

            MarkerColor = cfg.Bind("UI", "MarkerColor", new Color(0.35f, 0.75f, 1.0f, 0.85f),
                "Color of the spawn-point markers drawn on the large map.");
            MarkerRadiusPixels = cfg.Bind("UI", "MarkerRadiusPixels", 18,
                "On-screen radius (pixels) of each spawn-point marker. Markers stay the same size regardless of map zoom.");
            MarkerRingThickness = cfg.Bind("UI", "MarkerRingThickness", 0.35f,
                "Ring thickness as a fraction of the marker radius (0.01 = thin line, 0.50 = solid disc). Clamped to [0.01, 0.50].");
        }
    }
}
