using System;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal class CabinetTab : MonoBehaviour
    {
        public int Index { get; private set; }
        public RectTransform Rect { get; private set; }
        public Text LabelText { get; private set; }
        public Image SelectedImage { get; private set; }
        public Action<int> OnClicked;

        private string _label = "1";
        private int _fillCount;
        private bool _active;

        public static CabinetTab Create(Transform parent, int index)
        {
            var go = new GameObject(
                $"CabinetTab_{index}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            // Root Image is the click target; keep it transparent so the wood
            // panel underneath shows through on inactive tabs.
            var rootImg = go.GetComponent<Image>();
            rootImg.color = new Color(1f, 1f, 1f, 0f);
            rootImg.raycastTarget = true;

            var tab = go.AddComponent<CabinetTab>();
            tab.Index = index;
            tab.Rect = rect;

            // Selected highlight — sits under the label, shown only when active.
            tab.SelectedImage = CreateSelectedImage(go.transform);

            // Single Text using rich-text for "Label [N/15]" with the count in
            // yellow. Vanilla Hammer tabs use the same pattern.
            tab.LabelText = CreateLabel(go.transform);

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => tab.OnClicked?.Invoke(tab.Index));

            tab.RefreshActive();
            return tab;
        }

        public void SetLabel(string text)
        {
            _label = string.IsNullOrEmpty(text) ? "?" : text;
            RefreshLabel();
        }

        public void SetFill(int count)
        {
            _fillCount = count;
            RefreshLabel();
        }

        public void SetActive(bool active)
        {
            _active = active;
            RefreshActive();
        }

        public float MeasuredWidth(float horizontalPadding)
        {
            if (LabelText == null) return 0f;
            // Force-rebuild geometry to get a fresh preferredWidth.
            LabelText.SetAllDirty();
            return LabelText.preferredWidth + horizontalPadding;
        }

        private void RefreshLabel()
        {
            if (LabelText == null) return;
            var countColorHex = ColorUtility.ToHtmlStringRGBA(HammerTabStyle.CountColor);
            LabelText.text = $"{_label} <color=#{countColorHex}>[{_fillCount}]</color>";
        }

        private void RefreshActive()
        {
            if (SelectedImage != null) SelectedImage.gameObject.SetActive(_active);
            if (LabelText != null)
            {
                LabelText.color = _active
                    ? new Color(1f, 1f, 1f, 1f)
                    : HammerTabStyle.LabelColor;
            }
        }

        private static Image CreateSelectedImage(Transform parent)
        {
            var go = new GameObject("Selected", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            if (HammerTabStyle.SelectedHighlight != null)
            {
                img.sprite = HammerTabStyle.SelectedHighlight;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                // Fallback: solid blue rectangle.
                img.color = new Color(0.30f, 0.65f, 0.95f, 0.55f);
            }
            go.SetActive(false);
            return img;
        }

        private static Text CreateLabel(Transform parent)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);

            var t = go.GetComponent<Text>();
            t.font = HammerTabStyle.LabelFont ?? Font.CreateDynamicFontFromOSFont("Arial", 16);
            t.fontSize = HammerTabStyle.FontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = HammerTabStyle.LabelColor;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }
    }
}
