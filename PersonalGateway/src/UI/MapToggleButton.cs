using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PersonalGateway.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PersonalGateway.UI
{
    /// <summary>
    /// Adds a "Bifrost Totem Range: ON/OFF" toggle to Valheim's large map UI by
    /// cloning the Cartography Table toggle for visual fidelity (matching frame,
    /// font, icon style) and then surgically destroying every Valheim behavioural
    /// component on the clone (Toggle, ToggleGroup, EventTrigger, Localize, any
    /// non-Button Selectable). A fresh Button is wired to flip our config.
    /// </summary>
    internal static class MapToggleButton
    {
        private static GameObject _go;
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

            GameObject source = FindCartographyAnchor(minimap);
            if (source == null)
            {
                if (!_anchorMissingLogged)
                {
                    PersonalGatewayPlugin.Log?.LogInfo("MapToggleButton: cartography/public-position anchor not found; toggle disabled.");
                    _anchorMissingLogged = true;
                }
                return false;
            }

            BuildFromClone(source);
            return _go != null;
        }

        private static void BuildFromClone(GameObject source)
        {
            var sourceRt = (RectTransform)source.transform;
            var parent = sourceRt.parent;
            if (parent == null) return;

            // Clone the visuals wholesale. This preserves the rounded background,
            // hex icon, TMP text styling, and child hierarchy used by Valheim.
            var clone = UnityEngine.Object.Instantiate(source, parent);
            clone.name = "BifrostRangeToggle";

            StripBehavioralComponents(clone);
            StripLocalizeComponents(clone);

            // Take the clone out of the parent's VerticalLayoutGroup so we can
            // position it manually with a config-tunable offset.
            var layout = clone.GetComponent<LayoutElement>() ?? clone.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            // Make the root act as a single button surface that catches clicks
            // (prevents click-through to the cartography toggle behind us).
            var rootImage = clone.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.raycastTarget = true;
            }
            foreach (var img in clone.GetComponentsInChildren<Image>(includeInactive: true))
            {
                if (img == rootImage) continue;
                img.raycastTarget = false;
            }
            foreach (var graphic in clone.GetComponentsInChildren<Graphic>(includeInactive: true))
            {
                if (graphic is Image) continue;
                graphic.raycastTarget = false;
            }

            var btn = clone.GetComponent<Button>();
            if (btn == null) btn = clone.AddComponent<Button>();
            if (rootImage != null) btn.targetGraphic = rootImage;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClicked);

            CaptureLabelTargets(clone);

            _go = clone;
            ApplyCurrentLabel();
        }

        /// <summary>
        /// Destroy every behavioural component that could carry Valheim's
        /// cartography logic across to the clone. Everything visual (Image,
        /// TMP text, RectTransform, Canvas-related) is preserved.
        /// </summary>
        private static void StripBehavioralComponents(GameObject clone)
        {
            foreach (var t in clone.GetComponentsInChildren<Toggle>(includeInactive: true))
            {
                if (t != null) UnityEngine.Object.DestroyImmediate(t);
            }
            foreach (var tg in clone.GetComponentsInChildren<ToggleGroup>(includeInactive: true))
            {
                if (tg != null) UnityEngine.Object.DestroyImmediate(tg);
            }
            foreach (var ev in clone.GetComponentsInChildren<EventTrigger>(includeInactive: true))
            {
                if (ev != null) UnityEngine.Object.DestroyImmediate(ev);
            }
            // Strip any Selectable other than the Button we will add (or already exists).
            foreach (var sel in clone.GetComponentsInChildren<Selectable>(includeInactive: true))
            {
                if (sel == null) continue;
                if (sel is Button) continue;
                UnityEngine.Object.DestroyImmediate(sel);
            }
        }

        private static void StripLocalizeComponents(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Component>(includeInactive: true))
            {
                if (c == null) continue;
                if (c.GetType().Name == "Localize")
                {
                    UnityEngine.Object.DestroyImmediate(c);
                }
            }
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
