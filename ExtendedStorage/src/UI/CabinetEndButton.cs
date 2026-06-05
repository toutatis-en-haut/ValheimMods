using System;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal class CabinetEndButton : MonoBehaviour
    {
        public RectTransform Rect { get; private set; }

        public static CabinetEndButton Create(Transform parent, string glyph, Action onClick)
        {
            var go = new GameObject(
                $"CabinetEndButton_{glyph}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            var bg = go.GetComponent<Image>();
            if (HammerTabStyle.TabBackground != null)
            {
                bg.sprite = HammerTabStyle.TabBackground;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.22f, 0.16f, 0.10f, 0.92f);
            }

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var labelGo = new GameObject("Glyph",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lr = labelGo.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;

            var label = labelGo.GetComponent<Text>();
            label.font = HammerTabStyle.LabelFont ?? Font.CreateDynamicFontFromOSFont("Arial", 16);
            label.fontSize = HammerTabStyle.FontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = HammerTabStyle.LabelColor;
            label.text = glyph;
            label.raycastTarget = false;

            var endBtn = go.AddComponent<CabinetEndButton>();
            endBtn.Rect = rect;
            return endBtn;
        }
    }
}
