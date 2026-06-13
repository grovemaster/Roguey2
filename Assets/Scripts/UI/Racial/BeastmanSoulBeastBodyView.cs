using System.Collections.Generic;
using JRogue.Actors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public sealed class BeastmanSoulBeastBodyView : MonoBehaviour
    {
        GameObject _unbondedRoot;
        Image _emptyEmblem;
        TextMeshProUGUI _unbondedTitle;
        TextMeshProUGUI _unbondedBody;

        GameObject _bondedRoot;
        Image _bondIcon;
        TextMeshProUGUI _bondTitle;
        TextMeshProUGUI _bondSubtitle;
        TextMeshProUGUI _bondDescription;
        TextMeshProUGUI _bondStats;
        TextMeshProUGUI _bondResistances;
        TextMeshProUGUI _bondProgressHint;
        TextMeshProUGUI _emptyAbilitiesHint;
        Transform _abilityListRoot;

        readonly List<GameObject> _abilityRows = new List<GameObject>();

        public static BeastmanSoulBeastBodyView Create(Transform parent)
        {
            Transform existing = parent.Find("BeastmanSoulBeastBodyContent");
            if (existing != null)
                Object.Destroy(existing.gameObject);

            var root = new GameObject("BeastmanSoulBeastBodyContent", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI section = RacialUiTheme.CreateText(
                root.transform,
                "SectionLabel",
                "SOUL BEAST BOND",
                RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            section.color = RacialUiTheme.BeastmanSectionAccent;
            section.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var contentArea = new GameObject("ContentArea", typeof(RectTransform));
            contentArea.transform.SetParent(root.transform, false);
            var contentLe = contentArea.AddComponent<LayoutElement>();
            contentLe.flexibleHeight = 1f;
            contentLe.minHeight = 280f;

            var view = root.AddComponent<BeastmanSoulBeastBodyView>();
            view.BuildUnbondedPanel(contentArea.transform);
            view.BuildBondedPanel(contentArea.transform);
            return view;
        }

        void BuildUnbondedPanel(Transform parent)
        {
            _unbondedRoot = new GameObject("UnbondedPanel", typeof(RectTransform));
            _unbondedRoot.transform.SetParent(parent, false);
            RacialUiTheme.Stretch(_unbondedRoot.GetComponent<RectTransform>());

            var layout = _unbondedRoot.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset(24, 24, 48, 48);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var emblemGo = new GameObject("EmptyEmblem", typeof(RectTransform), typeof(Image));
            emblemGo.transform.SetParent(_unbondedRoot.transform, false);
            var emblemLe = emblemGo.AddComponent<LayoutElement>();
            emblemLe.minWidth = emblemLe.preferredWidth = 96f;
            emblemLe.minHeight = emblemLe.preferredHeight = 96f;
            _emptyEmblem = emblemGo.GetComponent<Image>();
            _emptyEmblem.sprite = RacialUiTheme.SoulBeastEmptyEmblemSprite;
            _emptyEmblem.preserveAspect = true;
            _emptyEmblem.color = Color.white;

            _unbondedTitle = RacialUiTheme.CreateText(
                _unbondedRoot.transform,
                "Title",
                string.Empty,
                RacialUiTheme.CardTitleFontSize,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            _unbondedTitle.color = RacialUiTheme.TitleText;

            _unbondedBody = RacialUiTheme.CreateText(
                _unbondedRoot.transform,
                "Body",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.Center);
            _unbondedBody.color = RacialUiTheme.MutedText;
        }

        void BuildBondedPanel(Transform parent)
        {
            _bondedRoot = new GameObject("BondedPanel", typeof(RectTransform));
            _bondedRoot.transform.SetParent(parent, false);
            RacialUiTheme.Stretch(_bondedRoot.GetComponent<RectTransform>());

            var layout = _bondedRoot.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildBondSummaryCard(_bondedRoot.transform);

            TextMeshProUGUI abilitiesLabel = RacialUiTheme.CreateText(
                _bondedRoot.transform,
                "AbilitiesLabel",
                "CURRENT ABILITIES",
                RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            abilitiesLabel.color = RacialUiTheme.ActiveAccent;
            abilitiesLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            var scrollHost = new GameObject("AbilityScroll", typeof(RectTransform));
            scrollHost.transform.SetParent(_bondedRoot.transform, false);
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
            viewport.GetComponent<Image>().color = new Color(0.12f, 0.125f, 0.135f, 0.92f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero;
            _abilityListRoot = content.transform;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 0f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;

            _emptyAbilitiesHint = RacialUiTheme.CreateText(
                _bondedRoot.transform,
                "EmptyAbilitiesHint",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            _emptyAbilitiesHint.color = RacialUiTheme.MutedText;
            _emptyAbilitiesHint.gameObject.SetActive(false);
        }

        void BuildBondSummaryCard(Transform parent)
        {
            var card = new GameObject("BondSummaryCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            Image bg = card.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = RacialUiTheme.BeastmanCardBackground;

            var cardLe = card.AddComponent<LayoutElement>();
            cardLe.minHeight = 120f;

            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(card.transform, false);
            RectTransform accentRt = accent.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 0f);
            accentRt.anchorMax = new Vector2(0f, 1f);
            accentRt.pivot = new Vector2(0f, 0.5f);
            accentRt.sizeDelta = new Vector2(4f, 0f);
            accentRt.anchoredPosition = Vector2.zero;
            accent.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            accent.GetComponent<Image>().color = RacialUiTheme.BeastmanCardAccent;

            var layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 12, 10, 10);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var iconGo = new GameObject("BondIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(card.transform, false);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.minWidth = iconLe.preferredWidth = 72f;
            iconLe.minHeight = iconLe.preferredHeight = 72f;
            _bondIcon = iconGo.GetComponent<Image>();
            _bondIcon.sprite = RacialUiTheme.SoulBeastBondEmblemSprite;
            _bondIcon.preserveAspect = true;

            var textColumn = new GameObject("TextColumn", typeof(RectTransform));
            textColumn.transform.SetParent(card.transform, false);
            var textLe = textColumn.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1f;

            var textLayout = textColumn.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 4f;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            _bondTitle = RacialUiTheme.CreateText(
                textColumn.transform,
                "Title",
                string.Empty,
                24f,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            _bondTitle.color = RacialUiTheme.TitleText;

            _bondSubtitle = RacialUiTheme.CreateText(
                textColumn.transform,
                "Subtitle",
                string.Empty,
                RacialUiTheme.FooterFontSize,
                TextAlignmentOptions.MidlineLeft);
            _bondSubtitle.color = RacialUiTheme.BeastmanSectionAccent;

            _bondDescription = RacialUiTheme.CreateText(
                textColumn.transform,
                "Description",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft);

            _bondStats = RacialUiTheme.CreateText(
                textColumn.transform,
                "Stats",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft);

            _bondResistances = RacialUiTheme.CreateText(
                textColumn.transform,
                "Resistances",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft);

            _bondProgressHint = RacialUiTheme.CreateText(
                textColumn.transform,
                "ProgressHint",
                string.Empty,
                RacialUiTheme.FooterFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            _bondProgressHint.color = RacialUiTheme.MutedText;
        }

        public void Rebuild(BaseActor beastman)
        {
            ClearAbilityRows();
            BeastmanSoulBeastBodyViewModel vm = BeastmanSoulBeastBodyViewModel.Build(beastman);

            if (!vm.IsBonded)
            {
                _unbondedRoot.SetActive(true);
                _bondedRoot.SetActive(false);
                _emptyEmblem.sprite = RacialUiTheme.SoulBeastEmptyEmblemSprite;
                _unbondedTitle.text = vm.EmptyStateTitle;
                _unbondedBody.text = vm.EmptyStateBody;
                return;
            }

            _unbondedRoot.SetActive(false);
            _bondedRoot.SetActive(true);

            _bondIcon.sprite = RacialUiTheme.SoulBeastBondEmblemSprite;
            _bondTitle.text = vm.Summary.Title;
            _bondSubtitle.text = vm.Summary.Subtitle;
            _bondDescription.text = vm.Summary.Description;
            _bondDescription.gameObject.SetActive(!string.IsNullOrWhiteSpace(vm.Summary.Description));

            _bondStats.text = vm.Summary.StatsLine;
            _bondStats.gameObject.SetActive(!string.IsNullOrWhiteSpace(vm.Summary.StatsLine));

            _bondResistances.text = vm.Summary.ResistancesLine;
            _bondResistances.gameObject.SetActive(!string.IsNullOrWhiteSpace(vm.Summary.ResistancesLine));

            _bondProgressHint.text = vm.Summary.ProgressHint;
            _bondProgressHint.gameObject.SetActive(!string.IsNullOrWhiteSpace(vm.Summary.ProgressHint));

            if (vm.ShowEmptyAbilitiesHint)
            {
                _emptyAbilitiesHint.text = BeastmanSoulBeastBodyViewModel.EmptyAbilitiesHint;
                _emptyAbilitiesHint.gameObject.SetActive(true);
            }
            else
            {
                _emptyAbilitiesHint.gameObject.SetActive(false);
            }

            for (int i = 0; i < vm.AbilityRows.Count; i++)
                _abilityRows.Add(CreateAbilityRow(vm.AbilityRows[i]));
        }

        public void SetPlainMessage(string message)
        {
            ClearAbilityRows();
            _unbondedRoot.SetActive(true);
            _bondedRoot.SetActive(false);
            _unbondedTitle.text = string.Empty;
            _unbondedBody.text = message;
            _emptyEmblem.sprite = RacialUiTheme.SoulBeastEmptyEmblemSprite;
        }

        void ClearAbilityRows()
        {
            foreach (GameObject row in _abilityRows)
            {
                if (row != null)
                    Destroy(row);
            }

            _abilityRows.Clear();
        }

        GameObject CreateAbilityRow(BeastmanSoulBeastAbilityRowModel model)
        {
            var row = new GameObject("AbilityRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_abilityListRoot, false);
            row.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            row.GetComponent<Image>().color = new Color(0.14f, 0.15f, 0.165f, 0.55f);

            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 72f;

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(row.transform, false);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.minWidth = iconLe.preferredWidth = 52f;
            iconLe.minHeight = iconLe.preferredHeight = 52f;
            Image icon = iconGo.GetComponent<Image>();
            icon.sprite = ResolveAbilityIcon(model);
            icon.preserveAspect = true;

            var textColumn = new GameObject("TextColumn", typeof(RectTransform));
            textColumn.transform.SetParent(row.transform, false);
            var textLe = textColumn.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1f;

            var textLayout = textColumn.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2f;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            var headerRow = new GameObject("HeaderRow", typeof(RectTransform));
            headerRow.transform.SetParent(textColumn.transform, false);
            var headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8f;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI title = RacialUiTheme.CreateText(
                headerRow.transform,
                "Title",
                model.Title,
                19f,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            title.color = RacialUiTheme.TitleText;
            var titleLe = title.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;

            TextMeshProUGUI levelTag = RacialUiTheme.CreateText(
                headerRow.transform,
                "LevelTag",
                model.LevelTag,
                RacialUiTheme.FooterFontSize,
                TextAlignmentOptions.MidlineRight);
            levelTag.color = RacialUiTheme.MutedText;

            string body = model.Description;
            if (!string.IsNullOrWhiteSpace(model.Meta))
                body = string.IsNullOrWhiteSpace(body) ? model.Meta : $"{body} · {model.Meta}";

            if (!string.IsNullOrWhiteSpace(body))
            {
                TextMeshProUGUI description = RacialUiTheme.CreateText(
                    textColumn.transform,
                    "Description",
                    body,
                    RacialUiTheme.CardBodyFontSize,
                    TextAlignmentOptions.MidlineLeft);
                description.color = RacialUiTheme.BodyText;
            }

            if (model.ShowHotbarFootnote)
            {
                TextMeshProUGUI footnote = RacialUiTheme.CreateText(
                    textColumn.transform,
                    "Footnote",
                    "Assign on the ability hotbar to use in combat.",
                    RacialUiTheme.FooterFontSize,
                    TextAlignmentOptions.MidlineLeft,
                    FontStyles.Italic);
                footnote.color = RacialUiTheme.MutedText;
            }

            return row;
        }

        static Sprite ResolveAbilityIcon(BeastmanSoulBeastAbilityRowModel model)
        {
            if (model.Icon != null)
                return model.Icon;

            return model.Kind == BeastmanSoulBeastAbilityKind.Active
                ? RacialUiTheme.SoulBeastActiveEmblemSprite
                : RacialUiTheme.SoulBeastPassiveEmblemSprite;
        }
    }
}
