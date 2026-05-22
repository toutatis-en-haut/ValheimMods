using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PersonalGateway.Config;
using UnityEngine;
using UnityEngine.UI;

namespace PersonalGateway.UI
{
    internal static class MapToggleButton
    {
        private static GameObject _go;
        private static Image _backgroundImage;
        private static readonly List<Component> _labelTargets = new List<Component>();
        private static readonly List<PropertyInfo> _labelProperties = new List<PropertyInfo>();
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
                    PersonalGatewayPlugin.Log?.LogInfo("MapToggleButton: no cartography/public-position anchor found; toggle disabled.");
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

            var layout = go.GetComponent<LayoutElement>();
            layout.ignoreLayout = true;

            rt.anchorMin = anchorRt.anchorMin;
            rt.anchorMax = anchorRt.anchorMax;
            rt.pivot = anchorRt.pivot;
            rt.sizeDelta = anchorRt.sizeDelta;
            rt.localScale = anchorRt.localScale;

            _backgroundImage = go.GetComponent<Image>();
            _backgroundImage.sprite = RoundedRectBuilder.CreateRoundedRectSprite(64, 12);
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = new Color(0.08f, 0.06f, 0.04f, 0.85f);
            _backgroundImage.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = _backgroundImage;
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.15f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.75f, 0.65f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;
            btn.onClick.AddListener(OnClicked);

            AttachLabel(go, anchor);

            _go = go;
        }

        private static void AttachLabel(GameObject parent, GameObject anchor)
        {
            GameObject labelTemplate = FindAnchorTextGameObject(anchor);

            GameObject labelGo;
            if (labelTemplate != null)
            {
                labelGo = UnityEngine.Object.Instantiate(labelTemplate, parent.transform);
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
                t.alignment = TextAnchor.MiddleCenter;
                t.fontSize = 22;
            }

            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(14f, 0f);
            labelRt.offsetMax = new Vector2(-14f, 0f);
            labelRt.localScale = Vector3.one;

            foreach (var g in labelGo.GetComponentsInChildren<Graphic>(includeInactive: true))
            {
                if (g == _backgroundImage) continue;
                g.raycastTarget = false;
            }

            CaptureLabelTargets(labelGo);
        }

        private static void UpdatePosition(Minimap minimap)
        {
            if (_go == null) return;
            var anchor = FindCartographyAnchor(minimap);
            if (anchor == null) return;
            var anchorRt = (RectTransform)anchor.transform;
            var rt = (RectTransform)_go.transform;
            float vOffset = GatewayConfig.RangeToggleVerticalOffset.Value;
            rt.anchoredPosition = anchorRt.anchoredPosition + new Vector2(0f, anchorRt.sizeDelta.y + vOffset);
        }

        private static void OnClicked()
        {
            GatewayConfig.ShowRangeCircle.Value = !GatewayConfig.ShowRangeCircle.Value;
            ApplyCurrentLabel();
        }

        private static void CaptureLabelTargets(GameObject root)
        {
            _labelTargets.Clear();
            _labelProperties.Clear();
            var comps = root.GetComponentsInChildren<Component>(includeInactive: true);
            foreach (var c in comps)
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
            string token = GatewayConfig.ShowRangeCircle.Value ? "$bifrost_toggle_on" : "$bifrost_toggle_off";
            string text = Localization.instance != null ? Localization.instance.Localize(token) : token;
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

        private static GameObject FindCartographyAnchor(Minimap minimap)
        {
            if (minimap == null) return null;

            string[] cartographyFields = { "m_sharedMapToggle", "m_cartographyToggle", "m_sharedToggle" };
            foreach (var name in cartographyFields)
            {
                var go = TryReadFieldAsGameObject(minimap, name);
                if (go != null) return go;
            }

            GameObject publicPos = null;
            foreach (var name in new[] { "m_publicPosition", "m_publicPositionToggle", "m_publicCheck" })
            {
                publicPos = TryReadFieldAsGameObject(minimap, name);
                if (publicPos != null) break;
            }

            if (publicPos != null && publicPos.transform.parent != null)
            {
                var parent = publicPos.transform.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (child.gameObject == publicPos) continue;
                    if (child.gameObject == _go) continue;
                    var label = FindAnyTextString(child.gameObject);
                    if (label == null) continue;
                    if (label.IndexOf("cartograph", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || label.IndexOf("table", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || label.IndexOf("shared", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return child.gameObject;
                    }
                }
            }

            return publicPos;
        }

        private static GameObject FindAnchorTextGameObject(GameObject anchor)
        {
            if (anchor == null) return null;
            var uiText = anchor.GetComponentInChildren<Text>(includeInactive: true);
            if (uiText != null) return uiText.gameObject;
            var comps = anchor.GetComponentsInChildren<Component>(includeInactive: true);
            foreach (var c in comps)
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
            var comps = root.GetComponentsInChildren<Component>(includeInactive: true);
            foreach (var c in comps)
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
            var field = AccessTools.Field(typeof(Minimap), fieldName);
            if (field == null) return null;
            var value = field.GetValue(minimap);
            if (value is GameObject go) return go;
            if (value is Component c && c != null) return c.gameObject;
            return null;
        }
    }
}
