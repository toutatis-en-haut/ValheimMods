using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal class CabinetTab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const int MaxLabelLength = 25;

        public int Index { get; private set; }
        public RectTransform Rect { get; private set; }
        public Text LabelText { get; private set; }
        public Image SelectedImage { get; private set; }
        public Action<int> OnClicked;

        public bool IsHovered { get; private set; }
        public bool IsEditing { get; private set; }
        public string EditingText => _input != null ? _input.text : null;

        private string _label = "1";
        private int _fillCount;
        private bool _active;
        private InputField _input;
        private GameObject _inputGo;
        private Action<int, string> _onCommit;
        private Action<int, string> _onLiveChange;

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

            var rootImg = go.GetComponent<Image>();
            rootImg.sprite = null;
            rootImg.color = HammerTabStyle.TabBodyColor;
            rootImg.raycastTarget = true;

            var tab = go.AddComponent<CabinetTab>();
            tab.Index = index;
            tab.Rect = rect;

            tab.SelectedImage = CreateSelectedImage(go.transform);
            tab.LabelText = CreateLabel(go.transform);

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => tab.OnClicked?.Invoke(tab.Index));

            tab.RefreshActive();
            return tab;
        }

        public void OnPointerEnter(PointerEventData _) { IsHovered = true; }
        public void OnPointerExit(PointerEventData _)  { IsHovered = false; }

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
            LabelText.SetAllDirty();
            return LabelText.preferredWidth + horizontalPadding;
        }

        public void EnterEditMode(string initial, Action<int, string> onCommit, Action<int, string> onLiveChange)
        {
            if (IsEditing) return;
            EnsureInput();

            _onCommit = onCommit;
            _onLiveChange = onLiveChange;

            _input.SetTextWithoutNotify(initial ?? string.Empty);
            _inputGo.SetActive(true);
            LabelText.gameObject.SetActive(false);
            IsEditing = true;

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_inputGo);
            }
            _input.ActivateInputField();
            _input.MoveTextEnd(false);
        }

        public void ExitEditMode(bool commit)
        {
            if (!IsEditing) return;
            string final = _input != null ? _input.text : string.Empty;

            if (_input != null) _input.DeactivateInputField();
            if (_inputGo != null) _inputGo.SetActive(false);
            LabelText.gameObject.SetActive(true);

            var commitCb = _onCommit;
            IsEditing = false;
            _onCommit = null;
            _onLiveChange = null;

            if (commit && commitCb != null)
            {
                commitCb(Index, final);
            }
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

        private void EnsureInput()
        {
            if (_input != null) return;

            _inputGo = new GameObject("LabelInput",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            _inputGo.transform.SetParent(transform, false);
            var rect = _inputGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 3f);
            rect.offsetMax = new Vector2(-6f, -3f);

            var bg = _inputGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            bg.raycastTarget = true;

            // Visible text component the InputField edits.
            var textGo = new GameObject("Text",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(_inputGo.transform, false);
            var tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(4f, 0f);
            tr.offsetMax = new Vector2(-4f, 0f);
            var textComp = textGo.GetComponent<Text>();
            textComp.font = HammerTabStyle.LabelFont ?? Font.CreateDynamicFontFromOSFont("Arial", 16);
            textComp.fontSize = HammerTabStyle.FontSize;
            textComp.fontStyle = FontStyle.Bold;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.supportRichText = false;
            textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComp.verticalOverflow = VerticalWrapMode.Truncate;
            textComp.raycastTarget = false;

            _input = _inputGo.GetComponent<InputField>();
            _input.textComponent = textComp;
            _input.characterLimit = MaxLabelLength;
            _input.lineType = InputField.LineType.SingleLine;
            _input.contentType = InputField.ContentType.Standard;
            _input.caretBlinkRate = 0.85f;
            _input.caretWidth = 2;

            _input.onValueChanged.AddListener(value =>
            {
                _onLiveChange?.Invoke(Index, value);
            });
            _input.onSubmit.AddListener(_ =>
            {
                if (IsEditing) ExitEditMode(commit: true);
            });
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
            img.sprite = null;
            img.color = HammerTabStyle.ActiveHighlightColor;
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
            t.fontStyle = FontStyle.Bold;
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
