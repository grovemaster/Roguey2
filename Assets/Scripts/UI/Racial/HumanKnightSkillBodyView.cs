using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.UI.Hotbar;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public sealed class HumanKnightSkillBodyView : MonoBehaviour
    {
        TextMeshProUGUI _summaryLineText;
        TextMeshProUGUI _summaryFootnoteText;
        RectTransform _treeListRoot;
        ScrollRect _treeScroll;
        TextMeshProUGUI _treeEmptyText;
        Image _detailIcon;
        TextMeshProUGUI _detailTitleText;
        TextMeshProUGUI _detailBodyText;
        TextMeshProUGUI _detailErrorText;
        TextMeshProUGUI _detailFootnoteText;
        Button _spendButton;
        Button _addToHotbarButton;
        TextMeshProUGUI _spendButtonLabel;
        TextMeshProUGUI _addToHotbarButtonLabel;

        BaseActor _focusedActor;
        string _selectedNodeId = string.Empty;
        HumanKnightSkillBodyViewModel _viewModel;

        readonly List<GameObject> _rowObjects = new List<GameObject>();

        public static HumanKnightSkillBodyView Create(Transform parent)
        {
            Transform existing = parent.Find("HumanKnightSkillBodyContent");
            if (existing != null)
                UnityEngine.Object.Destroy(existing.gameObject);

            var root = new GameObject("HumanKnightSkillBodyContent", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var view = root.AddComponent<HumanKnightSkillBodyView>();
            view.BuildSummaryStrip(root.transform);
            view.BuildTreeList(root.transform);
            view.BuildDetailPane(root.transform);
            return view;
        }

        void BuildSummaryStrip(Transform parent)
        {
            var strip = new GameObject("SummaryStrip", typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(parent, false);
            Image bg = strip.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = RacialUiTheme.HumanKnightBudgetBackground;

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

            _summaryLineText = RacialUiTheme.CreateText(
                strip.transform,
                "SummaryLine",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            _summaryLineText.color = RacialUiTheme.HumanKnightSectionAccent;

            _summaryFootnoteText = RacialUiTheme.CreateText(
                strip.transform,
                "SummaryFootnote",
                HumanKnightSkillBodyViewModel.SummaryFootnote,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            _summaryFootnoteText.color = RacialUiTheme.MutedText;
        }

        void BuildTreeList(Transform parent)
        {
            var middle = new GameObject("TreeList", typeof(RectTransform));
            middle.transform.SetParent(parent, false);
            var middleLe = middle.AddComponent<LayoutElement>();
            middleLe.flexibleHeight = 1f;
            middleLe.minHeight = 280f;

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(middle.transform, false);
            RacialUiTheme.Stretch(scrollGo.GetComponent<RectTransform>());
            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.sprite = RacialUiTheme.PlaceholderSprite;
            scrollBg.color = RacialUiTheme.HumanKnightColumnBackground;

            _treeScroll = scrollGo.GetComponent<ScrollRect>();
            _treeScroll.horizontal = false;
            _treeScroll.vertical = true;
            _treeScroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            viewport.GetComponent<Image>().color = Color.white;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            _treeListRoot = content.GetComponent<RectTransform>();
            _treeListRoot.anchorMin = new Vector2(0f, 1f);
            _treeListRoot.anchorMax = new Vector2(1f, 1f);
            _treeListRoot.pivot = new Vector2(0.5f, 1f);
            _treeListRoot.sizeDelta = Vector2.zero;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(6, 6, 6, 6);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _treeScroll.viewport = viewport.GetComponent<RectTransform>();
            _treeScroll.content = _treeListRoot;

            _treeEmptyText = RacialUiTheme.CreateText(
                middle.transform,
                "EmptyText",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            _treeEmptyText.color = RacialUiTheme.MutedText;
            _treeEmptyText.gameObject.SetActive(false);
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
            section.color = RacialUiTheme.HumanKnightSectionAccent;
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
            _detailIcon.sprite = RacialUiTheme.HumanKnightSkillEmblemSprite;

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

            _spendButton = CreateActionButton(
                actionRow.transform,
                "SpendButton",
                "Spend skill point",
                OnSpendClicked);
            _spendButtonLabel = _spendButton.GetComponentInChildren<TextMeshProUGUI>();
            var spendLe = _spendButton.gameObject.GetComponent<LayoutElement>();
            spendLe.minWidth = 170f;
            spendLe.preferredWidth = 170f;

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
                new HumanKnightSkillDetailModel().HotbarFootnote,
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
            image.color = RacialUiTheme.HumanKnightActionButtonBackground;

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

        public void Rebuild(BaseActor knight, string selectedNodeId = null)
        {
            _focusedActor = knight;
            if (!string.IsNullOrWhiteSpace(selectedNodeId))
                _selectedNodeId = selectedNodeId.Trim();

            _viewModel = HumanKnightSkillBodyViewModel.Build(_focusedActor, _selectedNodeId);
            _selectedNodeId = _viewModel.SelectedNodeId;
            RefreshViews();
        }

        void SelectNode(string nodeId)
        {
            _selectedNodeId = nodeId?.Trim() ?? string.Empty;
            if (_focusedActor == null)
                return;

            _viewModel = HumanKnightSkillBodyViewModel.Build(_focusedActor, _selectedNodeId);
            _selectedNodeId = _viewModel.SelectedNodeId;
            RefreshViews();
        }

        void RefreshViews()
        {
            if (_viewModel == null)
                return;

            _summaryLineText.text = _viewModel.SummaryLine;
            RebuildTreeList();
            PopulateDetailPane(_viewModel.Detail);
        }

        void RebuildTreeList()
        {
            ClearRows();

            if (_viewModel.BranchSections == null || _viewModel.BranchSections.Count == 0)
            {
                _treeEmptyText.text = HumanKnightSkillBodyViewModel.TreeEmptyMessage;
                _treeEmptyText.gameObject.SetActive(true);
                return;
            }

            _treeEmptyText.gameObject.SetActive(false);
            for (int i = 0; i < _viewModel.BranchSections.Count; i++)
            {
                HumanKnightSkillBranchSectionModel section = _viewModel.BranchSections[i];
                if (section?.Rows == null || section.Rows.Count == 0)
                    continue;

                _rowObjects.Add(CreateBranchHeader(section.BranchHeader));
                for (int r = 0; r < section.Rows.Count; r++)
                    _rowObjects.Add(CreateSkillRow(section.Rows[r]));
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

        GameObject CreateBranchHeader(string header)
        {
            var go = new GameObject($"Branch_{header}", typeof(RectTransform));
            go.transform.SetParent(_treeListRoot, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 24f;
            le.preferredHeight = 24f;

            TextMeshProUGUI text = RacialUiTheme.CreateText(
                go.transform,
                "Label",
                header,
                RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            text.color = RacialUiTheme.HumanKnightSecondaryAccent;
            return go;
        }

        GameObject CreateSkillRow(HumanKnightSkillRowModel row)
        {
            bool selected = string.Equals(
                row.NodeId,
                _selectedNodeId,
                StringComparison.OrdinalIgnoreCase);

            var go = new GameObject(
                $"SkillRow_{row.NodeId}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            go.transform.SetParent(_treeListRoot, false);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 64f;
            le.preferredHeight = 64f;

            Image bg = go.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = selected
                ? new Color(0.22f, 0.18f, 0.12f, 0.96f)
                : RacialUiTheme.HumanKnightRowBackground;

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = selected ? RacialUiTheme.FocusBorder : RacialUiTheme.HumanKnightRowBorder;
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
                accent.GetComponent<Image>().color = RacialUiTheme.HumanKnightSectionAccent;
            }

            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            string capturedId = row.NodeId;
            button.onClick.AddListener(() => SelectNode(capturedId));

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
            icon.sprite = ResolveSkillIcon(row.Node);
            icon.preserveAspect = true;

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

            TextMeshProUGUI subtitle = RacialUiTheme.CreateText(
                textStack.transform,
                "Subtitle",
                BuildRowSubtitle(row),
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft);
            subtitle.color = RacialUiTheme.MutedText;
            subtitle.raycastTarget = false;

            if (row.ShowRankProficiencyBar)
                CreateMiniBar(textStack.transform, "RankBar", row.RankProficiencyFraction);

            if (row.ShowMastery)
                CreateMiniBar(textStack.transform, "MasteryBar", row.MasteryFraction);

            if (row.ShowActiveBadge)
                CreateBadge(go.transform, "ACTIVE", RacialUiTheme.HumanKnightSectionAccent);
            else if (row.ShowMaxBadge)
                CreateBadge(go.transform, "MAX", RacialUiTheme.ActiveBadge);
            else if (row.ShowLockedBadge)
                CreateBadge(go.transform, "LOCKED", RacialUiTheme.MutedText);

            return go;
        }

        static string BuildRowSubtitle(HumanKnightSkillRowModel row)
        {
            string rankPart = $"Rank {row.RankLabel}";
            if (!row.ShowMastery)
                return rankPart;

            return $"{rankPart} · {row.MasteryLabel}";
        }

        static void CreateMiniBar(Transform parent, string name, float fraction)
        {
            var barRoot = new GameObject(name, typeof(RectTransform), typeof(Image));
            barRoot.transform.SetParent(parent, false);
            var barLe = barRoot.AddComponent<LayoutElement>();
            barLe.minHeight = 6f;
            barLe.preferredHeight = 6f;

            Image barBg = barRoot.GetComponent<Image>();
            barBg.sprite = RacialUiTheme.PlaceholderSprite;
            barBg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(barRoot.transform, false);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillGo.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            fillGo.GetComponent<Image>().color = RacialUiTheme.HumanKnightSecondaryAccent;
        }

        static void CreateBadge(Transform parent, string label, Color color)
        {
            var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(parent, false);
            var badgeLe = badgeGo.AddComponent<LayoutElement>();
            badgeLe.minWidth = badgeLe.preferredWidth = 72f;
            badgeLe.minHeight = 24f;
            Image badgeBg = badgeGo.GetComponent<Image>();
            badgeBg.sprite = RacialUiTheme.PlaceholderSprite;
            badgeBg.color = new Color(0.18f, 0.16f, 0.12f, 0.95f);

            TextMeshProUGUI badgeText = RacialUiTheme.CreateText(
                badgeGo.transform,
                "Label",
                label,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            badgeText.color = color;
            RacialUiTheme.Stretch(badgeText.rectTransform);
            badgeText.raycastTarget = false;
        }

        static Sprite ResolveSkillIcon(HumanClassSkillTreeNodeData node)
        {
            AbilityAction ability = node?.ResolveActiveAbility();
            if (ability != null && ability.hotbarIcon != null)
                return ability.hotbarIcon;

            return RacialUiTheme.HumanKnightSkillEmblemSprite;
        }

        void PopulateDetailPane(HumanKnightSkillDetailModel detail)
        {
            if (detail == null)
                return;

            HumanClassSkillTreeNodeData node = FindNode(detail.NodeId);
            _detailIcon.sprite = node != null ? ResolveSkillIcon(node) : RacialUiTheme.HumanKnightSkillEmblemSprite;
            _detailTitleText.text = string.IsNullOrWhiteSpace(detail.Title) ? "Select a skill." : detail.Title;

            if (string.IsNullOrWhiteSpace(detail.NodeId))
            {
                _detailBodyText.text = "Select a skill row to read its details.";
                _spendButton.gameObject.SetActive(false);
                _addToHotbarButton.gameObject.SetActive(false);
                _detailErrorText.gameObject.SetActive(false);
                _detailFootnoteText.text = detail.HotbarFootnote;
                return;
            }

            var lines = new List<string> { detail.Description, detail.RankLine };
            if (!string.IsNullOrWhiteSpace(detail.ProficiencyLine))
                lines.Add(detail.ProficiencyLine);
            if (!string.IsNullOrWhiteSpace(detail.MasteryLine))
                lines.Add(detail.MasteryLine);
            if (!string.IsNullOrWhiteSpace(detail.GateReason))
                lines.Add(detail.GateReason);

            _detailBodyText.text = string.Join("\n\n", lines);

            bool showActions = _viewModel.EditMode == HumanKnightSkillEditMode.Edit;
            _spendButton.gameObject.SetActive(showActions && detail.ShowSpendButton);
            _addToHotbarButton.gameObject.SetActive(showActions && detail.ShowAddToHotbarButton);

            if (detail.ShowSpendButton)
            {
                _spendButton.interactable = detail.SpendEnabled;
                _spendButtonLabel.text = "Spend skill point";
            }

            if (detail.ShowAddToHotbarButton)
            {
                _addToHotbarButton.interactable = detail.AddToHotbarEnabled;
                _addToHotbarButtonLabel.text = "Add to hotbar";
            }

            string error = detail.SpendDisabledReason;
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

            _detailFootnoteText.text =
                $"{detail.HotbarFootnote}\n{detail.ProficienciesFootnote}";
        }

        HumanClassSkillTreeNodeData FindNode(string nodeId)
        {
            if (_viewModel?.BranchSections == null || string.IsNullOrWhiteSpace(nodeId))
                return null;

            for (int i = 0; i < _viewModel.BranchSections.Count; i++)
            {
                HumanKnightSkillBranchSectionModel section = _viewModel.BranchSections[i];
                if (section?.Rows == null)
                    continue;

                for (int r = 0; r < section.Rows.Count; r++)
                {
                    if (string.Equals(section.Rows[r]?.NodeId, nodeId, StringComparison.OrdinalIgnoreCase))
                        return section.Rows[r].Node;
                }
            }

            return null;
        }

        void OnSpendClicked()
        {
            if (_focusedActor == null || string.IsNullOrWhiteSpace(_selectedNodeId))
                return;

            if (!HumanKnightSkillTreeService.TrySpendPoint(
                    _focusedActor,
                    _selectedNodeId,
                    out string failureReason))
            {
                _detailErrorText.text = failureReason ?? "Could not spend skill point.";
                _detailErrorText.gameObject.SetActive(true);
                return;
            }

            AbilityHotbarUI.Instance?.RefreshAll();
            Rebuild(_focusedActor, _selectedNodeId);
        }

        void OnAddToHotbarClicked()
        {
            if (_focusedActor == null || string.IsNullOrWhiteSpace(_selectedNodeId))
                return;

            HumanClassSkillTreeNodeData node = FindNode(_selectedNodeId);
            if (node == null)
                return;

            int abilityIndex = Mathf.Clamp(node.activeAbilityIndex, 0, node.activeAbilities.Count - 1);
            if (!HumanKnightHotbarSync.TryAssignActiveToHotbar(
                    _focusedActor,
                    _selectedNodeId,
                    abilityIndex,
                    out string failureReason))
            {
                _detailErrorText.text = failureReason ?? "Could not add skill to hotbar.";
                _detailErrorText.gameObject.SetActive(true);
                return;
            }

            AbilityHotbarUI.Instance?.RefreshAll();
            Rebuild(_focusedActor, _selectedNodeId);
        }
    }
}
