using UnityEngine;

namespace PersonalGateway.Items
{
    internal static class StarIconBuilder
    {
        public static Sprite CreateStarSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            tex.SetPixels(pixels);

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

            var bodyColor = new Color(1.0f, 0.85f, 0.15f, 1f);
            var edgeColor = new Color(0.45f, 0.32f, 0.05f, 1f);
            var glowColor = new Color(1.0f, 0.92f, 0.55f, 1f);

            float glowRadius = outer * 1.05f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (PointInPolygon(p, poly))
                    {
                        float distCenter = Vector2.Distance(p, center) / outer;
                        Color c = Color.Lerp(glowColor, bodyColor, Mathf.Clamp01(distCenter));
                        float edge = EdgeDistance(p, poly);
                        if (edge < 1.5f)
                        {
                            c = Color.Lerp(edgeColor, c, Mathf.Clamp01(edge / 1.5f));
                        }
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        float d = Vector2.Distance(p, center);
                        if (d < glowRadius)
                        {
                            float t = 1f - Mathf.Clamp01((d - outer * 0.85f) / (glowRadius - outer * 0.85f));
                            if (t > 0f)
                            {
                                tex.SetPixel(x, y, new Color(1.0f, 0.85f, 0.4f, t * 0.20f));
                            }
                        }
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
                float d = DistancePointSegment(p, poly[j], poly[i]);
                if (d < min) min = d;
            }
            return min;
        }

        private static float DistancePointSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 1e-6f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
