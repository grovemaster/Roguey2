using System;
using System.Collections.Generic;
using JRogue.Item.Essence;
using JRogue.UI.Racial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Character
{
    public sealed class EssenceSlotPanelView : MonoBehaviour
    {
        GameObject _scrollRoot;
        RectTransform _contentRt;
        ScrollRect _scrollRect;
        Transform _cellsRoot;
        TextMeshProUGUI _disabledMessage;
        readonly List<EssenceCellChrome> _cells = new List<EssenceCellChrome>();

        sealed class EssenceCellChrome
        {
            public RectTransform Root;
            public Image Frame;
            public Image Icon;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Subtitle;
            public Button Button;
            public int SlotIndex;
        }

        public static EssenceSlotPanelView Create(Transform parent, Action<int> onSelect)
        {
            Transform existing = parent.Find("EssencePanel");
            EssenceSlotPanelView view;
            if (existing != null)
            {
                view = existing.GetComponent<EssenceSlotPanelView>() ??
                       existing.gameObject.AddComponent<EssenceSlotPanelView>();
                view.WireExisting();
            }
            else
            {
                var root = new GameObject("EssencePanel", typeof(RectTransform));
                root.transform.SetParent(parent, false);
                var le = root.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.flexibleHeight = 1f;

                var layout = root.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                TextMeshProUGUI heading = RacialUiTheme.CreateText(
                    root.transform, "Heading", "ESSENCES", RacialUiTheme.SectionFontSize,
                    TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                heading.color = RacialUiTheme.SectionLabel;
                heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

                BuildScrollArea(root.transform, out GameObject scrollRoot, out ScrollRect scrollRect,
                    out RectTransform contentRt, out Transform cellsRoot);

                var disabled = RacialUiTheme.CreateText(
                    root.transform, "Disabled", string.Empty, RacialUiTheme.MessageFontSize,
                    TextAlignmentOptions.TopLeft);
                disabled.color = RacialUiTheme.MutedText;
                disabled.gameObject.SetActive(false);

                view = root.AddComponent<EssenceSlotPanelView>();
                view._scrollRoot = scrollRoot;
                view._scrollRect = scrollRect;
                view._contentRt = contentRt;
                view._cellsRoot = cellsRoot;
                view._disabledMessage = disabled;
            }

            view._onSelect = onSelect;
            return view;
        }

        static void BuildScrollArea(
            Transform parent,
            out GameObject scrollRoot,
            out ScrollRect scrollRect,
            out RectTransform contentRt,
            out Transform cellsRoot)
        {
            var scrollGo = new GameObject("CellScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            scrollRoot = scrollGo;

            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 96f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(0f, contentRt.offsetMin.y);
            contentRt.offsetMax = new Vector2(0f, contentRt.offsetMax.y);

            var cellsLayout = content.AddComponent<VerticalLayoutGroup>();
            cellsLayout.spacing = 8f;
            cellsLayout.childControlWidth = true;
            cellsLayout.childControlHeight = true;
            cellsLayout.childForceExpandWidth = true;
            cellsLayout.childForceExpandHeight = false;

            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            cellsRoot = content.transform;
        }

        Action<int> _onSelect;

        void WireExisting()
        {
            Transform scroll = transform.Find("CellScroll");
            if (scroll != null)
            {
                _scrollRoot = scroll.gameObject;
                _scrollRect = scroll.GetComponent<ScrollRect>();
                Transform content = scroll.Find("Viewport/Content");
                _contentRt = content != null ? content.GetComponent<RectTransform>() : null;
                _cellsRoot = content != null ? content : scroll;
            }
            else
            {
                _cellsRoot = transform.Find("Cells") ?? transform;
            }

            _disabledMessage = transform.Find("Disabled")?.GetComponent<TextMeshProUGUI>();
        }

        public void Rebuild(CharacterEquipmentSheetModel sheet, int selectedEssenceIndex)
        {
            ClearCells();

            if (!sheet.CanGainEssences)
            {
                SetScrollVisible(false);
                if (_disabledMessage != null)
                {
                    _disabledMessage.gameObject.SetActive(true);
                    _disabledMessage.text = "This class cannot equip essences.";
                }

                return;
            }

            SetScrollVisible(true);
            if (_disabledMessage != null)
                _disabledMessage.gameObject.SetActive(false);

            foreach (EssenceSlotCellModel model in sheet.EssenceSlots)
                CreateCell(model, model.SlotIndex == selectedEssenceIndex);

            RefreshScrollLayout();
            ScrollToSelectedCell(selectedEssenceIndex);
        }

        void SetScrollVisible(bool visible)
        {
            if (_scrollRoot != null)
                _scrollRoot.SetActive(visible);
        }

        void RefreshScrollLayout()
        {
            if (_contentRt == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRt);
            if (_scrollRect != null && _scrollRect.viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.viewport);
        }

        void ScrollToSelectedCell(int selectedEssenceIndex)
        {
            if (_scrollRect == null || _contentRt == null || selectedEssenceIndex < 0)
                return;

            EssenceCellChrome selected = _cells.Find(c => c.SlotIndex == selectedEssenceIndex);
            if (selected?.Root == null)
                return;

            Canvas.ForceUpdateCanvases();
            RefreshScrollLayout();

            RectTransform viewport = _scrollRect.viewport;
            float contentHeight = _contentRt.rect.height;
            float viewportHeight = viewport.rect.height;
            if (contentHeight <= viewportHeight)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
                return;
            }

            float cellTop = -selected.Root.anchoredPosition.y;
            float cellHeight = selected.Root.rect.height;
            float cellCenter = cellTop + cellHeight * 0.5f;
            float scrollRange = contentHeight - viewportHeight;
            float targetOffset = Mathf.Clamp(cellCenter - viewportHeight * 0.5f, 0f, scrollRange);
            _scrollRect.verticalNormalizedPosition = 1f - targetOffset / scrollRange;
        }

        void ClearCells()
        {
            for (int i = _cellsRoot.childCount - 1; i >= 0; i--)
                Destroy(_cellsRoot.GetChild(i).gameObject);

            _cells.Clear();
        }

        void CreateCell(EssenceSlotCellModel model, bool selected)
        {
            var go = new GameObject($"Essence_{model.SlotIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_cellsRoot, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 88f;

            Image frame = go.GetComponent<Image>();
            frame.sprite = RacialUiTheme.PlaceholderSprite;
            frame.color = selected ? RacialUiTheme.FocusBorder : RacialUiTheme.InactiveBorder;

            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(8, 8, 8, 8);
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.minWidth = iconLe.preferredWidth = 56f;
            iconLe.minHeight = iconLe.preferredHeight = 56f;
            Image icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;

            var textCol = new GameObject("Text", typeof(RectTransform));
            textCol.transform.SetParent(go.transform, false);
            var textLe = textCol.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1f;
            var textLayout = textCol.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2f;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandWidth = true;

            TextMeshProUGUI title = RacialUiTheme.CreateText(
                textCol.transform, "Title", string.Empty, RacialUiTheme.PartyNameFontSize,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            TextMeshProUGUI subtitle = RacialUiTheme.CreateText(
                textCol.transform, "Subtitle", string.Empty, 13f,
                TextAlignmentOptions.MidlineLeft);
            subtitle.color = RacialUiTheme.MutedText;

            if (model.Occupied)
            {
                EssenceData essence = model.Essence;
                icon.sprite = essence.mapIcon != null ? essence.mapIcon : RacialUiTheme.ImprintEmblemSprite;
                icon.color = Color.white;
                title.text = essence.essenceName;
                subtitle.text = $"Tier {essence.tier}";
            }
            else
            {
                icon.sprite = RacialUiTheme.ImprintEmblemSprite;
                icon.color = new Color(1f, 1f, 1f, 0.2f);
                title.text = $"Essence slot {model.SlotIndex + 1}";
                subtitle.text = "Empty";
            }

            int captured = model.SlotIndex;
            go.GetComponent<Button>().onClick.AddListener(() => _onSelect?.Invoke(captured));

            _cells.Add(new EssenceCellChrome
            {
                Root = go.GetComponent<RectTransform>(),
                Frame = frame,
                Icon = icon,
                Title = title,
                Subtitle = subtitle,
                Button = go.GetComponent<Button>(),
                SlotIndex = captured
            });
        }
    }
}
