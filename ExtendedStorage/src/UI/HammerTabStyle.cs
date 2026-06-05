using UnityEngine;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal static class HammerTabStyle
    {
        public static Sprite PanelBackground;
        public static Sprite SelectedHighlight;
        public static Font LabelFont;
        public static int FontSize = 16;
        public static Color LabelColor = new Color(0.95f, 0.88f, 0.65f, 1f);
        public static Color CountColor = new Color(1f, 0.83f, 0.18f, 1f); // yellow [N/15]

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
                        LabelColor = label.color;
                    }

                    // The Selected highlight is typically a child image whose
                    // GameObject is named "Selected" (or similar). Match by
                    // any image child that is currently inactive in this
                    // template — that's the highlight shown only on selection.
                    var images = tab.GetComponentsInChildren<Image>(includeInactive: true);
                    foreach (var img in images)
                    {
                        if (img == null || img.sprite == null) continue;
                        var n = img.gameObject.name?.ToLowerInvariant();
                        if (n != null && (n.Contains("select") || n.Contains("active") || n.Contains("highlight")))
                        {
                            SelectedHighlight = img.sprite;
                            break;
                        }
                    }
                    if (SelectedHighlight == null)
                    {
                        // Fallback: any non-default sliced image in the tab.
                        foreach (var img in images)
                        {
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
