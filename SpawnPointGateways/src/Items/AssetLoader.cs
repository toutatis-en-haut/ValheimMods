using System.IO;
using System.Reflection;
using UnityEngine;

namespace SpawnPointGateways.Items
{
    internal static class AssetLoader
    {
        public static Sprite TryLoadSprite(string relativePath, bool keyWhiteToAlpha = false)
        {
            try
            {
                string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(baseDir)) return null;
                string fullPath = Path.Combine(baseDir, relativePath);
                if (!File.Exists(fullPath))
                {
                    SpawnPointGatewaysPlugin.Log?.LogInfo($"Asset not found: {fullPath} (falling back to procedural)");
                    return null;
                }

                var tex = Jotunn.Utils.AssetUtils.LoadTexture(fullPath, relativePath: false);
                if (tex == null)
                {
                    SpawnPointGatewaysPlugin.Log?.LogWarning($"Failed to decode image: {fullPath}");
                    return null;
                }
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;

                if (keyWhiteToAlpha) KeyWhiteToTransparent(tex);

                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            catch (System.Exception ex)
            {
                SpawnPointGatewaysPlugin.Log?.LogWarning($"AssetLoader.TryLoadSprite('{relativePath}') failed: {ex.Message}");
                return null;
            }
        }

        private static void KeyWhiteToTransparent(Texture2D tex)
        {
            const float fullAlphaCutoff = 0.94f;
            const float fadeStart = 0.78f;
            const float maxChannelSpread = 0.06f;

            var pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                float r = p.r / 255f;
                float g = p.g / 255f;
                float b = p.b / 255f;
                float min = Mathf.Min(r, Mathf.Min(g, b));
                float max = Mathf.Max(r, Mathf.Max(g, b));
                float spread = max - min;
                float brightness = (r + g + b) / 3f;

                if (spread > maxChannelSpread) continue;

                if (brightness >= fullAlphaCutoff)
                {
                    pixels[i] = new Color32(p.r, p.g, p.b, 0);
                }
                else if (brightness >= fadeStart)
                {
                    float t = (brightness - fadeStart) / (fullAlphaCutoff - fadeStart);
                    byte alpha = (byte)Mathf.RoundToInt((1f - t) * 255f);
                    pixels[i] = new Color32(p.r, p.g, p.b, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }
    }
}
