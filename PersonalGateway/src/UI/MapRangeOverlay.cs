using HarmonyLib;
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
        private static float _ringSpriteThickness = -1f;
        private static System.Reflection.MethodInfo _worldToMapPointMethod;

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
            if (minimap.m_mapImageLarge == null) return false;
            var parent = (RectTransform)minimap.m_mapImageLarge.transform;

            float thickness = Mathf.Clamp(GatewayConfig.RangeCircleThickness.Value, 0.01f, 0.5f);
            bool spriteOutOfDate = _ringSprite == null || !Mathf.Approximately(_ringSpriteThickness, thickness);
            if (spriteOutOfDate)
            {
                _ringSprite = CircleSpriteBuilder.CreateRingSprite(256, 1f - thickness, 1f);
                _ringSpriteThickness = thickness;
            }

            if (_go == null || _rt == null || _rt.transform.parent != parent)
            {
                if (_go != null) UnityEngine.Object.Destroy(_go);

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
                _img.type = Image.Type.Simple;
            }
            else if (spriteOutOfDate && _img != null)
            {
                _img.sprite = _ringSprite;
            }
            return true;
        }

        private static void UpdateOverlay(Minimap minimap, Player player)
        {
            if (minimap.m_mapImageLarge == null)
            {
                Disable();
                return;
            }

            if (!InvokeWorldToMapPoint(minimap, player.transform.position, out float pmx, out float pmy))
            {
                Disable();
                return;
            }

            var uv = minimap.m_mapImageLarge.uvRect;
            if (uv.width <= 0f || uv.height <= 0f)
            {
                Disable();
                return;
            }

            var parentRt = (RectTransform)minimap.m_mapImageLarge.transform;
            Vector2 displaySize = parentRt.rect.size;

            float visX = (pmx - (uv.x + uv.width * 0.5f)) / uv.width;
            float visY = (pmy - (uv.y + uv.height * 0.5f)) / uv.height;
            Vector2 playerAnchored = new Vector2(visX * displaySize.x, visY * displaySize.y);
            _rt.anchoredPosition = playerAnchored;

            float rangeMeters = BifrostSkill.GetRangeMeters(player);
            float diameterPixels = MeasureRangeInPixels(minimap, player.transform.position, rangeMeters, uv, displaySize);
            if (diameterPixels <= 0f)
            {
                Disable();
                return;
            }

            _rt.sizeDelta = new Vector2(diameterPixels, diameterPixels);
            _img.color = GatewayConfig.RangeCircleColor.Value;
            _go.SetActive(true);
        }

        private static float MeasureRangeInPixels(Minimap minimap, Vector3 playerWorld, float rangeMeters, Rect uv, Vector2 displaySize)
        {
            if (!InvokeWorldToMapPoint(minimap, playerWorld, out float pmx, out float pmy)) return 0f;
            if (!InvokeWorldToMapPoint(minimap, playerWorld + new Vector3(rangeMeters, 0f, 0f), out float emx, out float emy)) return 0f;

            float dx = (emx - pmx) / uv.width * displaySize.x;
            float dy = (emy - pmy) / uv.height * displaySize.y;
            float radiusPixels = Mathf.Sqrt(dx * dx + dy * dy);
            return radiusPixels * 2f;
        }

        private static bool InvokeWorldToMapPoint(Minimap minimap, Vector3 worldPos, out float mx, out float my)
        {
            mx = my = 0f;
            if (minimap == null) return false;
            if (_worldToMapPointMethod == null)
            {
                _worldToMapPointMethod = AccessTools.Method(typeof(Minimap), "WorldToMapPoint");
                if (_worldToMapPointMethod == null) return false;
            }
            try
            {
                var args = new object[] { worldPos, 0f, 0f };
                _worldToMapPointMethod.Invoke(minimap, args);
                if (args[1] is float fmx) mx = fmx;
                if (args[2] is float fmy) my = fmy;
                return mx >= 0f && my >= 0f && mx <= 1f && my <= 1f;
            }
            catch (System.Exception ex)
            {
                PersonalGatewayPlugin.Log?.LogWarning($"WorldToMapPoint reflection failed: {ex.Message}");
                return false;
            }
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
                    if (d <= outer && d >= inner)
                    {
                        float edgeFade = Mathf.Min(d - inner, outer - d);
                        float alpha = Mathf.Clamp01(edgeFade);
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
