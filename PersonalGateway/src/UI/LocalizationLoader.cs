using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace PersonalGateway.UI
{
    internal static class LocalizationLoader
    {
        public static void Register()
        {
            var loc = new CustomLocalization(new LocalizationConfig("English"));

            loc.AddTranslation("bifrost_totem_name", "Bifröst Totem");
            loc.AddTranslation("bifrost_totem_desc",
                "A relic of Heimdall's bridge. Awakened by the blood of fallen beasts, it parts the sky for those who carry it.");

            loc.AddTranslation("bifrost_skill_name", "Bifröst");
            loc.AddTranslation("bifrost_skill_desc",
                "Mastery over Heimdall's bridge. Higher mastery lets you cross greater distances in a single step.");

            loc.AddTranslation("bifrost_msg_armed", "The totem hums. Choose a trophy to sacrifice.");
            loc.AddTranslation("bifrost_msg_no_trophies", "You hold no trophy to sacrifice.");
            loc.AddTranslation("bifrost_msg_channel", "The Bifröst gathers. Open your map and Ctrl+double-click your destination.");
            loc.AddTranslation("bifrost_msg_cancelled", "The Bifröst fades.");
            loc.AddTranslation("bifrost_msg_too_far", "Beyond the bridge's reach.");
            loc.AddTranslation("bifrost_msg_unknown", "The stars cannot find a place you have not seen.");
            loc.AddTranslation("bifrost_msg_teleported", "The Bifröst carries you. {0} consumed.");
            loc.AddTranslation("bifrost_msg_trophy_lost", "The chosen sacrifice is gone. The Bifröst dims.");

            LocalizationManager.Instance.AddLocalization(loc);
        }
    }
}
