using System.Collections.Generic;
using HarmonyLib;
using SpawnPointGateways.Config;
using SpawnPointGateways.Items;
using SpawnPointGateways.SpawnPoints;
using SpawnPointGateways.State;
using UnityEngine;
using UnityEngine.UI;

namespace SpawnPointGateways.UI
{
    /// <summary>
    /// Draws a small fixed-size circle on the large map for every spawn point the
    /// local player has ever recorded. The markers remain a constant on-screen
    /// size regardless of map zoom — they don't scale with the world.
    /// </summary>
    internal static class SpawnMarkerOverlay
    {
        private const int RingTextureSize = 128;

        private static Transform _root;
        private static readonly List<RectTransform> _pool = new List<RectTransform>();
        private static readonly List<Image> _poolImages = new List<Image>();
        private static Sprite _ringSprite;
        private static float _ringSpriteThickness = -1f;
        private static System.Reflection.MethodInfo _worldToMapPointMethod;

        public static IReadOnlyList<RectTransform> ActiveMarkers => _pool;

        public static bool TryGetMarkerWorldPos(int index, out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            if (index < 0 || index >= _markerWorldPositions.Count) return false;
            worldPos = _markerWorldPositions[index];
            return true;
        }

        private static readonly List<Vector3> _markerWorldPositions = new List<Vector3>();

        public static void Tick()
        {
            var minimap = Minimap.instance;
            var player = Player.m_localPlayer;
            if (minimap == null || player == null)
            {
                Hide();
                return;
            }

            bool largeMapOpen = minimap.m_largeRoot != null && minimap.m_largeRoot.activeSelf;
            if (!largeMapOpen || GatewayState.Phase != ArmingPhase.AwaitingDestination)
            {
                Hide();
                return;
            }

            if (!BifrostCharm.PlayerHasCharm(player))
            {
                Hide();
                return;
            }

            if (!EnsureRoot(minimap)) return;
            EnsureRingSprite();
            UpdateMarkers(minimap, player);
        }

        public static void Hide()
        {
            if (_root != null && _root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }

        private static bool EnsureRoot(Minimap minimap)
        {
            if (minimap.m_mapImageLarge == null) return false;
            var parent = (RectTransform)minimap.m_mapImageLarge.transform;

            if (_root == null || _root.parent != parent)
            {
                if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);

                var go = new GameObject("SpawnMarkersRoot", typeof(RectTransform));
                _root = go.transform;
                var rt = (RectTransform)_root;
                rt.SetParent(parent, false);
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _pool.Clear();
                _poolImages.Clear();
            }

            _root.gameObject.SetActive(true);
            // Force markers to the top of the z-order each frame so vanilla map UI
            // (player marker, pins, the cart/cargo icons) can't render over them.
            _root.SetAsLastSibling();
            return true;
        }

        private static void EnsureRingSprite()
        {
            float thickness = Mathf.Clamp(GatewayConfig.MarkerRingThickness.Value, 0.01f, 0.5f);
            if (_ringSprite != null && Mathf.Approximately(_ringSpriteThickness, thickness)) return;
            _ringSprite = CircleSpriteBuilder.CreateRingSprite(RingTextureSize, 1f - thickness, 1f);
            _ringSpriteThickness = thickness;

            for (int i = 0; i < _poolImages.Count; i++)
            {
                if (_poolImages[i] != null) _poolImages[i].sprite = _ringSprite;
            }
        }

        private static void UpdateMarkers(Minimap minimap, Player player)
        {
            var points = SpawnPointRegistry.GetAll(player);
            _markerWorldPositions.Clear();

            var parentRt = (RectTransform)minimap.m_mapImageLarge.transform;
            var uv = minimap.m_mapImageLarge.uvRect;
            if (uv.width <= 0f || uv.height <= 0f)
            {
                HideAllMarkers();
                return;
            }
            Vector2 displaySize = parentRt.rect.size;
            int radius = Mathf.Max(4, GatewayConfig.MarkerRadiusPixels.Value);
            int diameter = radius * 2;
            var color = GatewayConfig.MarkerColor.Value;

            int activeCount = 0;
            for (int i = 0; i < points.Count; i++)
            {
                var world = points[i];
                if (!InvokeWorldToMapPoint(minimap, world, out float pmx, out float pmy)) continue;

                float visX = (pmx - (uv.x + uv.width * 0.5f)) / uv.width;
                float visY = (pmy - (uv.y + uv.height * 0.5f)) / uv.height;
                Vector2 anchored = new Vector2(visX * displaySize.x, visY * displaySize.y);

                RectTransform rt = GetOrCreateMarker(activeCount);
                rt.sizeDelta = new Vector2(diameter, diameter);
                rt.anchoredPosition = anchored;
                rt.gameObject.SetActive(true);

                var img = _poolImages[activeCount];
                if (img != null)
                {
                    img.sprite = _ringSprite;
                    img.color = color;
                }
                _markerWorldPositions.Add(world);
                activeCount++;
            }

            for (int i = activeCount; i < _pool.Count; i++)
            {
                if (_pool[i] != null) _pool[i].gameObject.SetActive(false);
            }
        }

        private static void HideAllMarkers()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null) _pool[i].gameObject.SetActive(false);
            }
            _markerWorldPositions.Clear();
        }

        private static RectTransform GetOrCreateMarker(int index)
        {
            while (_pool.Count <= index)
            {
                var go = new GameObject($"SpawnMarker_{_pool.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(_root, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                var img = go.GetComponent<Image>();
                img.sprite = _ringSprite;
                img.raycastTarget = false;
                img.preserveAspect = true;
                img.type = Image.Type.Simple;
                _pool.Add(rt);
                _poolImages.Add(img);
            }
            return _pool[index];
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
                SpawnPointGatewaysPlugin.Log?.LogWarning($"WorldToMapPoint reflection failed: {ex.Message}");
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
