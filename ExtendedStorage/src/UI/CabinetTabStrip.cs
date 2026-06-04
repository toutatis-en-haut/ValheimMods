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
        private const float TabHeight   = 28f;
        private const float TabGap      = 4f;
        private const float RowGap      = 4f;
        private const float LabelPaddingX = 22f; // left + right padding around the label

        public Action<int> OnTabActivated;

        private CabinetContainer _cab;
        private CabinetTab[] _tabs;
        private float _stripWidth;
        private RectTransform _rect;
        private Action[] _onChangedHandlers;
        public float TotalHeight { get; private set; }

        public static CabinetTabStrip Build(Transform parent, CabinetContainer cab, float stripWidth, Font font)
        {
            var go = new GameObject("CabinetTabStrip", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(stripWidth, TabHeight);

            var strip = go.AddComponent<CabinetTabStrip>();
            strip._rect = rect;
            strip._cab = cab;
            strip._stripWidth = stripWidth;

            strip._tabs = new CabinetTab[CabinetStorage.TabCount];
            strip._onChangedHandlers = new Action[CabinetStorage.TabCount];
            for (int i = 0; i < CabinetStorage.TabCount; i++)
            {
                var tab = CabinetTab.Create(go.transform, i, font);
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
            float x = 0f;
            float y = 0f;
            float maxRowEnd = 0f;

            for (int i = 0; i < _tabs.Length; i++)
            {
                var tab = _tabs[i];
                var label = _cab.Storage.EffectiveLabel(i);
                tab.SetLabel(label);

                var inv = _cab.GetTab(i);
                int fill = inv?.NrOfItems() ?? 0;
                tab.SetFill(fill, CabinetStorage.TabSlots);

                float textWidth = tab.LabelText.preferredWidth;
                float fillWidth = tab.FillText.preferredWidth;
                float desired = textWidth + LabelPaddingX + fillWidth + 8f;
                float width = Mathf.Clamp(desired, MinTabWidth, MaxTabWidth);

                // Row cascade: if this tab would overflow, wrap to the next row.
                if (x > 0f && x + width > _stripWidth)
                {
                    x = 0f;
                    y -= TabHeight + RowGap;
                }

                tab.Rect.sizeDelta = new Vector2(width, TabHeight);
                tab.Rect.anchoredPosition = new Vector2(x, y);

                x += width + TabGap;
                maxRowEnd = Mathf.Max(maxRowEnd, x);
            }

            TotalHeight = -y + TabHeight;
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
            _tabs[index].SetFill(fill, CabinetStorage.TabSlots);
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
