using System.Collections.Generic;
using ExtendedStorage.Config;
using ExtendedStorage.Storage;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedStorage.UI
{
    internal class CabinetHoverTabSection : MonoBehaviour
    {
        private const float HeaderHeight = 22f;
        private const float HeaderGap = 4f;
        private const float CellGap = 2f;

        private Text _header;
        private GridLayoutGroup _grid;
        private RectTransform _gridRect;
        private RectTransform _rect;
        private Cell[] _cells;

        public RectTransform Rect => _rect;

        public static CabinetHoverTabSection Create(Transform parent, int cellSize)
        {
            var go = new GameObject(
                "CabinetHoverSection",
                typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            var section = go.AddComponent<CabinetHoverTabSection>();
            section._rect = go.GetComponent<RectTransform>();
            section._rect.anchorMin = new Vector2(0f, 1f);
            section._rect.anchorMax = new Vector2(0f, 1f);
            section._rect.pivot = new Vector2(0f, 1f);

            section._header = CreateHeader(go.transform);
            section.BuildGrid(cellSize);

            float gridWidth  = cellSize * CabinetStorage.TabWidth  + CellGap * (CabinetStorage.TabWidth - 1);
            float gridHeight = cellSize * CabinetStorage.TabHeight + CellGap * (CabinetStorage.TabHeight - 1);
            section._rect.sizeDelta = new Vector2(gridWidth, HeaderHeight + HeaderGap + gridHeight);

            var headerRect = section._header.GetComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(gridWidth, HeaderHeight);
            headerRect.anchoredPosition = new Vector2(0f, 0f);

            section._gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);
            section._gridRect.anchoredPosition = new Vector2(0f, -(HeaderHeight + HeaderGap));

            return section;
        }

        public void Render(string label, Inventory inv)
        {
            int count = inv?.NrOfItems() ?? 0;
            var countColor = ColorUtility.ToHtmlStringRGB(HammerTabStyle.CountColor);
            _header.text = $"{label} <color=#{countColor}>{count}/{CabinetStorage.TabSlots}</color>";

            // Map (x,y) → item for O(1) lookup; vanilla Inventory.GetItemAt is also fine
            // but allocating a dict keeps the per-cell loop cheap.
            var byPos = new Dictionary<int, ItemDrop.ItemData>(CabinetStorage.TabSlots);
            if (inv != null)
            {
                foreach (var item in inv.GetAllItems())
                {
                    if (item == null) continue;
                    int key = item.m_gridPos.y * CabinetStorage.TabWidth + item.m_gridPos.x;
                    byPos[key] = item;
                }
            }

            for (int y = 0; y < CabinetStorage.TabHeight; y++)
            {
                for (int x = 0; x < CabinetStorage.TabWidth; x++)
                {
                    int idx = y * CabinetStorage.TabWidth + x;
                    byPos.TryGetValue(idx, out var item);
                    _cells[idx].Set(item);
                }
            }
        }

        private void BuildGrid(int cellSize)
        {
            var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(transform, false);
            _gridRect = gridGo.GetComponent<RectTransform>();
            _gridRect.anchorMin = new Vector2(0f, 1f);
            _gridRect.anchorMax = new Vector2(0f, 1f);
            _gridRect.pivot = new Vector2(0f, 1f);

            _grid = gridGo.GetComponent<GridLayoutGroup>();
            _grid.cellSize = new Vector2(cellSize, cellSize);
            _grid.spacing = new Vector2(CellGap, CellGap);
            _grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _grid.childAlignment = TextAnchor.UpperLeft;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = CabinetStorage.TabWidth;

            _cells = new Cell[CabinetStorage.TabSlots];
            for (int i = 0; i < CabinetStorage.TabSlots; i++)
            {
                _cells[i] = Cell.Create(gridGo.transform);
            }
        }

        private static Text CreateHeader(Transform parent)
        {
            var go = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            var t = go.GetComponent<Text>();
            t.font = HammerTabStyle.LabelFont ?? Font.CreateDynamicFontFromOSFont("Arial", 16);
            t.fontSize = HammerTabStyle.FontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = HammerTabStyle.LabelColor;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        private class Cell
        {
            public Image Background;
            public Image Icon;
            public Text Stack;

            public static Cell Create(Transform parent)
            {
                var go = new GameObject("Cell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);

                var bg = go.GetComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.55f);
                bg.raycastTarget = false;

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(2f, 2f);
                iconRect.offsetMax = new Vector2(-2f, -2f);
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconImg.enabled = false;

                var stackGo = new GameObject("Stack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                stackGo.transform.SetParent(go.transform, false);
                var stackRect = stackGo.GetComponent<RectTransform>();
                stackRect.anchorMin = Vector2.zero;
                stackRect.anchorMax = Vector2.one;
                stackRect.offsetMin = new Vector2(2f, 2f);
                stackRect.offsetMax = new Vector2(-2f, -2f);
                var stackText = stackGo.GetComponent<Text>();
                stackText.font = HammerTabStyle.LabelFont ?? Font.CreateDynamicFontFromOSFont("Arial", 14);
                stackText.fontSize = Mathf.Max(10, HammerTabStyle.FontSize - 4);
                stackText.fontStyle = FontStyle.Bold;
                stackText.alignment = TextAnchor.LowerRight;
                stackText.color = Color.white;
                stackText.supportRichText = false;
                stackText.raycastTarget = false;
                stackText.text = string.Empty;

                return new Cell { Background = bg, Icon = iconImg, Stack = stackText };
            }

            public void Set(ItemDrop.ItemData item)
            {
                if (item == null)
                {
                    Icon.enabled = false;
                    Stack.text = string.Empty;
                    Background.color = new Color(0f, 0f, 0f, 0.35f);
                    return;
                }

                Background.color = new Color(0f, 0f, 0f, 0.65f);
                var sprite = item.GetIcon();
                if (sprite != null)
                {
                    Icon.sprite = sprite;
                    Icon.enabled = true;
                }
                else
                {
                    Icon.enabled = false;
                }
                Stack.text = item.m_stack > 1 ? item.m_stack.ToString() : string.Empty;
            }
        }
    }
}
