using System.Collections.Generic;
using JRogue.Actors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public sealed class DwarfClanBodyView : MonoBehaviour
    {
        GameObject _unaffiliatedRoot;
        TextMeshProUGUI _unaffiliatedBody;

        GameObject _memberRoot;
        TextMeshProUGUI _summaryLineText;
        TextMeshProUGUI _patronLineText;
        SpiritImprintTimelineView _clanTimeline;

        TextMeshProUGUI _commonFootnoteText;
        Transform _commonListRoot;
        readonly List<GameObject> _commonRows = new List<GameObject>();

        public static DwarfClanBodyView Create(Transform parent)
        {
            Transform existing = parent.Find("DwarfClanBodyContent");
            if (existing != null)
                Object.Destroy(existing.gameObject);

            var root = new GameObject("DwarfClanBodyContent", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var view = root.AddComponent<DwarfClanBodyView>();
            view.BuildUnaffiliatedPanel(root.transform);
            view.BuildMemberPanel(root.transform);
            view.BuildCommonAbilitiesSection(root.transform);
            return view;
        }

        void BuildUnaffiliatedPanel(Transform parent)
        {
            _unaffiliatedRoot = new GameObject("UnaffiliatedPanel", typeof(RectTransform));
            _unaffiliatedRoot.transform.SetParent(parent, false);

            var le = _unaffiliatedRoot.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;
            le.minHeight = 180f;

            var layout = _unaffiliatedRoot.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset(24, 24, 36, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = RacialUiTheme.CreateText(
                _unaffiliatedRoot.transform,
                "Title",
                "NO CLAN ALLEGIANCE",
                RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            title.color = RacialUiTheme.DwarfSectionAccent;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            _unaffiliatedBody = RacialUiTheme.CreateText(
                _unaffiliatedRoot.transform,
                "Body",
                string.Empty,
                RacialUiTheme.MessageFontSize,
                TextAlignmentOptions.Center);
            _unaffiliatedBody.color = RacialUiTheme.MutedText;
        }

        void BuildMemberPanel(Transform parent)
        {
            _memberRoot = new GameObject("MemberPanel", typeof(RectTransform));
            _memberRoot.transform.SetParent(parent, false);

            var memberLe = _memberRoot.AddComponent<LayoutElement>();
            memberLe.flexibleHeight = 1f;
            memberLe.minHeight = 220f;

            var memberLayout = _memberRoot.AddComponent<VerticalLayoutGroup>();
            memberLayout.spacing = 6f;
            memberLayout.childControlWidth = true;
            memberLayout.childControlHeight = true;
            memberLayout.childForceExpandWidth = true;
            memberLayout.childForceExpandHeight = false;

            BuildSummaryStrip(_memberRoot.transform);
            BuildClanTimeline(_memberRoot.transform);
        }

        void BuildSummaryStrip(Transform parent)
        {
            var strip = new GameObject("SummaryStrip", typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(parent, false);
            Image bg = strip.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = RacialUiTheme.DwarfBudgetBackground;

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
            _summaryLineText.color = RacialUiTheme.DwarfSectionAccent;

            _patronLineText = RacialUiTheme.CreateText(
                strip.transform,
                "PatronLine",
                string.Empty,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft);
            _patronLineText.color = RacialUiTheme.MutedText;
        }

        void BuildClanTimeline(Transform parent)
        {
            TextMeshProUGUI section = RacialUiTheme.CreateText(
                parent,
                "ClanSectionLabel",
                "ANCESTOR PATH",
                RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            section.color = RacialUiTheme.DwarfSectionAccent;
            section.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var scrollHost = new GameObject("ClanScroll", typeof(RectTransform));
            scrollHost.transform.SetParent(parent, false);
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
            viewport.GetComponent<Image>().color = RacialUiTheme.DwarfColumnBackground;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;

            _clanTimeline = SpiritImprintTimelineView.Create(contentRt);
        }

        void BuildCommonAbilitiesSection(Transform parent)
        {
            TextMeshProUGUI section = RacialUiTheme.CreateText(
                parent,
                "CommonSectionLabel",
                "COMMON ABILITIES",
                RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            section.color = RacialUiTheme.DwarfSectionAccent;
            section.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            _commonListRoot = new GameObject("CommonList", typeof(RectTransform)).transform;
            _commonListRoot.SetParent(parent, false);
            var listLayout = _commonListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 6f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            _commonListRoot.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _commonFootnoteText = RacialUiTheme.CreateText(
                parent,
                "CommonFootnote",
                DwarfClanBodyViewModel.CommonAbilitiesFootnote,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            _commonFootnoteText.color = RacialUiTheme.MutedText;
            _commonFootnoteText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        }

        public void Rebuild(BaseActor actor)
        {
            DwarfClanBodyViewModel vm = DwarfClanBodyViewModel.Build(actor);
            Rebuild(vm);
        }

        public void Rebuild(DwarfClanBodyViewModel vm)
        {
            ClearCommonRows();

            if (vm == null || !vm.CanDisplay)
            {
                _unaffiliatedRoot.SetActive(true);
                _memberRoot.SetActive(false);
                _unaffiliatedBody.text = DwarfClanBodyViewModel.CannotDisplayMessage;
                _commonFootnoteText.gameObject.SetActive(false);
                return;
            }

            _commonFootnoteText.gameObject.SetActive(true);
            RebuildCommonSlots(vm.CommonSlots);

            if (vm.IsUnaffiliated)
            {
                _unaffiliatedRoot.SetActive(true);
                _memberRoot.SetActive(false);
                _unaffiliatedBody.text = vm.UnaffiliatedMessage;
                return;
            }

            _unaffiliatedRoot.SetActive(false);
            _memberRoot.SetActive(true);
            _summaryLineText.text = vm.SummaryLine;
            _patronLineText.text = vm.PatronLine;
            _patronLineText.gameObject.SetActive(!string.IsNullOrWhiteSpace(vm.PatronLine));

            if (vm.ClanCards == null || vm.ClanCards.Count == 0)
            {
                _clanTimeline.SetPlainMessage(
                    "No ancestor techniques learned yet.\n\nPay respects at the Hall of Ancestors altar.");
            }
            else
            {
                _clanTimeline.Rebuild(vm.ClanCards);
            }
        }

        void RebuildCommonSlots(IReadOnlyList<DwarfCommonSlotRowModel> rows)
        {
            if (rows == null)
                return;

            foreach (DwarfCommonSlotRowModel row in rows)
                _commonRows.Add(CreateCommonRow(row));
        }

        GameObject CreateCommonRow(DwarfCommonSlotRowModel row)
        {
            var card = new GameObject($"CommonSlot{row.SlotIndex}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_commonListRoot, false);

            Image bg = card.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = row.IsEmpty ? RacialUiTheme.DwarfColumnBackground : RacialUiTheme.DwarfRowBackground;

            var outline = card.AddComponent<Outline>();
            outline.effectColor = RacialUiTheme.DwarfRowBorder;
            outline.effectDistance = new Vector2(1f, -1f);

            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 72f;
            le.preferredHeight = 72f;

            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = RacialUiTheme.CreateText(
                card.transform,
                "Title",
                row.Title,
                RacialUiTheme.CardTitleFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            title.color = row.IsEmpty ? RacialUiTheme.MutedText : RacialUiTheme.BodyText;

            TextMeshProUGUI subtitle = RacialUiTheme.CreateText(
                card.transform,
                "Subtitle",
                row.Subtitle,
                RacialUiTheme.CardBadgeFontSize,
                TextAlignmentOptions.MidlineLeft);
            subtitle.color = row.IsEmpty ? RacialUiTheme.MutedText : RacialUiTheme.DwarfSecondaryAccent;

            TextMeshProUGUI body = RacialUiTheme.CreateText(
                card.transform,
                "Body",
                row.Description,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.TopLeft);
            body.color = RacialUiTheme.MutedText;

            return card;
        }

        void ClearCommonRows()
        {
            foreach (GameObject row in _commonRows)
            {
                if (row != null)
                    Destroy(row);
            }

            _commonRows.Clear();
        }
    }
}
