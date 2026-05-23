using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace PersonalGateway.UI
{
    internal static class LocalizationLoader
    {
        public static void Register()
        {
            var plugin = PersonalGatewayPlugin.Instance;
            if (plugin == null) return;

            var loc = new CustomLocalization(plugin.Info.Metadata);
            LocalizationManager.Instance.AddLocalization(loc);

            loc.AddTranslation("English", new Dictionary<string, string>
            {
                { "bifrost_totem_name", "Bifröst Totem" },
                { "bifrost_totem_desc", "A relic of Heimdall's bridge. Awakened by the blood of fallen beasts, it parts the sky for those who carry it." },
                { "bifrost_skill_name", "Bifröst" },
                { "bifrost_skill_desc", "Mastery over Heimdall's bridge. Higher mastery lets you cross greater distances in a single step." },
                { "bifrost_msg_armed", "The totem hums. Choose a trophy to sacrifice." },
                { "bifrost_msg_no_trophies", "You hold no trophy to sacrifice." },
                { "bifrost_msg_channel", "The Bifröst gathers. Open your map and click your destination." },
                { "bifrost_msg_cancelled", "The Bifröst fades." },
                { "bifrost_msg_too_far", "Beyond the bridge's reach." },
                { "bifrost_msg_unknown", "The stars cannot find a place you have not seen." },
                { "bifrost_msg_teleported", "The Bifröst carries you. {0} consumed." },
                { "bifrost_msg_trophy_lost", "The chosen sacrifice is gone. The Bifröst dims." },
                { "bifrost_toggle_label", "Bifröst Totem Range" },
                { "bifrost_toggle_on", "Bifröst Totem Range: ON" },
                { "bifrost_toggle_off", "Bifröst Totem Range: OFF" }
            });
        }
    }
}
