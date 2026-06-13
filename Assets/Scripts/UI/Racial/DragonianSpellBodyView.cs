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
    public sealed class DragonianSpellBodyView : MonoBehaviour
    {
        TextMeshProUGUI _budgetLineText;
        TextMeshProUGUI _budgetFootnoteText;
        RectTransform _memorizedListRoot;
        RectTransform _knownListRoot;
        TextMeshProUGUI _memorizedEmptyText;
        TextMeshProUGUI _knownEmptyText;
        Image _detailIcon;
        TextMeshProUGUI _detailTitleText;
        TextMeshProUGUI _detailBodyText;
        TextMeshProUGUI _detailErrorText;
        TextMeshProUGUI _detailFootnoteText;
        Button _equipButton;
        Button _unequipButton;
        TextMeshProUGUI _equipButtonLabel;
        TextMeshProUGUI _unequipButtonLabel;

        BaseActor _focusedActor;
        string _selectedSpellId = string.Empty;
        DragonianSpellBodyViewModel _viewModel;

        readonly List<GameObject> _memorizedRowObjects = new List<GameObject>();
        readonly List<GameObject> _knownRowObjects = new List<GameObject>();

        public static DragonianSpellBodyView Create(Transform parent)
        {
            Transform existing = parent.Find("DragonianSpellBodyContent");
            if (existing != null)
                UnityEngine.Object.Destroy(existing.gameObject);

            var root = new GameObject("DragonianSpellBodyContent", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var view = root.AddComponent<DragonianSpellBodyView>();
            view.BuildBudgetStrip(root.transform);
            view.BuildColumns(root.transform);
            view.BuildDetailPane(root.transform);
            return view;
        }

        void BuildBudgetStrip(Transform parent)
        {
            var strip = new GameObject("BudgetStrip", typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(parent, false);
            Image bg = strip.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = RacialUiTheme.DragonianBudgetBackground;

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

            _budgetLineText = RacialUiTheme.CreateText(
                strip.transform,
                "BudgetLine",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            _budgetLineText.color = RacialUiTheme.DragonianSectionAccent;

            _budgetFootnoteText = RacialUiTheme.CreateText(
                strip.transform,
                "BudgetFootnote",
                DragonianSpellBodyViewModel.BudgetFootnote,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            _budgetFootnoteText.color = RacialUiTheme.MutedText;
        }

        void BuildColumns(Transform parent)
        {
            var middle = new GameObject("SpellColumns", typeof(RectTransform));
            middle.transform.SetParent(parent, false);
            var middleLe = middle.AddComponent<LayoutElement>();
            middleLe.flexibleHeight = 1f;
            middleLe.minHeight = 280f;

            var layout = middle.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            BuildColumn(
                middle.transform,
                "MemorizedColumn",
                "EQUIPPED WORD-FORMS",
                "Ready to assign on the hotbar",
                flexibleWidth: 0.45f,
                out _memorizedListRoot,
                out _memorizedEmptyText);

            BuildColumn(
                middle.transform,
                "KnownColumn",
                "ALL WORD-FORMS",
                "Learned draconic techniques",
                flexibleWidth: 0.55f,
                out _knownListRoot,
                out _knownEmptyText);
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
            headerText.color = RacialUiTheme.DragonianSectionAccent;
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
            scrollLe.minHeight = 180f;

            ScrollRect scroll = scrollHost.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollHost.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            viewport.GetComponent<Image>().color = RacialUiTheme.DragonianColumnBackground;
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
            section.color = RacialUiTheme.DragonianSectionAccent;
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
            _detailIcon.sprite = RacialUiTheme.DragonianSpellEmblemSprite;

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

            _equipButton = CreateActionButton(actionRow.transform, "EquipButton", "Equip", OnEquipClicked);
            _equipButtonLabel = _equipButton.GetComponentInChildren<TextMeshProUGUI>();
            _unequipButton = CreateActionButton(actionRow.transform, "UnequipButton", "Unequip", OnUnequipClicked);
            _unequipButtonLabel = _unequipButton.GetComponentInChildren<TextMeshProUGUI>();

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
                new DragonianSpellDetailModel().HotbarFootnote,
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
            image.color = RacialUiTheme.DragonianActionButtonBackground;

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

        public void Rebuild(BaseActor dragonian, string selectedSpellId = null)
        {
            _focusedActor = dragonian;
            if (!string.IsNullOrWhiteSpace(selectedSpellId))
                _selectedSpellId = selectedSpellId.Trim();

            _viewModel = DragonianSpellBodyViewModel.Build(_focusedActor, _selectedSpellId);
            _selectedSpellId = _viewModel.SelectedSpellId;
            RefreshViews();
        }

        void SelectSpell(string spellId)
        {
            _selectedSpellId = spellId?.Trim() ?? string.Empty;
            if (_focusedActor == null)
                return;

            _viewModel = DragonianSpellBodyViewModel.Build(_focusedActor, _selectedSpellId);
            _selectedSpellId = _viewModel.SelectedSpellId;
            RefreshViews();
        }

        void RefreshViews()
        {
            if (_viewModel == null)
                return;

            _budgetLineText.text = _viewModel.BudgetLine;
            RebuildColumn(
                _memorizedListRoot,
                _memorizedRowObjects,
                _memorizedEmptyText,
                _viewModel.MemorizedRows,
                DragonianSpellBodyViewModel.MemorizedEmptyMessage);
            RebuildColumn(
                _knownListRoot,
                _knownRowObjects,
                _knownEmptyText,
                _viewModel.KnownRows,
                DragonianSpellBodyViewModel.KnownEmptyMessage);
            PopulateDetailPane(_viewModel.Detail);
        }

        void RebuildColumn(
            RectTransform listRoot,
            List<GameObject> rowObjects,
            TextMeshProUGUI emptyText,
            IReadOnlyList<DragonianSpellRowModel> rows,
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
            {
                DragonianSpellRowModel row = rows[i];
                rowObjects.Add(CreateSpellRow(listRoot, row));
            }
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

        GameObject CreateSpellRow(RectTransform parent, DragonianSpellRowModel row)
        {
            bool selected = string.Equals(
                row.SpellId,
                _selectedSpellId,
                StringComparison.OrdinalIgnoreCase);

            var go = new GameObject(
                $"SpellRow_{row.SpellId}",
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
            bg.color = selected
                ? new Color(0.22f, 0.14f, 0.13f, 0.96f)
                : RacialUiTheme.DragonianRowBackground;

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = selected ? RacialUiTheme.FocusBorder : RacialUiTheme.DragonianRowBorder;
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
                accent.GetComponent<Image>().color = RacialUiTheme.DragonianSectionAccent;
            }

            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            string capturedId = row.SpellId;
            button.onClick.AddListener(() => SelectSpell(capturedId));

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
            icon.sprite = ResolveSpellIcon(row.Spell);
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
                row.Subtitle,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft);
            subtitle.color = RacialUiTheme.MutedText;
            subtitle.raycastTarget = false;

            if (row.ShowEquippedBadge)
            {
                var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                badgeGo.transform.SetParent(go.transform, false);
                var badgeLe = badgeGo.AddComponent<LayoutElement>();
                badgeLe.minWidth = badgeLe.preferredWidth = 72f;
                badgeLe.minHeight = 24f;
                Image badgeBg = badgeGo.GetComponent<Image>();
                badgeBg.sprite = RacialUiTheme.PlaceholderSprite;
                badgeBg.color = new Color(0.45f, 0.32f, 0.12f, 0.95f);

                TextMeshProUGUI badgeText = RacialUiTheme.CreateText(
                    badgeGo.transform,
                    "Label",
                    "Equipped",
                    RacialUiTheme.CardBadgeFontSize,
                    TextAlignmentOptions.Center,
                    FontStyles.Bold);
                badgeText.color = RacialUiTheme.ActiveAccent;
                RacialUiTheme.Stretch(badgeText.rectTransform);
                badgeText.raycastTarget = false;
            }

            return go;
        }

        static Sprite ResolveSpellIcon(DragonianSpellDefinition spell)
        {
            if (spell?.ability != null && spell.ability.hotbarIcon != null)
                return spell.ability.hotbarIcon;

            return RacialUiTheme.DragonianSpellEmblemSprite;
        }

        void PopulateDetailPane(DragonianSpellDetailModel detail)
        {
            if (detail == null)
                return;

            DragonianSpellDefinition spell = FindSpellDefinition(detail.SpellId);
            _detailIcon.sprite = spell != null ? ResolveSpellIcon(spell) : RacialUiTheme.DragonianSpellEmblemSprite;
            _detailTitleText.text = string.IsNullOrWhiteSpace(detail.Title) ? "Select a word-form." : detail.Title;

            if (string.IsNullOrWhiteSpace(detail.SpellId))
            {
                _detailBodyText.text = "Select a spell row to read its details.";
                _equipButton.gameObject.SetActive(false);
                _unequipButton.gameObject.SetActive(false);
                _detailErrorText.gameObject.SetActive(false);
                _detailFootnoteText.text = detail.HotbarFootnote;
                return;
            }

            string abilitySuffix = string.IsNullOrWhiteSpace(detail.AbilityLine)
                ? string.Empty
                : $"\n{detail.AbilityLine}";
            _detailBodyText.text = $"{detail.Description}\n\n{detail.CostLine}{abilitySuffix}";

            bool showActions = _viewModel.EditMode == DragonianSpellLoadoutEditMode.Edit;
            _equipButton.gameObject.SetActive(showActions && detail.ShowEquipButton);
            _unequipButton.gameObject.SetActive(showActions && detail.ShowUnequipButton);

            if (detail.ShowEquipButton)
            {
                _equipButton.interactable = detail.EquipEnabled;
                _equipButtonLabel.text = "Equip";
            }

            if (detail.ShowUnequipButton)
                _unequipButtonLabel.text = "Unequip";

            if (!string.IsNullOrWhiteSpace(detail.EquipDisabledReason))
            {
                _detailErrorText.text = detail.EquipDisabledReason;
                _detailErrorText.gameObject.SetActive(true);
            }
            else
            {
                _detailErrorText.gameObject.SetActive(false);
            }

            _detailFootnoteText.text = detail.HotbarFootnote;
        }

        DragonianSpellDefinition FindSpellDefinition(string spellId)
        {
            if (_viewModel == null || string.IsNullOrWhiteSpace(spellId))
                return null;

            DragonianSpellDefinition spell = FindInRows(_viewModel.MemorizedRows, spellId);
            return spell ?? FindInRows(_viewModel.KnownRows, spellId);
        }

        static DragonianSpellDefinition FindInRows(IReadOnlyList<DragonianSpellRowModel> rows, string spellId)
        {
            if (rows == null)
                return null;

            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i]?.SpellId, spellId, StringComparison.OrdinalIgnoreCase))
                    return rows[i].Spell;
            }

            return null;
        }

        void OnEquipClicked()
        {
            TryApplyLoadoutChange(memorize: true);
        }

        void OnUnequipClicked()
        {
            TryApplyLoadoutChange(memorize: false);
        }

        void TryApplyLoadoutChange(bool memorize)
        {
            if (_focusedActor == null || string.IsNullOrWhiteSpace(_selectedSpellId))
                return;

            string failureReason;
            bool ok = memorize
                ? DragonianSpellLoadoutService.TryMemorize(_focusedActor, _selectedSpellId, out failureReason)
                : DragonianSpellLoadoutService.TryUnmemorize(_focusedActor, _selectedSpellId, out failureReason);

            if (!ok)
            {
                _detailErrorText.text = failureReason ?? "Could not update equipped word-forms.";
                _detailErrorText.gameObject.SetActive(true);
                return;
            }

            AbilityHotbarUI.Instance?.RefreshAll();
            Rebuild(_focusedActor, _selectedSpellId);
        }
    }
}
