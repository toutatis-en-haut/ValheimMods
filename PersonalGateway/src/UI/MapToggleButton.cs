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

            _sourceButton = FindToggleSourceButton(minimap);
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

            // Make sure the source's layout is settled before we read its height.
            if (parent is RectTransform parentRt)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
            }

            var clone = UnityEngine.Object.Instantiate(_sourceButton, parent);
            clone.name = "BifrostRangeToggle";
            _go = clone;

            // Earlier versions assumed the source's parent owned a VerticalLayoutGroup
            // that would re-flow children after SetAsFirstSibling. Current Valheim doesn't
            // — the toggles are positioned by anchored coordinates, so a cloned child
            // lands on top of its source. Position the clone manually instead, and opt it
            // out of any layout group that might still be present.
            var cloneRt = (RectTransform)clone.transform;
            cloneRt.anchorMin = sourceRt.anchorMin;
            cloneRt.anchorMax = sourceRt.anchorMax;
            cloneRt.pivot = sourceRt.pivot;
            cloneRt.sizeDelta = sourceRt.sizeDelta;

            const float spacingPixels = 6f;
            float stackHeight = sourceRt.rect.height > 0f ? sourceRt.rect.height : sourceRt.sizeDelta.y;
            if (stackHeight <= 0f) stackHeight = 48f;
            cloneRt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, stackHeight + spacingPixels);

            var layoutElement = clone.GetComponent<UnityEngine.UI.LayoutElement>()
                                ?? clone.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.ignoreLayout = true;

            clone.transform.SetAsLastSibling();

            // The cloned toggle inherits its source's ToggleGroup membership, which makes
            // it mutually exclusive with "Visible to other players". Cut that tie so our
            // toggle is independent.
            var inheritedToggle = clone.GetComponent<Toggle>();
            if (inheritedToggle != null) inheritedToggle.group = null;

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

        private static GameObject FindToggleSourceButton(Minimap minimap)
        {
            // Prefer the Cartography Table toggle (hex radio style) per the design intent.
            string[] cartographyFields = { "m_sharedMapToggle", "m_cartographyToggle", "m_sharedToggle" };
            foreach (var name in cartographyFields)
            {
                var go = TryReadFieldAsGameObject(minimap, name);
                if (go != null)
                {
                    PersonalGatewayPlugin.Log?.LogInfo($"MapToggleButton: source = Minimap.{name} (cartography toggle).");
                    return go;
                }
            }

            // Resolve the public-position toggle and scan its siblings for a cartography-labelled button.
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
                    var label = FindAnyTextString(child.gameObject);
                    if (label == null) continue;
                    if (label.IndexOf("cartograph", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || label.IndexOf("table", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || label.IndexOf("shared", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        PersonalGatewayPlugin.Log?.LogInfo($"MapToggleButton: source = sibling '{child.name}' (label: '{label}').");
                        return child.gameObject;
                    }
                }
            }

            if (minimap.m_largeRoot != null)
            {
                // In current Valheim the Cartography Table widget is a Toggle, not a
                // Button (the hex radio indicator), so we have to check both component
                // types or we silently fall back to the public-position toggle.
                var candidates = new List<GameObject>();
                foreach (var b in minimap.m_largeRoot.GetComponentsInChildren<Button>(includeInactive: true))
                {
                    if (b != null) candidates.Add(b.gameObject);
                }
                foreach (var t in minimap.m_largeRoot.GetComponentsInChildren<Toggle>(includeInactive: true))
                {
                    if (t != null) candidates.Add(t.gameObject);
                }
                foreach (var go in candidates)
                {
                    var label = FindAnyTextString(go);
                    if (label == null) continue;
                    if (label.IndexOf("cartograph", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        PersonalGatewayPlugin.Log?.LogInfo($"MapToggleButton: source = deep-scan '{go.name}' (cartography by label).");
                        return go;
                    }
                }
            }

            if (publicPos != null)
            {
                PersonalGatewayPlugin.Log?.LogInfo("MapToggleButton: cartography toggle not located; falling back to public-position toggle as source.");
                return publicPos;
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
