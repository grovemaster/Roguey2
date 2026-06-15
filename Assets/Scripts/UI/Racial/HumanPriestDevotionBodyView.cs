using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.UI.Hotbar;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public sealed class HumanPriestDevotionBodyView : MonoBehaviour
    {
        TextMeshProUGUI _statusLineText;
        TextMeshProUGUI _penanceLineText;
        RectTransform _preparedListRoot;
        RectTransform _libraryListRoot;
        TextMeshProUGUI _preparedEmptyText;
        TextMeshProUGUI _libraryEmptyText;
        RectTransform _conductListRoot;
        TextMeshProUGUI _conductEmptyText;
        Image _detailIcon;
        TextMeshProUGUI _detailTitleText;
        TextMeshProUGUI _detailBodyText;
        TextMeshProUGUI _detailErrorText;
        TextMeshProUGUI _detailFootnoteText;
        Button _prepareButton;
        Button _unprepareButton;
        Button _addToHotbarButton;
        TextMeshProUGUI _prepareButtonLabel;
        TextMeshProUGUI _unprepareButtonLabel;
        TextMeshProUGUI _addToHotbarButtonLabel;

        BaseActor _focusedActor;
        string _selectedInvocationId = string.Empty;
        HumanPriestDevotionBodyViewModel _viewModel;

        readonly List<GameObject> _preparedRowObjects = new List<GameObject>();
        readonly List<GameObject> _libraryRowObjects = new List<GameObject>();
        readonly List<GameObject> _conductRowObjects = new List<GameObject>();

        public static HumanPriestDevotionBodyView Create(Transform parent)
        {
            Transform existing = parent.Find("HumanPriestDevotionBodyContent");
            if (existing != null)
                UnityEngine.Object.Destroy(existing.gameObject);

            var root = new GameObject("HumanPriestDevotionBodyContent", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var view = root.AddComponent<HumanPriestDevotionBodyView>();
            view.BuildStatusStrip(root.transform);
            view.BuildColumns(root.transform);
            view.BuildConductLedger(root.transform);
            view.BuildDetailPane(root.transform);
            return view;
        }

        void BuildStatusStrip(Transform parent)
        {
            var strip = new GameObject("StatusStrip", typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(parent, false);
            Image bg = strip.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = RacialUiTheme.HumanMageBudgetBackground;

            var le = strip.AddComponent<LayoutElement>();
            le.minHeight = 52f;
            le.preferredHeight = 52f;

            var layout = strip.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _statusLineText = RacialUiTheme.CreateText(
                strip.transform,
                "StatusLine",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            _statusLineText.color = RacialUiTheme.HumanMageSectionAccent;

            _penanceLineText = RacialUiTheme.CreateText(
                strip.transform,
                "PenanceLine",
                string.Empty,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            _penanceLineText.color = RacialUiTheme.MutedText;
            _penanceLineText.gameObject.SetActive(false);
        }

        void BuildColumns(Transform parent)
        {
            var middle = new GameObject("DevotionColumns", typeof(RectTransform));
            middle.transform.SetParent(parent, false);
            var middleLe = middle.AddComponent<LayoutElement>();
            middleLe.flexibleHeight = 1f;
            middleLe.minHeight = 240f;

            var layout = middle.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            BuildColumn(
                middle.transform,
                "PreparedColumn",
                "PREPARED DEVOTIONS",
                "Ready to assign on the hotbar",
                flexibleWidth: 0.45f,
                out _preparedListRoot,
                out _preparedEmptyText);

            BuildColumn(
                middle.transform,
                "LibraryColumn",
                "COVENANT LIBRARY",
                "Invocations granted by your patron",
                flexibleWidth: 0.55f,
                out _libraryListRoot,
                out _libraryEmptyText);
        }

        void BuildColumn(
            Transform parent,
            string name,
            string header,
            string subtitle,
            float flexibleWidth,
            out RectTransform listRoot,
            out TextMeshProUGUI emptyText)
        {
            var column = new GameObject(name, typeof(RectTransform));
            column.transform.SetParent(parent, false);
            var columnLe = column.AddComponent<LayoutElement>();
            columnLe.flexibleWidth = flexibleWidth;
            columnLe.minWidth = 240f;

            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI headerText = RacialUiTheme.CreateText(
                column.transform,
                "Header",
                header,
                RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            headerText.color = RacialUiTheme.HumanMageSectionAccent;
            headerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            TextMeshProUGUI subtitleText = RacialUiTheme.CreateText(
                column.transform,
                "Subtitle",
                subtitle,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            subtitleText.color = RacialUiTheme.MutedText;
            subtitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var scrollHost = new GameObject("Scroll", typeof(RectTransform));
            scrollHost.transform.SetParent(column.transform, false);
            var scrollLe = scrollHost.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 160f;

            ScrollRect scroll = scrollHost.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollHost.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            viewport.GetComponent<Image>().color = RacialUiTheme.HumanMageColumnBackground;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            listRoot = content.GetComponent<RectTransform>();
            listRoot.anchorMin = new Vector2(0f, 1f);
            listRoot.anchorMax = new Vector2(1f, 1f);
            listRoot.pivot = new Vector2(0.5f, 1f);
            listRoot.sizeDelta = Vector2.zero;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 0f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = listRoot;

            emptyText = RacialUiTheme.CreateText(
                column.transform,
                "EmptyText",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            emptyText.color = RacialUiTheme.MutedText;
            emptyText.gameObject.SetActive(false);
        }

        void BuildConductLedger(Transform parent)
        {
            var root = new GameObject("ConductLedger", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Image bg = root.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = RacialUiTheme.HumanMageBudgetBackground;

            var le = root.AddComponent<LayoutElement>();
            le.minHeight = 72f;
            le.preferredHeight = 88f;
            le.flexibleHeight = 0f;

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI header = RacialUiTheme.CreateText(
                root.transform,
                "Header",
                "CONDUCT LEDGER",
                RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            header.color = RacialUiTheme.HumanMageSectionAccent;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            var scrollHost = new GameObject("Scroll", typeof(RectTransform));
            scrollHost.transform.SetParent(root.transform, false);
            var scrollLe = scrollHost.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 40f;

            ScrollRect scroll = scrollHost.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollHost.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            viewport.GetComponent<Image>().color = RacialUiTheme.HumanMageColumnBackground;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            _conductListRoot = content.GetComponent<RectTransform>();
            _conductListRoot.anchorMin = new Vector2(0f, 1f);
            _conductListRoot.anchorMax = new Vector2(1f, 1f);
            _conductListRoot.pivot = new Vector2(0.5f, 1f);
            _conductListRoot.sizeDelta = Vector2.zero;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = _conductListRoot;

            _conductEmptyText = RacialUiTheme.CreateText(
                root.transform,
                "EmptyText",
                "No recent conduct events.",
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            _conductEmptyText.color = RacialUiTheme.MutedText;
            _conductEmptyText.gameObject.SetActive(false);
        }

        void BuildDetailPane(Transform parent)
        {
            var root = new GameObject("DetailPane", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Image bg = root.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = new Color(0.12f, 0.125f, 0.135f, 0.92f);

            var le = root.AddComponent<LayoutElement>();
            le.minHeight = 220f;
            le.preferredHeight = 260f;
            le.flexibleHeight = 0f;

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI section = RacialUiTheme.CreateText(
                root.transform,
                "Section",
                "DETAILS",
                RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            section.color = RacialUiTheme.HumanMageSectionAccent;
            section.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            var mainRow = new GameObject("MainRow", typeof(RectTransform));
            mainRow.transform.SetParent(root.transform, false);
            var mainRowLe = mainRow.AddComponent<LayoutElement>();
            mainRowLe.flexibleHeight = 1f;
            mainRowLe.minHeight = 120f;

            var mainRowLayout = mainRow.AddComponent<HorizontalLayoutGroup>();
            mainRowLayout.spacing = 16f;
            mainRowLayout.childAlignment = TextAnchor.UpperLeft;
            mainRowLayout.childControlWidth = true;
            mainRowLayout.childControlHeight = true;
            mainRowLayout.childForceExpandWidth = false;
            mainRowLayout.childForceExpandHeight = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(mainRow.transform, false);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.minWidth = iconLe.preferredWidth = 80f;
            iconLe.minHeight = iconLe.preferredHeight = 80f;
            _detailIcon = iconGo.GetComponent<Image>();
            _detailIcon.preserveAspect = true;
            _detailIcon.sprite = RacialUiTheme.HumanMageSpellEmblemSprite;

            var textStack = new GameObject("TextStack", typeof(RectTransform));
            textStack.transform.SetParent(mainRow.transform, false);
            var textStackLe = textStack.AddComponent<LayoutElement>();
            textStackLe.flexibleWidth = 1f;
            textStackLe.minWidth = 320f;

            var textLayout = textStack.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 6f;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            _detailTitleText = RacialUiTheme.CreateText(
                textStack.transform,
                "Title",
                string.Empty,
                RacialUiTheme.CardTitleFontSize,
                TextAlignmentOptions.TopLeft,
                FontStyles.Bold);

            _detailBodyText = RacialUiTheme.CreateText(
                textStack.transform,
                "Body",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.TopLeft);
            _detailBodyText.gameObject.AddComponent<LayoutElement>().minHeight = 72f;

            var actionRow = new GameObject("ActionRow", typeof(RectTransform));
            actionRow.transform.SetParent(root.transform, false);
            actionRow.AddComponent<LayoutElement>().preferredHeight = 40f;
            var actionLayout = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 12f;
            actionLayout.childAlignment = TextAnchor.MiddleLeft;
            actionLayout.childControlWidth = false;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;

            _prepareButton = CreateActionButton(actionRow.transform, "PrepareButton", "Prepare", OnPrepareClicked);
            _prepareButtonLabel = _prepareButton.GetComponentInChildren<TextMeshProUGUI>();
            _unprepareButton = CreateActionButton(actionRow.transform, "UnprepareButton", "Unprepare", OnUnprepareClicked);
            _unprepareButtonLabel = _unprepareButton.GetComponentInChildren<TextMeshProUGUI>();
            _addToHotbarButton = CreateActionButton(
                actionRow.transform,
                "AddToHotbarButton",
                "Add to hotbar",
                OnAddToHotbarClicked);
            _addToHotbarButtonLabel = _addToHotbarButton.GetComponentInChildren<TextMeshProUGUI>();
            var addLe = _addToHotbarButton.gameObject.GetComponent<LayoutElement>();
            addLe.minWidth = 150f;
            addLe.preferredWidth = 150f;

            _detailErrorText = RacialUiTheme.CreateText(
                root.transform,
                "Error",
                string.Empty,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft);
            _detailErrorText.color = new Color(0.92f, 0.42f, 0.38f, 1f);
            _detailErrorText.gameObject.SetActive(false);

            _detailFootnoteText = RacialUiTheme.CreateText(
                root.transform,
                "Footnote",
                new HumanPriestDevotionDetailModel().HotbarFootnote,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            _detailFootnoteText.color = RacialUiTheme.MutedText;
        }

        static Button CreateActionButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 120f;
            le.preferredWidth = 120f;
            le.minHeight = 36f;

            Image image = go.GetComponent<Image>();
            image.sprite = RacialUiTheme.PlaceholderSprite;
            image.color = RacialUiTheme.HumanMageActionButtonBackground;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            TextMeshProUGUI text = RacialUiTheme.CreateText(
                go.transform,
                "Label",
                label,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            RacialUiTheme.Stretch(text.rectTransform);
            text.raycastTarget = false;
            return button;
        }

        public void Rebuild(BaseActor actor, string selectedInvocationId)
        {
            _focusedActor = actor;
            if (!string.IsNullOrWhiteSpace(selectedInvocationId))
                _selectedInvocationId = selectedInvocationId.Trim();

            _viewModel = HumanPriestDevotionBodyViewModel.Build(_focusedActor, _selectedInvocationId);
            _selectedInvocationId = _viewModel.SelectedInvocationId;
            RefreshViews();
        }

        void SelectInvocation(string invocationId)
        {
            _selectedInvocationId = invocationId?.Trim() ?? string.Empty;
            if (_focusedActor == null)
                return;

            _viewModel = HumanPriestDevotionBodyViewModel.Build(_focusedActor, _selectedInvocationId);
            _selectedInvocationId = _viewModel.SelectedInvocationId;
            RefreshViews();
        }

        void RefreshViews()
        {
            if (_viewModel == null)
                return;

            _statusLineText.text = string.IsNullOrWhiteSpace(_viewModel.StatusLine)
                ? HumanPriestDevotionBodyViewModel.MissingRuntimeMessage
                : _viewModel.StatusLine;

            bool hasPenance = !string.IsNullOrWhiteSpace(_viewModel.PenanceLine);
            _penanceLineText.text = _viewModel.PenanceLine ?? string.Empty;
            _penanceLineText.gameObject.SetActive(hasPenance);

            RebuildColumn(
                _preparedListRoot,
                _preparedRowObjects,
                _preparedEmptyText,
                _viewModel.PreparedRows,
                HumanPriestDevotionBodyViewModel.PreparedEmptyMessage);
            RebuildColumn(
                _libraryListRoot,
                _libraryRowObjects,
                _libraryEmptyText,
                _viewModel.LibraryRows,
                HumanPriestDevotionBodyViewModel.LibraryEmptyMessage);
            RebuildConductLedger(_viewModel.ConductRows);
            PopulateDetailPane(_viewModel.Detail);
        }

        void RebuildColumn(
            RectTransform listRoot,
            List<GameObject> rowObjects,
            TextMeshProUGUI emptyText,
            IReadOnlyList<HumanPriestDevotionRowModel> rows,
            string emptyMessage)
        {
            ClearRows(rowObjects);

            if (rows == null || rows.Count == 0)
            {
                emptyText.text = emptyMessage;
                emptyText.gameObject.SetActive(true);
                return;
            }

            emptyText.gameObject.SetActive(false);
            for (int i = 0; i < rows.Count; i++)
                rowObjects.Add(CreateInvocationRow(listRoot, rows[i]));
        }

        void RebuildConductLedger(IReadOnlyList<HumanPriestConductRowModel> rows)
        {
            ClearRows(_conductRowObjects);

            if (rows == null || rows.Count == 0)
            {
                _conductEmptyText.gameObject.SetActive(true);
                return;
            }

            _conductEmptyText.gameObject.SetActive(false);
            for (int i = 0; i < rows.Count; i++)
                _conductRowObjects.Add(CreateConductRow(_conductListRoot, rows[i]));
        }

        void ClearRows(List<GameObject> rowObjects)
        {
            for (int i = 0; i < rowObjects.Count; i++)
            {
                if (rowObjects[i] != null)
                    Destroy(rowObjects[i]);
            }

            rowObjects.Clear();
        }

        GameObject CreateInvocationRow(RectTransform parent, HumanPriestDevotionRowModel row)
        {
            bool selected = string.Equals(
                row.InvocationId,
                _selectedInvocationId,
                StringComparison.OrdinalIgnoreCase);
            bool locked = row.IsLocked;

            var go = new GameObject(
                $"InvocationRow_{row.InvocationId}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 64f;
            le.preferredHeight = 64f;

            Image bg = go.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            Color rowBackground = selected
                ? new Color(0.18f, 0.16f, 0.28f, 0.96f)
                : RacialUiTheme.HumanMageRowBackground;
            if (locked)
                rowBackground = DimColor(rowBackground, 0.55f);
            bg.color = rowBackground;

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = selected
                ? RacialUiTheme.FocusBorder
                : DimColor(RacialUiTheme.HumanMageRowBorder, locked ? 0.65f : 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            if (selected)
            {
                var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
                accent.transform.SetParent(go.transform, false);
                RectTransform accentRt = accent.GetComponent<RectTransform>();
                accentRt.anchorMin = new Vector2(0f, 0f);
                accentRt.anchorMax = new Vector2(0f, 1f);
                accentRt.pivot = new Vector2(0f, 0.5f);
                accentRt.sizeDelta = new Vector2(3f, 0f);
                accentRt.anchoredPosition = Vector2.zero;
                accent.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
                accent.GetComponent<Image>().color = RacialUiTheme.HumanMageSectionAccent;
            }

            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            string capturedId = row.InvocationId;
            button.onClick.AddListener(() => SelectInvocation(capturedId));

            var rowLayout = go.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(selected ? 10 : 8, 8, 8, 8);
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.minWidth = iconLe.preferredWidth = 44f;
            iconLe.minHeight = iconLe.preferredHeight = 44f;
            Image icon = iconGo.GetComponent<Image>();
            icon.sprite = ResolveInvocationIcon(row.Invocation);
            icon.preserveAspect = true;
            if (locked)
                icon.color = DimColor(Color.white, 0.55f);

            var textStack = new GameObject("TextStack", typeof(RectTransform));
            textStack.transform.SetParent(go.transform, false);
            var textStackLe = textStack.AddComponent<LayoutElement>();
            textStackLe.flexibleWidth = 1f;

            var textLayout = textStack.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2f;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            TextMeshProUGUI title = RacialUiTheme.CreateText(
                textStack.transform,
                "Title",
                row.Title,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            title.raycastTarget = false;
            if (locked)
                title.color = DimColor(RacialUiTheme.BodyText, 0.55f);

            TextMeshProUGUI subtitle = RacialUiTheme.CreateText(
                textStack.transform,
                "Subtitle",
                row.Subtitle,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft);
            subtitle.color = locked
                ? DimColor(RacialUiTheme.MutedText, 0.7f)
                : RacialUiTheme.MutedText;
            subtitle.raycastTarget = false;

            if (row.ShowPreparedBadge && row.IsPrepared)
            {
                var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                badgeGo.transform.SetParent(go.transform, false);
                var badgeLe = badgeGo.AddComponent<LayoutElement>();
                badgeLe.minWidth = badgeLe.preferredWidth = 72f;
                badgeLe.minHeight = 24f;
                Image badgeBg = badgeGo.GetComponent<Image>();
                badgeBg.sprite = RacialUiTheme.PlaceholderSprite;
                badgeBg.color = new Color(0.18f, 0.34f, 0.52f, locked ? 0.55f : 0.95f);

                TextMeshProUGUI badgeText = RacialUiTheme.CreateText(
                    badgeGo.transform,
                    "Label",
                    "Prepared",
                    RacialUiTheme.CardBadgeFontSize,
                    TextAlignmentOptions.Center,
                    FontStyles.Bold);
                badgeText.color = RacialUiTheme.HumanMageSecondaryAccent;
                RacialUiTheme.Stretch(badgeText.rectTransform);
                badgeText.raycastTarget = false;
            }

            return go;
        }

        static GameObject CreateConductRow(RectTransform parent, HumanPriestConductRowModel row)
        {
            var go = new GameObject("ConductRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 18f;

            string deltaPrefix = row.PietyDelta > 0
                ? $"+{row.PietyDelta}"
                : row.PietyDelta.ToString();
            TextMeshProUGUI text = RacialUiTheme.CreateText(
                go.transform,
                "Line",
                $"{deltaPrefix} piety — {row.Message}",
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft);
            text.color = row.PietyDelta >= 0
                ? new Color(0.62f, 0.78f, 0.66f, 1f)
                : new Color(0.88f, 0.58f, 0.52f, 1f);
            RacialUiTheme.Stretch(text.rectTransform);
            return go;
        }

        static Color DimColor(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        static Sprite ResolveInvocationIcon(PriestInvocationDefinition invocation)
        {
            if (invocation?.ability != null && invocation.ability.hotbarIcon != null)
                return invocation.ability.hotbarIcon;

            return RacialUiTheme.HumanMageSpellEmblemSprite;
        }

        void PopulateDetailPane(HumanPriestDevotionDetailModel detail)
        {
            if (detail == null)
                return;

            PriestInvocationDefinition invocation = FindInvocationDefinition(detail.InvocationId);
            _detailIcon.sprite = invocation != null
                ? ResolveInvocationIcon(invocation)
                : RacialUiTheme.HumanMageSpellEmblemSprite;
            _detailTitleText.text = string.IsNullOrWhiteSpace(detail.Title)
                ? "Select an invocation."
                : detail.Title;

            if (string.IsNullOrWhiteSpace(detail.InvocationId))
            {
                _detailBodyText.text = "Select an invocation row to read its details.";
                _prepareButton.gameObject.SetActive(false);
                _unprepareButton.gameObject.SetActive(false);
                _addToHotbarButton.gameObject.SetActive(false);
                _detailErrorText.gameObject.SetActive(false);
                _detailFootnoteText.text = detail.HotbarFootnote;
                return;
            }

            _detailBodyText.text = $"{detail.Description}\n\n{detail.CostLine}";

            bool showActions = _viewModel.EditMode == HumanPriestDevotionEditMode.Edit;
            _prepareButton.gameObject.SetActive(showActions && detail.ShowPrepareButton);
            _unprepareButton.gameObject.SetActive(showActions && detail.ShowUnprepareButton);
            _addToHotbarButton.gameObject.SetActive(showActions && detail.ShowAddToHotbarButton);

            if (detail.ShowPrepareButton)
            {
                _prepareButton.interactable = detail.PrepareEnabled;
                _prepareButtonLabel.text = "Prepare";
            }

            if (detail.ShowUnprepareButton)
                _unprepareButtonLabel.text = "Unprepare";

            if (detail.ShowAddToHotbarButton)
            {
                _addToHotbarButton.interactable = detail.AddToHotbarEnabled;
                _addToHotbarButtonLabel.text = "Add to hotbar";
            }

            string error = detail.PrepareDisabledReason;
            if (string.IsNullOrWhiteSpace(error))
                error = detail.AddToHotbarDisabledReason;

            if (!string.IsNullOrWhiteSpace(error))
            {
                _detailErrorText.text = error;
                _detailErrorText.gameObject.SetActive(true);
            }
            else
            {
                _detailErrorText.gameObject.SetActive(false);
            }

            _detailFootnoteText.text = detail.HotbarFootnote;
        }

        PriestInvocationDefinition FindInvocationDefinition(string invocationId)
        {
            if (_viewModel == null || string.IsNullOrWhiteSpace(invocationId))
                return null;

            PriestInvocationDefinition invocation = FindInRows(_viewModel.PreparedRows, invocationId);
            return invocation ?? FindInRows(_viewModel.LibraryRows, invocationId);
        }

        static PriestInvocationDefinition FindInRows(
            IReadOnlyList<HumanPriestDevotionRowModel> rows,
            string invocationId)
        {
            if (rows == null)
                return null;

            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i]?.InvocationId, invocationId, StringComparison.OrdinalIgnoreCase))
                    return rows[i].Invocation;
            }

            return null;
        }

        void OnPrepareClicked() => TryApplyLoadoutChange(prepare: true);

        void OnUnprepareClicked() => TryApplyLoadoutChange(prepare: false);

        void OnAddToHotbarClicked()
        {
            if (_focusedActor == null || string.IsNullOrWhiteSpace(_selectedInvocationId))
                return;

            if (!HumanPriestHotbarSync.TryAssignEquippedInvocationToHotbar(
                    _focusedActor,
                    _selectedInvocationId,
                    out string failureReason))
            {
                _detailErrorText.text = failureReason ?? "Could not add invocation to hotbar.";
                _detailErrorText.gameObject.SetActive(true);
                return;
            }

            AbilityHotbarUI.Instance?.RefreshAll();
            Rebuild(_focusedActor, _selectedInvocationId);
        }

        void TryApplyLoadoutChange(bool prepare)
        {
            if (_focusedActor == null || string.IsNullOrWhiteSpace(_selectedInvocationId))
                return;

            string failureReason;
            bool ok = prepare
                ? HumanPriestDevotionLoadoutService.TryEquip(_focusedActor, _selectedInvocationId, out failureReason)
                : HumanPriestDevotionLoadoutService.TryUnequip(_focusedActor, _selectedInvocationId, out failureReason);

            if (!ok)
            {
                _detailErrorText.text = failureReason ?? "Could not update prepared devotions.";
                _detailErrorText.gameObject.SetActive(true);
                return;
            }

            AbilityHotbarUI.Instance?.RefreshAll();
            Rebuild(_focusedActor, _selectedInvocationId);
        }
    }
}
