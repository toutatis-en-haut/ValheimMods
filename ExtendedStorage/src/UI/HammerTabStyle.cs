using UnityEngine;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal static class HammerTabStyle
    {
        public static Sprite PanelBackground;
        public static Font LabelFont;
        public static int FontSize = 16;
        public static Color LabelColor = new Color(0.96f, 0.92f, 0.82f, 1f); // warm cream
        public static Color CountColor = new Color(1f, 0.83f, 0.18f, 1f);    // yellow [N]

        // Body colour for inactive tabs and Q/E buttons. Default is a flat
        // warm brown; Resolve() will replace it with a darkened version of
        // the chest panel's runtime tint if a theming mod is in play.
        public static Color TabBodyColor = new Color(0.18f, 0.12f, 0.07f, 0.92f);

        // Vibrant blue used for the active tab's highlight overlay. Flat
        // colour (no sprite) so it can't fall victim to whatever near-white
        // sprite Hud's tab template happens to expose.
        public static Color ActiveHighlightColor = new Color(0.28f, 0.62f, 0.92f, 1f);

        private static bool s_resolved;

        public static void Resolve()
        {
            if (s_resolved) return;

            var hud = Hud.instance;
            if (hud != null && hud.m_pieceSelectionWindow != null)
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

            // Borrow font + size from a piece category tab's label.
            if (hud != null && hud.m_pieceCategoryTabs != null && hud.m_pieceCategoryTabs.Length > 0)
            {
                var tab = hud.m_pieceCategoryTabs[0];
                if (tab != null)
                {
                    var label = tab.GetComponentInChildren<Text>(includeInactive: true);
                    if (label != null)
                    {
                        LabelFont = label.font;
                        FontSize = label.fontSize > 0 ? label.fontSize : FontSize;
                    }
                }
            }

            // Theme-aware tab body: if the chest panel is currently tinted by
            // a UI mod, derive our body colour from that tint so we blend in.
            // If the panel reads as near-white (i.e. vanilla, no mod tinting),
            // keep the flat-brown default.
            var inv = InventoryGui.instance;
            if (inv != null && inv.m_container != null)
            {
                var panelImg = FindFirstTintedImage(inv.m_container);
                if (panelImg != null)
                {
                    var c = panelImg.color;
                    if (!IsNearWhite(c))
                    {
                        TabBodyColor = new Color(
                            c.r * 0.65f,
                            c.g * 0.65f,
                            c.b * 0.65f,
                            Mathf.Clamp01(c.a + 0.10f));
                    }
                }
            }

            s_resolved = true;
        }

        // Walks up the hierarchy from `start` and returns the first Image
        // that has a sprite. Used to find the chest panel's background so we
        // can derive a theme-matching tab body colour.
        private static Image FindFirstTintedImage(Transform start)
        {
            var t = start;
            while (t != null)
            {
                var img = t.GetComponent<Image>();
                if (img != null && img.sprite != null) return img;
                t = t.parent;
            }
            return null;
        }

        private static bool IsNearWhite(Color c)
        {
            return c.r > 0.92f && c.g > 0.92f && c.b > 0.92f;
        }
    }
}
