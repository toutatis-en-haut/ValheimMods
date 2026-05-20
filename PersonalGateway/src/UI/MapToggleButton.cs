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
        private static readonly List<Component> _labelTargets = new List<Component>();
        private static readonly List<PropertyInfo> _labelProperties = new List<PropertyInfo>();
        private static GameObject _sourceButton;
        private static bool _sourceMissingLogged;

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
            if (_go != null)
            {
                _go.SetActive(true);
                ApplyCurrentLabel();
            }
        }

        private static void Disable()
        {
            if (_go != null && _go.activeSelf) _go.SetActive(false);
        }

        private static bool EnsureCreated(Minimap minimap)
        {
            if (_go != null && _go.transform.parent != null) return true;

            _sourceButton = FindPublicPositionButton(minimap);
            if (_sourceButton == null)
            {
                if (!_sourceMissingLogged)
                {
                    PersonalGatewayPlugin.Log?.LogInfo("MapToggleButton: source toggle button not found; range toggle stays config-only.");
                    _sourceMissingLogged = true;
                }
                return false;
            }

            var sourceRt = (RectTransform)_sourceButton.transform;
            var parent = sourceRt.parent;

            var clone = UnityEngine.Object.Instantiate(_sourceButton, parent);
            clone.name = "BifrostRangeToggle";
            _go = clone;

            var rt = (RectTransform)clone.transform;
            rt.anchorMin = sourceRt.anchorMin;
            rt.anchorMax = sourceRt.anchorMax;
            rt.pivot = sourceRt.pivot;
            rt.sizeDelta = sourceRt.sizeDelta;

            float topY = sourceRt.anchoredPosition.y;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child == clone.transform) continue;
                if (!(child is RectTransform crt)) continue;
                bool isInteractive = child.GetComponent<Button>() != null || child.GetComponent<Toggle>() != null;
                if (!isInteractive) continue;
                if (crt.anchoredPosition.y > topY) topY = crt.anchoredPosition.y;
            }
            rt.anchoredPosition = new Vector2(sourceRt.anchoredPosition.x, topY + sourceRt.sizeDelta.y + 6f);

            StripLocalizeComponents(clone);
            CaptureLabelTargets(clone);
            WireInteraction(clone);
            ApplyCurrentLabel();
            return true;
        }

        private static void WireInteraction(GameObject clone)
        {
            var btn = clone.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnClickedToggleConfig);
            }
            var toggle = clone.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.SetIsOnWithoutNotify(GatewayConfig.ShowRangeCircle.Value);
                toggle.onValueChanged.AddListener(newValue =>
                {
                    GatewayConfig.ShowRangeCircle.Value = newValue;
                    ApplyCurrentLabel();
                });
            }
        }

        private static void OnClickedToggleConfig()
        {
            GatewayConfig.ShowRangeCircle.Value = !GatewayConfig.ShowRangeCircle.Value;
            var toggle = _go != null ? _go.GetComponent<Toggle>() : null;
            if (toggle != null) toggle.SetIsOnWithoutNotify(GatewayConfig.ShowRangeCircle.Value);
            ApplyCurrentLabel();
        }

        private static void StripLocalizeComponents(GameObject root)
        {
            var comps = root.GetComponentsInChildren<Component>(includeInactive: true);
            foreach (var c in comps)
            {
                if (c == null) continue;
                if (c.GetType().Name == "Localize")
                {
                    UnityEngine.Object.DestroyImmediate(c);
                }
            }
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

        private static GameObject FindPublicPositionButton(Minimap minimap)
        {
            string[] candidates = { "m_publicPosition", "m_publicPositionToggle", "m_publicCheck" };
            foreach (var name in candidates)
            {
                var field = AccessTools.Field(typeof(Minimap), name);
                if (field == null) continue;
                var value = field.GetValue(minimap);
                if (value is GameObject go) return go;
                if (value is Component c && c != null) return c.gameObject;
            }
            if (minimap.m_largeRoot != null)
            {
                var found = minimap.m_largeRoot.GetComponentsInChildren<Button>(includeInactive: true);
                foreach (var b in found)
                {
                    if (b == null) continue;
                    var label = FindAnyTextString(b.gameObject);
                    if (label == null) continue;
                    if (label.IndexOf("public", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || label.IndexOf("visible", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return b.gameObject;
                    }
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
    }
}
