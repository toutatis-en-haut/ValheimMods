using PersonalGateway.Config;
using PersonalGateway.Items;
using PersonalGateway.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace PersonalGateway.UI
{
    internal static class MapRangeOverlay
    {
        private static GameObject _go;
        private static RectTransform _rt;
        private static Image _img;
        private static Sprite _ringSprite;

        public static void Tick()
        {
            var minimap = Minimap.instance;
            var player = Player.m_localPlayer;
            if (minimap == null || player == null)
            {
                Disable();
                return;
            }

            bool largeMapOpen = minimap.m_largeRoot != null && minimap.m_largeRoot.activeSelf;
            if (!largeMapOpen
                || !GatewayConfig.ShowRangeCircle.Value
                || !BifrostTotem.PlayerHasTotem(player)
                || BifrostSkill.IsMaxed(player))
            {
                Disable();
                return;
            }

            if (!EnsureOverlay(minimap)) return;
            UpdateOverlay(minimap, player);
        }

        private static void Disable()
        {
            if (_go != null && _go.activeSelf) _go.SetActive(false);
        }

        private static bool EnsureOverlay(Minimap minimap)
        {
            if (_go != null) return true;
            if (minimap.m_mapImageLarge == null) return false;

            var parent = (RectTransform)minimap.m_mapImageLarge.transform;
            _ringSprite = CircleSpriteBuilder.CreateRingSprite(256, 0.93f, 1.0f);

            _go = new GameObject("BifrostRangeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _rt = (RectTransform)_go.transform;
            _rt.SetParent(parent, false);
            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _img = _go.GetComponent<Image>();
            _img.sprite = _ringSprite;
            _img.raycastTarget = false;
            _img.preserveAspect = true;
            return true;
        }

        private static void UpdateOverlay(Minimap minimap, Player player)
        {
            if (minimap.m_mapImageLarge == null) return;

            if (!minimap.WorldToMapPoint(player.transform.position, out float mx, out float my))
            {
                _go.SetActive(false);
                return;
            }

            var uv = minimap.m_mapImageLarge.uvRect;
            if (uv.width <= 0f || uv.height <= 0f)
            {
                _go.SetActive(false);
                return;
            }

            var parentRt = (RectTransform)minimap.m_mapImageLarge.transform;
            Vector2 displaySize = parentRt.rect.size;

            float visX = (mx - (uv.x + uv.width * 0.5f)) / uv.width;
            float visY = (my - (uv.y + uv.height * 0.5f)) / uv.height;
            _rt.anchoredPosition = new Vector2(visX * displaySize.x, visY * displaySize.y);

            float worldExtent = minimap.m_pixelSize * minimap.m_textureSize * uv.width;
            float pixelsPerMeter = worldExtent > 0f ? displaySize.x / worldExtent : 0f;
            float rangeMeters = BifrostSkill.GetRangeMeters(player);
            float diameterPixels = rangeMeters * 2f * pixelsPerMeter;

            _rt.sizeDelta = new Vector2(diameterPixels, diameterPixels);
            _img.color = GatewayConfig.RangeCircleColor.Value;
            _go.SetActive(true);
        }
    }

    internal static class CircleSpriteBuilder
    {
        public static Sprite CreateRingSprite(int size, float innerRatio, float outerRatio)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float outer = size * 0.5f * outerRatio;
            float inner = size * 0.5f * innerRatio;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 0f;
                    if (d <= outer && d >= inner)
                    {
                        float edgeFade = Mathf.Min(d - inner, outer - d);
                        alpha = Mathf.Clamp01(edgeFade);
                    }
                    if (alpha > 0f)
                    {
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
