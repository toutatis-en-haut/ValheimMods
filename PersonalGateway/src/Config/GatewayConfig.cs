using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace PersonalGateway.Config
{
    internal static class GatewayConfig
    {
        public static ConfigEntry<KeyCode> TeleportModifierKey;
        public static ConfigEntry<int> TeleportMouseButton;
        public static ConfigEntry<float> DoubleClickWindowSeconds;

        public static ConfigEntry<int> MaxSkillLevel;
        public static ConfigEntry<float> MaxTeleportRangeMeters;

        public static ConfigEntry<int> IngredientGreydwarfEye;
        public static ConfigEntry<int> IngredientThistle;
        public static ConfigEntry<int> IngredientDandelion;
        public static ConfigEntry<int> IngredientBlueberries;
        public static ConfigEntry<int> CraftingStationLevel;

        public static ConfigEntry<bool> ShowRangeCircle;
        public static ConfigEntry<Color> RangeCircleColor;
        public static ConfigEntry<bool> RequireDoubleClick;

        public static ConfigEntry<float> XpCommon;
        public static ConfigEntry<float> XpUncommon;
        public static ConfigEntry<float> XpRare;
        public static ConfigEntry<float> XpBoss;

        public static ConfigEntry<string> TrophyTierUncommon;
        public static ConfigEntry<string> TrophyTierRare;
        public static ConfigEntry<string> TrophyTierBoss;

        public static void Bind(ConfigFile cfg)
        {
            TeleportModifierKey = cfg.Bind(
                "Controls", "TeleportModifierKey", KeyCode.None,
                "Optional modifier key held while clicking the map to commit a teleport. Default None (no modifier required while the totem is armed).");
            TeleportMouseButton = cfg.Bind(
                "Controls", "TeleportMouseButton", 0,
                "Mouse button used to commit a teleport. 0=Left, 1=Right, 2=Middle.");
            RequireDoubleClick = cfg.Bind(
                "Controls", "RequireDoubleClick", false,
                "If true, require a double-click on the map to commit. Default false (single click while the gold-star cursor is active).");
            DoubleClickWindowSeconds = cfg.Bind(
                "Controls", "DoubleClickWindowSeconds", 0.4f,
                "Maximum seconds between two clicks for a double-click to register (only used if RequireDoubleClick is true).");

            MaxSkillLevel = cfg.Bind(
                "Skill", "MaxSkillLevel", 100,
                "Maximum Bifröst skill level. Range circle disappears at this level.");
            MaxTeleportRangeMeters = cfg.Bind(
                "Skill", "MaxTeleportRangeMeters", 35000f,
                "Range in meters available at max skill. Range at level L = MaxTeleportRangeMeters * (L / MaxSkillLevel).");

            IngredientGreydwarfEye = cfg.Bind("Recipe", "GreydwarfEye", 10, "Greydwarf Eyes required to craft the Bifröst Totem.");
            IngredientThistle = cfg.Bind("Recipe", "Thistle", 10, "Thistles required to craft the Bifröst Totem.");
            IngredientDandelion = cfg.Bind("Recipe", "Dandelion", 10, "Dandelions required to craft the Bifröst Totem.");
            IngredientBlueberries = cfg.Bind("Recipe", "Blueberries", 10, "Blueberries required to craft the Bifröst Totem.");
            CraftingStationLevel = cfg.Bind("Recipe", "WorkbenchLevel", 2, "Workbench level required to craft the Bifröst Totem.");

            ShowRangeCircle = cfg.Bind("UI", "ShowRangeCircle", true, "Show the light-blue range circle on the large map.");
            RangeCircleColor = cfg.Bind("UI", "RangeCircleColor", new Color(0.35f, 0.75f, 1.0f, 0.85f), "Color of the range circle on the large map.");

            XpCommon = cfg.Bind("XP", "Common", 1f, "Bifröst XP awarded for sacrificing a common trophy.");
            XpUncommon = cfg.Bind("XP", "Uncommon", 3f, "Bifröst XP awarded for sacrificing an uncommon trophy.");
            XpRare = cfg.Bind("XP", "Rare", 8f, "Bifröst XP awarded for sacrificing a rare trophy.");
            XpBoss = cfg.Bind("XP", "Boss", 25f, "Bifröst XP awarded for sacrificing a boss trophy.");

            TrophyTierUncommon = cfg.Bind(
                "XP.Tiers", "UncommonTrophies",
                "TrophyGreydwarf,TrophySkeleton,TrophySkeletonPoison,TrophyGhost,TrophyLeech,TrophyDeer,TrophyHare,TrophyChicken",
                "Comma-separated trophy prefab names treated as Uncommon.");
            TrophyTierRare = cfg.Bind(
                "XP.Tiers", "RareTrophies",
                "TrophyFrostTroll,TrophyTroll,TrophyDraugr,TrophyDraugrElite,TrophyWraith,TrophyFuling,TrophyLox,TrophySerpent,TrophyAbomination,TrophyGoblin,TrophyGoblinBrute,TrophyGoblinShaman,TrophyCultist,TrophyUlv,TrophyHatchling,TrophySeeker,TrophyTick,TrophyAsksvin,TrophyCharred",
                "Comma-separated trophy prefab names treated as Rare.");
            TrophyTierBoss = cfg.Bind(
                "XP.Tiers", "BossTrophies",
                "TrophyEikthyr,TrophyTheElder,TrophyBonemass,TrophyDragonQueen,TrophyGoblinKing,TrophySeekerQueen,TrophyFader",
                "Comma-separated trophy prefab names treated as Boss-tier.");
        }

        public static HashSet<string> ParseSet(ConfigEntry<string> entry)
        {
            var result = new HashSet<string>();
            if (entry == null || string.IsNullOrEmpty(entry.Value)) return result;
            foreach (var raw in entry.Value.Split(','))
            {
                var name = raw.Trim();
                if (name.Length > 0) result.Add(name);
            }
            return result;
        }
    }
}
