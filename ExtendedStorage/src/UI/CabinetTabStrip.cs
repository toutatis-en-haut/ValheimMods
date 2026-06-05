using System;
using ExtendedStorage.Storage;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal class CabinetTabStrip : MonoBehaviour
    {
        private const float MinTabWidth = 90f;
        private const float MaxTabWidth = 250f;
        private const float TabHeight   = 36f;
        private const float TabGap      = 2f;
        private const float RowGap      = 2f;
        private const float LabelHorizontalPadding = 32f;
        private const float StripVerticalPadding = 6f;
        private const float EndButtonWidth = 36f;
        private const float EndButtonGap = 6f;

        public Action<int> OnTabActivated;

        private CabinetContainer _cab;
        private CabinetTab[] _tabs;
        private RectTransform _rect;
        private Image _background;
        private float _stripWidth;
        private Action[] _onChangedHandlers;
        private CabinetEndButton _qButton;
        private CabinetEndButton _eButton;
        public float TotalHeight { get; private set; }

        public static CabinetTabStrip Build(Transform parent, CabinetContainer cab, float stripWidth)
        {
            HammerTabStyle.Resolve();

            var go = new GameObject("CabinetTabStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(stripWidth, TabHeight + StripVerticalPadding * 2f);

            var bg = go.GetComponent<Image>();
            if (HammerTabStyle.PanelBackground != null)
            {
                bg.sprite = HammerTabStyle.PanelBackground;
                bg.type = Image.Type.Tiled;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.12f, 0.08f, 0.04f, 0.90f);
            }

            var strip = go.AddComponent<CabinetTabStrip>();
            strip._rect = rect;
            strip._background = bg;
            strip._cab = cab;
            strip._stripWidth = stripWidth;

            strip._tabs = new CabinetTab[CabinetStorage.TabCount];
            strip._onChangedHandlers = new Action[CabinetStorage.TabCount];
            for (int i = 0; i < CabinetStorage.TabCount; i++)
            {
                var tab = CabinetTab.Create(go.transform, i);
                tab.OnClicked = idx => strip.OnTabClicked(idx);
                strip._tabs[i] = tab;

                int captured = i;
                Action handler = () => strip.RefreshFill(captured);
                strip._onChangedHandlers[i] = handler;
                var inv = cab.GetTab(i);
                if (inv != null) inv.m_onChanged += handler;
            }

            strip._qButton = CabinetEndButton.Create(go.transform, "Q", () => strip.StepActive(-1));
            strip._eButton = CabinetEndButton.Create(go.transform, "E", () => strip.StepActive(+1));

            strip.RefreshAll();
            strip.SetActive(cab.Storage.ActiveTab);
            return strip;
        }

        private void Update()
        {
            if (_cab == null || !_cab.IsReady) return;

            bool prev = Input.GetKeyDown(KeyCode.Q) || TryGetButtonDown("JoyLBumper") || TryGetButtonDown("JoyLB");
            bool next = Input.GetKeyDown(KeyCode.E) || TryGetButtonDown("JoyRBumper") || TryGetButtonDown("JoyRB");

            if (prev) StepActive(-1);
            else if (next) StepActive(+1);
        }

        private void StepActive(int delta)
        {
            int target = ((_cab.Storage.ActiveTab + delta) % CabinetStorage.TabCount + CabinetStorage.TabCount) % CabinetStorage.TabCount;
            SetActive(target);
        }

        private static bool TryGetButtonDown(string name)
        {
            try { return ZInput.instance != null && ZInput.GetButtonDown(name); }
            catch { return false; }
        }

        public void SetActive(int index)
        {
            if (index < 0 || index >= CabinetStorage.TabCount) return;
            _cab.Storage.ActiveTab = index;
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].SetActive(i == index);
            }
            OnTabActivated?.Invoke(index);
        }

        public void RefreshAll()
        {
            // First pass: figure out per-tab widths.
            float[] widths = new float[_tabs.Length];
            float totalRowOneWidth = 0f;
            for (int i = 0; i < _tabs.Length; i++)
            {
                var tab = _tabs[i];
                tab.SetLabel(_cab.Storage.EffectiveLabel(i));
                var inv = _cab.GetTab(i);
                tab.SetFill(inv?.NrOfItems() ?? 0);

                float measured = tab.MeasuredWidth(LabelHorizontalPadding);
                widths[i] = Mathf.Clamp(measured, MinTabWidth, MaxTabWidth);
                totalRowOneWidth += widths[i];
                if (i > 0) totalRowOneWidth += TabGap;
            }

            // End buttons are anchored to the strip's left/right ends, sharing
            // its full height.
            float endButtonOuterMargin = StripVerticalPadding;
            float tabsAvailableWidth = _stripWidth
                - 2f * EndButtonWidth - 2f * EndButtonGap - 2f * endButtonOuterMargin;

            // Single-row centred layout when everything fits; otherwise fall
            // back to multi-row left-aligned.
            bool singleRow = totalRowOneWidth <= tabsAvailableWidth;

            float y = -StripVerticalPadding;
            float rowOriginX;
            float x;

            if (singleRow)
            {
                rowOriginX = endButtonOuterMargin + EndButtonWidth + EndButtonGap
                             + (tabsAvailableWidth - totalRowOneWidth) * 0.5f;
                x = rowOriginX;
                for (int i = 0; i < _tabs.Length; i++)
                {
                    _tabs[i].Rect.sizeDelta = new Vector2(widths[i], TabHeight);
                    _tabs[i].Rect.anchoredPosition = new Vector2(x, y);
                    x += widths[i] + TabGap;
                }
            }
            else
            {
                rowOriginX = endButtonOuterMargin + EndButtonWidth + EndButtonGap;
                x = rowOriginX;
                for (int i = 0; i < _tabs.Length; i++)
                {
                    if (x > rowOriginX && x + widths[i] > rowOriginX + tabsAvailableWidth)
                    {
                        x = rowOriginX;
                        y -= TabHeight + RowGap;
                    }
                    _tabs[i].Rect.sizeDelta = new Vector2(widths[i], TabHeight);
                    _tabs[i].Rect.anchoredPosition = new Vector2(x, y);
                    x += widths[i] + TabGap;
                }
            }

            TotalHeight = -y + TabHeight + StripVerticalPadding;
            if (_rect != null)
            {
                _rect.sizeDelta = new Vector2(_stripWidth, TotalHeight);
            }

            PlaceEndButtons(endButtonOuterMargin);
        }

        private void PlaceEndButtons(float outerMargin)
        {
            if (_qButton != null)
            {
                var r = _qButton.Rect;
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.sizeDelta = new Vector2(EndButtonWidth, TabHeight);
                r.anchoredPosition = new Vector2(outerMargin, -StripVerticalPadding);
            }
            if (_eButton != null)
            {
                var r = _eButton.Rect;
                r.anchorMin = new Vector2(1f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(1f, 1f);
                r.sizeDelta = new Vector2(EndButtonWidth, TabHeight);
                r.anchoredPosition = new Vector2(-outerMargin, -StripVerticalPadding);
            }
        }

        private void RefreshFill(int index)
        {
            if (index < 0 || index >= _tabs.Length) return;
            var inv = _cab.GetTab(index);
            int fill = inv?.NrOfItems() ?? 0;
            _tabs[index].SetFill(fill);
        }

        private void OnTabClicked(int index) => SetActive(index);

        private void OnDestroy()
        {
            if (_cab != null && _onChangedHandlers != null)
            {
                for (int i = 0; i < CabinetStorage.TabCount; i++)
                {
                    var inv = _cab.GetTab(i);
                    if (inv != null && _onChangedHandlers[i] != null)
                    {
                        inv.m_onChanged -= _onChangedHandlers[i];
                    }
                }
            }
        }
    }
}
