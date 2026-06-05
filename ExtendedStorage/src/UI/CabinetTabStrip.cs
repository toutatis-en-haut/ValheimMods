using System;
using ExtendedStorage.Storage;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal class CabinetTabStrip : MonoBehaviour
    {
        private const float MinTabWidth = 80f;
        private const float MaxTabWidth = 250f;
        private const float TabHeight   = 36f;
        private const float TabGap      = 2f;
        private const float RowGap      = 2f;
        private const float LabelHorizontalPadding = 32f;
        private const float StripVerticalPadding = 4f;

        public Action<int> OnTabActivated;

        private CabinetContainer _cab;
        private CabinetTab[] _tabs;
        private RectTransform _rect;
        private Image _background;
        private float _stripWidth;
        private Action[] _onChangedHandlers;
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
                bg.type = Image.Type.Sliced;
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

            strip.RefreshAll();
            strip.SetActive(cab.Storage.ActiveTab);
            return strip;
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
            float x = StripVerticalPadding;
            float y = -StripVerticalPadding;

            for (int i = 0; i < _tabs.Length; i++)
            {
                var tab = _tabs[i];
                var label = _cab.Storage.EffectiveLabel(i);
                tab.SetLabel(label);

                var inv = _cab.GetTab(i);
                int fill = inv?.NrOfItems() ?? 0;
                tab.SetFill(fill);

                float measured = tab.MeasuredWidth(LabelHorizontalPadding);
                float width = Mathf.Clamp(measured, MinTabWidth, MaxTabWidth);

                if (x > StripVerticalPadding && x + width > _stripWidth - StripVerticalPadding)
                {
                    x = StripVerticalPadding;
                    y -= TabHeight + RowGap;
                }

                tab.Rect.sizeDelta = new Vector2(width, TabHeight);
                tab.Rect.anchoredPosition = new Vector2(x, y);

                x += width + TabGap;
            }

            TotalHeight = -y + TabHeight + StripVerticalPadding;
            if (_rect != null)
            {
                _rect.sizeDelta = new Vector2(_stripWidth, TotalHeight);
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
