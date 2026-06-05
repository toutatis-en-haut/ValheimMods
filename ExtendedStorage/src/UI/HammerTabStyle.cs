using UnityEngine;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal static class HammerTabStyle
    {
        public static Sprite PanelBackground;
        public static Sprite TabBackground;     // brown body sprite for inactive tabs
        public static Sprite SelectedHighlight; // blue cyan sprite for active tab
        public static Font LabelFont;
        public static int FontSize = 16;
        public static Color LabelColor = new Color(0.95f, 0.95f, 0.95f, 1f); // bold white-ish
        public static Color CountColor = new Color(1f, 0.83f, 0.18f, 1f);    // yellow [N]

        private static bool s_resolved;

        public static void Resolve()
        {
            if (s_resolved) return;

            var hud = Hud.instance;
            if (hud == null) return;

            // The wood-grain panel: lives behind the build-menu category bar.
            // We sample any Image sprite under the piece selection window
            // that looks like a stretched panel; fallback to the first.
            if (hud.m_pieceSelectionWindow != null)
            {
                var images = hud.m_pieceSelectionWindow.GetComponentsInChildren<Image>(includeInactive: true);
                foreach (var img in images)
                {
                    if (img != null && img.sprite != null && img.type == Image.Type.Sliced)
                    {
                        PanelBackground = img.sprite;
                        break;
                    }
                }
                if (PanelBackground == null && images.Length > 0 && images[0].sprite != null)
                {
                    PanelBackground = images[0].sprite;
                }
            }

            // Per-tab visuals — grab the first category tab as our template.
            if (hud.m_pieceCategoryTabs != null && hud.m_pieceCategoryTabs.Length > 0)
            {
                var tab = hud.m_pieceCategoryTabs[0];
                if (tab != null)
                {
                    var label = tab.GetComponentInChildren<Text>(includeInactive: true);
                    if (label != null)
                    {
                        LabelFont = label.font;
                        FontSize = label.fontSize > 0 ? label.fontSize : FontSize;
                        // Keep our white-ish default; vanilla colour can vary.
                    }

                    // The tab root has the brown body Image. The "Selected"
                    // child Image is the blue highlight, toggled on activation.
                    var rootImg = tab.GetComponent<Image>();
                    if (rootImg != null && rootImg.sprite != null)
                    {
                        TabBackground = rootImg.sprite;
                    }

                    var images = tab.GetComponentsInChildren<Image>(includeInactive: true);
                    foreach (var img in images)
                    {
                        if (img == null || img.sprite == null) continue;
                        if (img == rootImg) continue; // skip the root body sprite
                        var n = img.gameObject.name?.ToLowerInvariant();
                        if (n != null && (n.Contains("select") || n.Contains("active") || n.Contains("highlight")))
                        {
                            SelectedHighlight = img.sprite;
                            break;
                        }
                    }
                    if (SelectedHighlight == null)
                    {
                        foreach (var img in images)
                        {
                            if (img == rootImg) continue;
                            if (img.sprite != null && img.type == Image.Type.Sliced)
                            {
                                SelectedHighlight = img.sprite;
                                break;
                            }
                        }
                    }
                }
            }

            s_resolved = true;
        }
    }
}
