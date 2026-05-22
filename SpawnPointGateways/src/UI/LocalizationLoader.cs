using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace SpawnPointGateways.UI
{
    internal static class LocalizationLoader
    {
        public static void Register()
        {
            var plugin = SpawnPointGatewaysPlugin.Instance;
            if (plugin == null) return;

            var loc = new CustomLocalization(plugin.Info.Metadata);
            LocalizationManager.Instance.AddLocalization(loc);

            loc.AddTranslation("English", new Dictionary<string, string>
            {
                { "bifrost_charm_name", "Bifröst Charm" },
                { "bifrost_charm_desc", "A trinket whispered to by Heimdall. Burn resin to call upon the bridge — and step home to any bed you have ever called your own." },
                { "bifrost_charm_msg_no_resin", "You need {0} resin to light the charm." },
                { "bifrost_charm_msg_no_cost", "You need {0} {1} to light the charm." },
                { "bifrost_charm_msg_channel", "The Bifröst gathers. Click a remembered home." },
                { "bifrost_charm_msg_cancelled", "The Bifröst fades." },
                { "bifrost_charm_msg_teleported", "The Bifröst carries you home." }
            });
        }
    }
}
