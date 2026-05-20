using PersonalGateway.State;
using UnityEngine;

namespace PersonalGateway.UI
{
    internal static class TeleportCursor
    {
        private static Texture2D _cursorTex;
        private static bool _active;

        public static void Tick()
        {
            bool shouldShow = GatewayState.Phase == ArmingPhase.AwaitingDestination
                              && Minimap.instance != null
                              && Minimap.instance.m_largeRoot != null
                              && Minimap.instance.m_largeRoot.activeSelf;

            if (shouldShow && !_active)
            {
                EnsureCursor();
                Cursor.SetCursor(_cursorTex, new Vector2(16, 16), CursorMode.Auto);
                _active = true;
            }
            else if (!shouldShow && _active)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                _active = false;
            }
        }

        public static void ForceReset()
        {
            if (!_active) return;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            _active = false;
        }

        private static void EnsureCursor()
        {
            if (_cursorTex != null) return;
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outer = size * 0.46f;
            float inner = outer * 0.42f;
            var poly = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float r = (i % 2 == 0) ? outer : inner;
                float angle = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                poly[i] = center + new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
            }

            var fill = new Color(1.0f, 0.86f, 0.18f, 1f);
            var rim = new Color(0.45f, 0.30f, 0.0f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (PointInPolygon(p, poly))
                    {
                        float edge = EdgeDistance(p, poly);
                        Color c = edge < 1.2f ? Color.Lerp(rim, fill, Mathf.Clamp01(edge / 1.2f)) : fill;
                        pixels[y * size + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _cursorTex = tex;
        }

        private static bool PointInPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static float EdgeDistance(Vector2 p, Vector2[] poly)
        {
            float min = float.MaxValue;
            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 a = poly[j];
                Vector2 b = poly[i];
                Vector2 ab = b - a;
                float lenSq = ab.sqrMagnitude;
                if (lenSq < 1e-6f) continue;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
                float dist = Vector2.Distance(p, a + ab * t);
                if (dist < min) min = dist;
            }
            return min;
        }
    }
}
