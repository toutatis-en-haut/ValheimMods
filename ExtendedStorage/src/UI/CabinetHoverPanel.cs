using ExtendedStorage.Config;
using ExtendedStorage.Storage;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal class CabinetHoverPanel : MonoBehaviour
    {
        private const float PanelPadding = 8f;
        private const float SectionGap = 8f;
        private const float RefreshInterval = 0.25f;
        private const float TopOffset = 80f;

        private static CabinetHoverPanel s_instance;

        private RectTransform _rect;
        private Image _background;
        private CabinetHoverTabSection[] _sections;
        private CabinetContainer _currentCab;
        private int _builtCellSize;
        private float _nextRefreshTime;

        public static void ShowFor(CabinetContainer cab)
        {
            if (cab == null) { Hide(); return; }

            var panel = EnsureInstance();
            if (panel == null) return;

            if (panel._builtCellSize != CabinetConfig.HoverCellSize.Value)
            {
                panel.Rebuild();
            }

            if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);

            if (panel._currentCab != cab)
            {
                panel._currentCab = cab;
                panel.Render();
                panel._nextRefreshTime = Time.unscaledTime + RefreshInterval;
                return;
            }

            if (Time.unscaledTime >= panel._nextRefreshTime)
            {
                panel.Render();
                panel._nextRefreshTime = Time.unscaledTime + RefreshInterval;
            }
        }

        public static void Hide()
        {
            if (s_instance == null) return;
            if (s_instance.gameObject.activeSelf) s_instance.gameObject.SetActive(false);
            s_instance._currentCab = null;
        }

        private static CabinetHoverPanel EnsureInstance()
        {
            if (s_instance != null) return s_instance;

            HammerTabStyle.Resolve();

            var parent = GUIManager.PixelFix != null
                ? GUIManager.PixelFix.transform
                : (Hud.instance != null ? Hud.instance.transform : null);
            if (parent == null) return null;

            var go = new GameObject(
                "ExtendedStorage_HoverPanel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var panel = go.AddComponent<CabinetHoverPanel>();
            panel._rect = go.GetComponent<RectTransform>();
            panel._background = go.GetComponent<Image>();

            panel._rect.anchorMin = new Vector2(0.5f, 1f);
            panel._rect.anchorMax = new Vector2(0.5f, 1f);
            panel._rect.pivot = new Vector2(0.5f, 1f);
            panel._rect.anchoredPosition = new Vector2(0f, -TopOffset);

            if (HammerTabStyle.PanelBackground != null)
            {
                panel._background.sprite = HammerTabStyle.PanelBackground;
                panel._background.type = Image.Type.Tiled;
                panel._background.color = Color.white;
            }
            else
            {
                panel._background.color = new Color(0.12f, 0.08f, 0.04f, 0.92f);
            }
            panel._background.raycastTarget = false;

            panel.Rebuild();
            go.SetActive(false);
            s_instance = panel;
            return panel;
        }

        private void Rebuild()
        {
            if (_sections != null)
            {
                for (int i = 0; i < _sections.Length; i++)
                {
                    if (_sections[i] != null) Destroy(_sections[i].gameObject);
                }
            }

            int cellSize = CabinetConfig.HoverCellSize.Value;
            _sections = new CabinetHoverTabSection[CabinetStorage.TabCount];

            float maxSectionWidth = 0f;
            float runningY = -PanelPadding;
            for (int i = 0; i < CabinetStorage.TabCount; i++)
            {
                var section = CabinetHoverTabSection.Create(transform, cellSize);
                _sections[i] = section;
                var sr = section.Rect;
                sr.anchorMin = new Vector2(0f, 1f);
                sr.anchorMax = new Vector2(0f, 1f);
                sr.pivot = new Vector2(0f, 1f);
                sr.anchoredPosition = new Vector2(PanelPadding, runningY);

                maxSectionWidth = Mathf.Max(maxSectionWidth, sr.sizeDelta.x);
                runningY -= sr.sizeDelta.y;
                if (i < CabinetStorage.TabCount - 1) runningY -= SectionGap;
            }

            float panelWidth  = maxSectionWidth + PanelPadding * 2f;
            float panelHeight = -runningY + PanelPadding;
            _rect.sizeDelta = new Vector2(panelWidth, panelHeight);
            _builtCellSize = cellSize;
        }

        private void Render()
        {
            if (_currentCab == null || _sections == null) return;
            var storage = _currentCab.Storage;
            if (storage == null) return;

            for (int i = 0; i < _sections.Length; i++)
            {
                var label = storage.EffectiveLabel(i);
                var inv = _currentCab.GetTab(i);
                _sections[i].Render(label, inv);
            }
        }
    }
}
