using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PersonalGateway.Config;
using UnityEngine;
using UnityEngine.UI;

namespace PersonalGateway.UI
{
    /// <summary>
    /// "Bifröst Totem Range" map toggle. Built from scratch so we don't inherit
    /// any Valheim behaviour from a cloned button:
    ///   - Procedural rounded-rect background (semi-opaque)
    ///   - Procedural radio-button icon (empty when off, filled when on)
    ///   - Cloned TMP text element from any vanilla toggle for font/style parity
    /// Position is anchored above the Cartography Table toggle (or, if absent,
    /// the public-position toggle) with a config-tunable vertical offset.
    /// </summary>
    internal static class MapToggleButton
    {
        private static GameObject _go;
        private static Image _backgroundImage;
        private static Image _radioIcon;
        private static Sprite _radioOnSprite;
        private static Sprite _radioOffSprite;
        private static readonly List<Component> _labelTargets = new List<Component>();
        private static readonly List<PropertyInfo> _labelProperties = new List<PropertyInfo>();
        private static GameObject _cachedAnchor;
        private static bool _anchorMissingLogged;

        public static void Tick()
        {
            var minimap = Minimap.instance;
            if (minimap == null)
            {
                Disable();
                return;
            }

            bool largeMapOpen = minimap.m_largeRoot != null && minimap.m_largeRoot.activeSelf;
            if (!largeMapOpen)
            {
                Disable();
                return;
            }

            if (!EnsureCreated(minimap)) return;
            _go.SetActive(true);
            ApplyCurrentLabel();
            ApplyRadioState();
            UpdatePosition(minimap);
        }

        private static void Disable()
        {
            if (_go != null && _go.activeSelf) _go.SetActive(false);
        }

        private static bool EnsureCreated(Minimap minimap)
        {
            if (_go != null && _go.transform.parent != null) return true;

            GameObject anchor = FindCartographyAnchor(minimap);
            if (anchor == null)
            {
                if (!_anchorMissingLogged)
                {
                    PersonalGatewayPlugin.Log?.LogInfo("MapToggleButton: no anchor toggle found; range toggle disabled.");
                    _anchorMissingLogged = true;
                }
                return false;
            }

            BuildFromScratch(anchor);
            return _go != null;
        }

        private static void BuildFromScratch(GameObject anchor)
        {
            var anchorRt = (RectTransform)anchor.transform;
            var parent = anchorRt.parent;
            if (parent == null) return;

            var go = new GameObject(
                "BifrostRangeToggle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));

            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            go.GetComponent<LayoutElement>().ignoreLayout = true;

            rt.anchorMin = anchorRt.anchorMin;
            rt.anchorMax = anchorRt.anchorMax;
            rt.pivot = anchorRt.pivot;
            rt.sizeDelta = anchorRt.sizeDelta;
            rt.localScale = anchorRt.localScale;

            _backgroundImage = go.GetComponent<Image>();
            _backgroundImage.sprite = CreateRoundedRectSprite(64, 12);
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = new Color(0.10f, 0.08f, 0.05f, 0.85f);
            _backgroundImage.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = _backgroundImage;
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.10f, 1.0f, 1f);
            colors.pressedColor = new Color(0.8f, 0.75f, 0.65f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;
            btn.onClick.AddListener(OnClicked);

            BuildLabel(go, anchor);
            BuildRadioIcon(go);

            _go = go;
        }

        private static void BuildLabel(GameObject parent, GameObject anchor)
        {
            GameObject textTemplate = FindAnchorTextGameObject(anchor);

            GameObject labelGo;
            if (textTemplate != null)
            {
                labelGo = UnityEngine.Object.Instantiate(textTemplate, parent.transform);
                labelGo.name = "Label";

                foreach (var c in labelGo.GetComponentsInChildren<Component>(includeInactive: true))
                {
                    if (c == null) continue;
                    if (c.GetType().Name == "Localize")
                    {
                        UnityEngine.Object.DestroyImmediate(c);
                    }
                }
            }
            else
            {
                labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                labelGo.transform.SetParent(parent.transform, false);
                var t = labelGo.GetComponent<Text>();
                t.color = new Color(1f, 0.85f, 0.4f, 1f);
                t.alignment = TextAnchor.MiddleRight;
                t.fontSize = 22;
            }

            // Stretch across the toggle, leaving room on the right for the radio icon.
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(14f, 0f);
            labelRt.offsetMax = new Vector2(-50f, 0f);
            labelRt.localScale = Vector3.one;

            foreach (var g in labelGo.GetComponentsInChildren<Graphic>(includeInactive: true))
            {
                g.raycastTarget = false;
            }

            CaptureLabelTargets(labelGo);
        }

        private static void BuildRadioIcon(GameObject parent)
        {
            var iconGo = new GameObject(
                "RadioIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            iconGo.transform.SetParent(parent.transform, false);

            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(1f, 0.5f);
            iconRt.anchorMax = new Vector2(1f, 0.5f);
            iconRt.pivot = new Vector2(1f, 0.5f);
            iconRt.sizeDelta = new Vector2(28f, 28f);
            iconRt.anchoredPosition = new Vector2(-12f, 0f);
            iconRt.localScale = Vector3.one;

            _radioIcon = iconGo.GetComponent<Image>();
            _radioIcon.raycastTarget = false;
            _radioOffSprite = CreateRadioSprite(64, filled: false);
            _radioOnSprite = CreateRadioSprite(64, filled: true);
            ApplyRadioState();
        }

        private static void UpdatePosition(Minimap minimap)
        {
            if (_go == null) return;
            var anchor = FindCartographyAnchor(minimap);
            if (anchor == null) return;
            var anchorRt = (RectTransform)anchor.transform;
            var rt = (RectTransform)_go.transform;

            float vOffset = GatewayConfig.RangeToggleVerticalOffset.Value;
            rt.anchorMin = anchorRt.anchorMin;
            rt.anchorMax = anchorRt.anchorMax;
            rt.pivot = anchorRt.pivot;
            rt.sizeDelta = anchorRt.sizeDelta;
            rt.localScale = anchorRt.localScale;
            float anchorHeight = anchorRt.rect.size.y;
            rt.anchoredPosition = anchorRt.anchoredPosition + new Vector2(0f, anchorHeight + vOffset);
        }

        private static void OnClicked()
        {
            GatewayConfig.ShowRangeCircle.Value = !GatewayConfig.ShowRangeCircle.Value;
            ApplyCurrentLabel();
            ApplyRadioState();
        }

        private static void ApplyRadioState()
        {
            if (_radioIcon == null) return;
            _radioIcon.sprite = GatewayConfig.ShowRangeCircle.Value ? _radioOnSprite : _radioOffSprite;
        }

        private static void CaptureLabelTargets(GameObject root)
        {
            _labelTargets.Clear();
            _labelProperties.Clear();
            foreach (var c in root.GetComponentsInChildren<Component>(includeInactive: true))
            {
                if (c == null) continue;
                var t = c.GetType();
                if (t == typeof(Text))
                {
                    var prop = AccessTools.Property(t, "text");
                    if (prop != null && prop.CanWrite)
                    {
                        _labelTargets.Add(c);
                        _labelProperties.Add(prop);
                    }
                    continue;
                }
                if (t.Namespace != null && t.Namespace.StartsWith("TMPro"))
                {
                    var prop = AccessTools.Property(t, "text");
                    if (prop != null && prop.CanWrite)
                    {
                        _labelTargets.Add(c);
                        _labelProperties.Add(prop);
                    }
                }
            }
        }

        private static void ApplyCurrentLabel()
        {
            string text = Localization.instance != null
                ? Localization.instance.Localize("$bifrost_toggle_label")
                : "$bifrost_toggle_label";
            for (int i = 0; i < _labelTargets.Count; i++)
            {
                var target = _labelTargets[i];
                var prop = _labelProperties[i];
                if (target == null || prop == null) continue;
                try { prop.SetValue(target, text); }
                catch (System.Exception ex)
                {
                    PersonalGatewayPlugin.Log?.LogWarning($"MapToggleButton label set failed on {target.GetType().Name}: {ex.Message}");
                }
            }
        }

        // ---------- Sprite generators ----------

        private static Sprite CreateRoundedRectSprite(int size, int radius)
        {
            if (size < radius * 2) size = radius * 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            float r = radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float coverage = 1f;
                    int cx = -1, cy = -1;
                    if (x < radius && y < radius) { cx = radius; cy = radius; }
                    else if (x >= size - radius && y < radius) { cx = size - radius - 1; cy = radius; }
                    else if (x < radius && y >= size - radius) { cx = radius; cy = size - radius - 1; }
                    else if (x >= size - radius && y >= size - radius) { cx = size - radius - 1; cy = size - radius - 1; }
                    if (cx >= 0)
                    {
                        float dx = x + 0.5f - cx;
                        float dy = y + 0.5f - cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        coverage = Mathf.Clamp01(r - d + 0.5f);
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, coverage);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static Sprite CreateRadioSprite(int size, bool filled)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float outer = size * 0.45f;
            float ringInner = outer * 0.78f;
            float fillRadius = outer * 0.55f;

            var ringColor = new Color(1f, 0.85f, 0.35f, 1f);
            var fillColor = new Color(1f, 0.85f, 0.35f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d <= outer && d >= ringInner)
                    {
                        float edgeFade = Mathf.Min(d - ringInner, outer - d);
                        pixels[y * size + x] = new Color(ringColor.r, ringColor.g, ringColor.b, Mathf.Clamp01(edgeFade));
                    }
                    else if (filled && d <= fillRadius)
                    {
                        float edgeFade = fillRadius - d;
                        pixels[y * size + x] = new Color(fillColor.r, fillColor.g, fillColor.b, Mathf.Clamp01(edgeFade));
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        // ---------- Anchor / font donor lookup ----------

        private static GameObject FindCartographyAnchor(Minimap minimap)
        {
            if (minimap == null) return null;
            if (_cachedAnchor != null && _cachedAnchor.transform != null && _cachedAnchor.transform.parent != null)
            {
                return _cachedAnchor;
            }

            string[] cartographyFields = { "m_sharedMapToggle", "m_cartographyToggle", "m_sharedToggle" };
            foreach (var name in cartographyFields)
            {
                var go = TryReadFieldAsGameObject(minimap, name);
                if (go != null)
                {
                    _cachedAnchor = go;
                    return go;
                }
            }

            GameObject publicPos = null;
            foreach (var name in new[] { "m_publicPosition", "m_publicPositionToggle", "m_publicCheck" })
            {
                publicPos = TryReadFieldAsGameObject(minimap, name);
                if (publicPos != null) break;
            }

            // Try to find a cartography-labelled sibling of the public-position toggle.
            if (publicPos != null && publicPos.transform.parent != null)
            {
                var parent = publicPos.transform.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (child.gameObject == publicPos) continue;
                    if (child.gameObject == _go) continue;
                    var label = FindAnyTextString(child.gameObject);
                    if (LabelLooksLikeCartography(label))
                    {
                        _cachedAnchor = child.gameObject;
                        return _cachedAnchor;
                    }
                }
            }

            // Deeper scan: walk the whole large-map root looking for a cartography-labelled button.
            if (minimap.m_largeRoot != null)
            {
                var buttons = minimap.m_largeRoot.GetComponentsInChildren<Selectable>(includeInactive: true);
                foreach (var sel in buttons)
                {
                    if (sel == null) continue;
                    if (sel.gameObject == publicPos) continue;
                    if (sel.gameObject == _go) continue;
                    var label = FindAnyTextString(sel.gameObject);
                    if (LabelLooksLikeCartography(label))
                    {
                        _cachedAnchor = sel.gameObject;
                        return _cachedAnchor;
                    }
                }
            }

            _cachedAnchor = publicPos;
            return publicPos;
        }

        private static bool LabelLooksLikeCartography(string label)
        {
            if (string.IsNullOrEmpty(label)) return false;
            return label.IndexOf("cartograph", System.StringComparison.OrdinalIgnoreCase) >= 0
                || label.IndexOf("shared", System.StringComparison.OrdinalIgnoreCase) >= 0
                || label.IndexOf("$piece_cartograph", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static GameObject FindAnchorTextGameObject(GameObject anchor)
        {
            if (anchor == null) return null;
            var uiText = anchor.GetComponentInChildren<Text>(includeInactive: true);
            if (uiText != null) return uiText.gameObject;
            foreach (var c in anchor.GetComponentsInChildren<Component>(includeInactive: true))
            {
                if (c == null) continue;
                var t = c.GetType();
                if (t.Namespace != null && t.Namespace.StartsWith("TMPro"))
                {
                    return c.gameObject;
                }
            }
            return null;
        }

        private static string FindAnyTextString(GameObject root)
        {
            var uiText = root.GetComponentInChildren<Text>(includeInactive: true);
            if (uiText != null) return uiText.text;
            foreach (var c in root.GetComponentsInChildren<Component>(includeInactive: true))
            {
                if (c == null) continue;
                var t = c.GetType();
                if (t.Namespace != null && t.Namespace.StartsWith("TMPro"))
                {
                    var prop = AccessTools.Property(t, "text");
                    if (prop != null && prop.CanRead)
                    {
                        return prop.GetValue(c) as string;
                    }
                }
            }
            return null;
        }

        private static GameObject TryReadFieldAsGameObject(Minimap minimap, string fieldName)
        {
            var field = typeof(Minimap).GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (field == null) return null;
            var value = field.GetValue(field.IsStatic ? null : (object)minimap);
            if (value is GameObject go) return go;
            if (value is Component c && c != null) return c.gameObject;
            return null;
        }
    }
}
