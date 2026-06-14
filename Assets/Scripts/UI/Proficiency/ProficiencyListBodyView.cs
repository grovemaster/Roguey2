using System;
using System.Collections.Generic;
using JRogue.Stats;
using JRogue.UI.Racial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Proficiency
{
    public sealed class ProficiencyListBodyView : MonoBehaviour
    {
        readonly List<GameObject> _rowObjects = new();
        readonly List<Button> _filterButtons = new();

        RectTransform _scrollContent;
        Action<ProficiencyKind> _onSelect;
        ProficiencyCategoryFilter _activeFilter = ProficiencyCategoryFilter.All;
        ProficiencyKind _selectedKind = ProficiencyKind.Fighting;
        ProficiencySheetModel _currentSheet;

        public static ProficiencyListBodyView Create(Transform parent, Action<ProficiencyKind> onSelect)
        {
            var root = new GameObject("ProficiencyListBody", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var view = root.AddComponent<ProficiencyListBodyView>();
            view._onSelect = onSelect;
            view.BuildFilterRow(root.transform);
            view.BuildScrollArea(root.transform);

            var le = root.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;
            le.minHeight = 240f;

            return view;
        }

        void BuildFilterRow(Transform parent)
        {
            var row = new GameObject("FilterRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = 32f;
            rowLe.preferredHeight = 32f;

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            foreach (ProficiencyCategoryFilter filter in ProficiencyCategories.GetAllFilters())
            {
                Button button = CreateFilterChip(row.transform, filter);
                _filterButtons.Add(button);
            }
        }

        Button CreateFilterChip(Transform parent, ProficiencyCategoryFilter filter)
        {
            var go = new GameObject($"Filter_{filter}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Image bg = go.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = RacialUiTheme.CardBackground;

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 72f;
            le.preferredHeight = 28f;

            TextMeshProUGUI label = RacialUiTheme.CreateText(
                go.transform,
                "Label",
                ProficiencyCategories.GetFilterLabel(filter),
                14f,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            RacialUiTheme.Stretch(label.rectTransform);
            label.color = RacialUiTheme.BodyText;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => SetFilter(filter));
            return button;
        }

        void BuildScrollArea(Transform parent)
        {
            var scrollHost = new GameObject("ListScroll", typeof(RectTransform));
            scrollHost.transform.SetParent(parent, false);
            var scrollLe = scrollHost.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 200f;

            ScrollRect scroll = scrollHost.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollHost.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            viewport.GetComponent<Image>().color = new Color(0.12f, 0.125f, 0.135f, 0.92f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            _scrollContent = content.GetComponent<RectTransform>();
            _scrollContent.anchorMin = new Vector2(0f, 1f);
            _scrollContent.anchorMax = new Vector2(1f, 1f);
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            _scrollContent.offsetMin = Vector2.zero;
            _scrollContent.offsetMax = Vector2.zero;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(4, 4, 4, 8);
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = _scrollContent;
        }

        public void SetFilter(ProficiencyCategoryFilter filter)
        {
            _activeFilter = filter;
            Refresh(_currentSheet, _selectedKind);
        }

        public void Refresh(ProficiencySheetModel sheet, ProficiencyKind selectedKind)
        {
            _currentSheet = sheet;
            _selectedKind = selectedKind;
            ClearRows();

            if (sheet?.Rows == null || sheet.Rows.Count == 0)
            {
                CreateMessageRow("No party member selected.");
                UpdateFilterVisuals();
                return;
            }

            ProficiencyMenuCategory? onlyCategory = ProficiencyCategories.ToMenuCategory(_activeFilter);
            IReadOnlyList<ProficiencyMenuCategory> sections = ProficiencyCategories.GetAllSections();

            for (int s = 0; s < sections.Count; s++)
            {
                ProficiencyMenuCategory section = sections[s];
                if (onlyCategory.HasValue && onlyCategory.Value != section)
                    continue;

                bool hasRows = false;
                for (int i = 0; i < sheet.Rows.Count; i++)
                {
                    if (sheet.Rows[i].Category == section)
                    {
                        hasRows = true;
                        break;
                    }
                }

                if (!hasRows)
                    continue;

                CreateSectionHeader(ProficiencyCategories.GetSectionHeader(section));

                for (int i = 0; i < sheet.Rows.Count; i++)
                {
                    ProficiencyRowViewModel row = sheet.Rows[i];
                    if (row.Category != section)
                        continue;

                    CreateRow(row);
                }
            }

            UpdateFilterVisuals();
        }

        void UpdateFilterVisuals()
        {
            IReadOnlyList<ProficiencyCategoryFilter> filters = ProficiencyCategories.GetAllFilters();
            for (int i = 0; i < _filterButtons.Count && i < filters.Count; i++)
            {
                Image bg = _filterButtons[i].GetComponent<Image>();
                if (bg == null)
                    continue;

                bg.color = filters[i] == _activeFilter
                    ? RacialUiTheme.ActiveAccent
                    : RacialUiTheme.CardBackground;
            }
        }

        void ClearRows()
        {
            for (int i = 0; i < _rowObjects.Count; i++)
            {
                if (_rowObjects[i] != null)
                    Destroy(_rowObjects[i]);
            }

            _rowObjects.Clear();
        }

        void CreateMessageRow(string message)
        {
            var go = new GameObject("Message", typeof(RectTransform));
            go.transform.SetParent(_scrollContent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 40f;

            TextMeshProUGUI text = RacialUiTheme.CreateText(
                go.transform,
                "Label",
                message,
                RacialUiTheme.MessageFontSize,
                TextAlignmentOptions.MidlineLeft);
            RacialUiTheme.Stretch(text.rectTransform);
            text.color = RacialUiTheme.MutedText;
            _rowObjects.Add(go);
        }

        void CreateSectionHeader(string label)
        {
            var go = new GameObject("SectionHeader", typeof(RectTransform));
            go.transform.SetParent(_scrollContent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 24f;

            TextMeshProUGUI text = RacialUiTheme.CreateText(
                go.transform,
                "Label",
                label,
                18f,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            RacialUiTheme.Stretch(text.rectTransform);
            text.color = RacialUiTheme.BannerText;
            _rowObjects.Add(go);
        }

        void CreateRow(ProficiencyRowViewModel row)
        {
            var go = new GameObject($"Row_{row.Kind}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_scrollContent, false);

            Image bg = go.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bool selected = row.Kind == _selectedKind;
            bg.color = selected
                ? new Color(0.18f, 0.19f, 0.22f, 0.98f)
                : RacialUiTheme.CardBackground;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 38f;
            le.preferredHeight = 38f;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            if (!row.Eligible)
            {
                CanvasGroup group = go.AddComponent<CanvasGroup>();
                group.alpha = 0.45f;
            }

            CreateRowLabel(go.transform, row.DisplayName, 170f, row.Eligible ? FontStyles.Normal : FontStyles.Italic);
            CreateRowLabel(go.transform, row.LevelDisplayText, 96f, FontStyles.Bold);

            if (row.ShowProgressBar)
            {
                CreateProgressBar(go.transform, row.ProgressFraction);
                CreateRowLabel(go.transform, row.PxpHintText, 110f, FontStyles.Normal);
            }
            else
            {
                CreateSpacer(go.transform, 140f);
                CreateSpacer(go.transform, 110f);
            }

            if (row.Eligible)
            {
                Color aptColor = row.Aptitude >= 1
                    ? RacialUiTheme.ActiveBadge
                    : row.Aptitude <= -1
                        ? new Color(0.75f, 0.35f, 0.32f, 1f)
                        : RacialUiTheme.MutedText;
                CreateRowLabel(go.transform, row.AptitudeDisplayText, 64f, FontStyles.Normal, aptColor);
            }
            else
            {
                CreateSpacer(go.transform, 64f);
            }

            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            ProficiencyKind captured = row.Kind;
            button.onClick.AddListener(() => _onSelect?.Invoke(captured));

            _rowObjects.Add(go);
        }

        static void CreateRowLabel(
            Transform parent,
            string text,
            float width,
            FontStyles style,
            Color? color = null)
        {
            var go = new GameObject("Cell", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;

            TextMeshProUGUI tmp = RacialUiTheme.CreateText(
                go.transform,
                "Label",
                text,
                15f,
                TextAlignmentOptions.MidlineLeft,
                style);
            RacialUiTheme.Stretch(tmp.rectTransform);
            tmp.color = color ?? RacialUiTheme.BodyText;
        }

        static void CreateSpacer(Transform parent, float width)
        {
            var go = new GameObject("Spacer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;
        }

        static void CreateProgressBar(Transform parent, float fill)
        {
            var go = new GameObject("Progress", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 140f;
            le.preferredWidth = 140f;
            le.minHeight = 8f;
            le.preferredHeight = 8f;

            Image track = go.GetComponent<Image>();
            track.sprite = RacialUiTheme.PlaceholderSprite;
            track.color = new Color(0.1f, 0.11f, 0.12f, 1f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            Image fillImage = fillGo.GetComponent<Image>();
            fillImage.sprite = RacialUiTheme.PlaceholderSprite;
            fillImage.color = RacialUiTheme.ActiveAccent;
        }
    }
}
