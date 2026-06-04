using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace ExtendedStorage.UI
{
    internal static class LocalizationLoader
    {
        public static void Register()
        {
            var plugin = ExtendedStoragePlugin.Instance;
            if (plugin == null) return;

            var loc = new CustomLocalization(plugin.Info.Metadata);
            LocalizationManager.Instance.AddLocalization(loc);

            loc.AddTranslation("English", new Dictionary<string, string>
            {
                { "piece_woodencabinet", "Wooden Cabinet" },
                { "piece_woodencabinet_description", "A wooden cabinet with six labelled drawers." }
            });
        }
    }
}
