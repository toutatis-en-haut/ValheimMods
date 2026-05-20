using HarmonyLib;
using PersonalGateway.Config;
using UnityEngine;
using UnityEngine.UI;

namespace PersonalGateway.UI
{
    internal static class MapToggleButton
    {
        private static GameObject _go;
        private static Text _label;
        private static bool _searchedSource;
        private static GameObject _sourceButton;

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
                UpdateLabel();
            }
        }

        private static void Disable()
        {
            if (_go != null && _go.activeSelf) _go.SetActive(false);
        }

        private static bool EnsureCreated(Minimap minimap)
        {
            if (_go != null && _go.transform.parent != null) return true;

            if (!_searchedSource)
            {
                _searchedSource = true;
                _sourceButton = FindPublicPositionButton(minimap);
                if (_sourceButton == null)
                {
                    PersonalGatewayPlugin.Log?.LogInfo("MapToggleButton: source 'public position' button not found; range toggle stays config-only.");
                }
            }

            if (_sourceButton == null) return false;

            var clone = UnityEngine.Object.Instantiate(_sourceButton, _sourceButton.transform.parent);
            clone.name = "BifrostRangeToggle";
            _go = clone;

            var sourceRt = (RectTransform)_sourceButton.transform;
            var rt = (RectTransform)clone.transform;
            rt.anchorMin = sourceRt.anchorMin;
            rt.anchorMax = sourceRt.anchorMax;
            rt.pivot = sourceRt.pivot;
            rt.sizeDelta = sourceRt.sizeDelta;
            rt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, -(sourceRt.sizeDelta.y + 6f));

            _label = clone.GetComponentInChildren<Text>(includeInactive: true);

            var btn = clone.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnClicked);
            }

            var toggle = clone.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = GatewayConfig.ShowRangeCircle.Value;
                toggle.onValueChanged.AddListener(_ => OnClicked());
            }

            UpdateLabel();
            return true;
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
                    var txt = b.GetComponentInChildren<Text>(includeInactive: true);
                    if (txt == null) continue;
                    var s = txt.text ?? string.Empty;
                    if (s.IndexOf("public", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || s.IndexOf("visible", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return b.gameObject;
                    }
                }
            }
            return null;
        }

        private static void OnClicked()
        {
            GatewayConfig.ShowRangeCircle.Value = !GatewayConfig.ShowRangeCircle.Value;
            UpdateLabel();
        }

        private static void UpdateLabel()
        {
            if (_label == null) return;
            string token = GatewayConfig.ShowRangeCircle.Value ? "$bifrost_toggle_on" : "$bifrost_toggle_off";
            _label.text = Localization.instance != null ? Localization.instance.Localize(token) : token;
        }
    }
}
