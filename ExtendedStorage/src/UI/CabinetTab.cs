using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal class CabinetTab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color NormalColor = new Color(0.10f, 0.08f, 0.05f, 0.85f);
        private static readonly Color HoverColor  = new Color(0.20f, 0.16f, 0.10f, 0.92f);
        private static readonly Color ActiveColor = new Color(0.32f, 0.24f, 0.14f, 0.95f);

        private static readonly Color FillNormal = new Color(0.88f, 0.84f, 0.70f, 1f);
        private static readonly Color FillEmpty  = new Color(0.55f, 0.50f, 0.40f, 0.75f);

        public int Index { get; private set; }
        public RectTransform Rect { get; private set; }
        public Text LabelText { get; private set; }
        public Text FillText { get; private set; }
        public Image Background { get; private set; }
        public Action<int> OnClicked;

        private bool _hovered;
        private bool _active;

        public static CabinetTab Create(Transform parent, int index, Font font)
        {
            var go = new GameObject($"CabinetTab_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            var bg = go.GetComponent<Image>();
            bg.color = NormalColor;

            var tab = go.AddComponent<CabinetTab>();
            tab.Index = index;
            tab.Rect = rect;
            tab.Background = bg;

            tab.LabelText = CreateText(go.transform, "Label", font, TextAnchor.MiddleLeft);
            tab.FillText  = CreateText(go.transform, "Fill",  font, TextAnchor.MiddleRight);
            tab.FillText.fontSize = 12;

            // Label text — left-anchored with left padding.
            var lr = tab.LabelText.rectTransform;
            lr.anchorMin = new Vector2(0f, 0f);
            lr.anchorMax = new Vector2(1f, 1f);
            lr.offsetMin = new Vector2(10f, 0f);
            lr.offsetMax = new Vector2(-38f, 0f);

            // Fill indicator — right-anchored.
            var fr = tab.FillText.rectTransform;
            fr.anchorMin = new Vector2(1f, 0f);
            fr.anchorMax = new Vector2(1f, 1f);
            fr.pivot = new Vector2(1f, 0.5f);
            fr.offsetMin = new Vector2(-36f, 0f);
            fr.offsetMax = new Vector2(-6f, 0f);

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => tab.OnClicked?.Invoke(tab.Index));

            tab.Refresh();
            return tab;
        }

        public void SetLabel(string text)
        {
            if (LabelText != null) LabelText.text = text ?? string.Empty;
        }

        public void SetFill(int count, int max)
        {
            if (FillText == null) return;
            FillText.text = $"{count}/{max}";
            FillText.color = count == 0 ? FillEmpty : FillNormal;
        }

        public void SetActive(bool active)
        {
            _active = active;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData _)
        {
            _hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData _)
        {
            _hovered = false;
            Refresh();
        }

        private void Refresh()
        {
            if (Background == null) return;
            Background.color = _active ? ActiveColor : (_hovered ? HoverColor : NormalColor);
        }

        private static Text CreateText(Transform parent, string name, Font font, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.alignment = anchor;
            t.fontSize = 14;
            t.color = new Color(0.95f, 0.92f, 0.84f, 1f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }
    }
}
