using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public sealed class SpiritImprintTimelineView : MonoBehaviour
    {
        Transform _contentRoot;
        readonly List<GameObject> _rows = new List<GameObject>();

        public static SpiritImprintTimelineView Create(Transform scrollContentParent)
        {
            Transform existing = scrollContentParent.Find("TimelineContent");
            SpiritImprintTimelineView view;
            if (existing != null)
            {
                view = existing.GetComponent<SpiritImprintTimelineView>() ??
                       existing.gameObject.AddComponent<SpiritImprintTimelineView>();
                view._contentRoot = existing;
            }
            else
            {
                var content = new GameObject("TimelineContent", typeof(RectTransform));
                content.transform.SetParent(scrollContentParent, false);
                var rt = content.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, 0f);

                var layout = content.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.padding = new RectOffset(0, 0, 4, 8);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                view = content.AddComponent<SpiritImprintTimelineView>();
                view._contentRoot = content.transform;
            }

            return view;
        }

        public void Rebuild(IReadOnlyList<SpiritImprintCardViewModel> cards)
        {
            ClearRows();

            if (cards == null || cards.Count == 0)
            {
                CreateMessageRow("No Spirit Imprint selections yet.\n\nVisit the Shaman Barbarian in town to begin your path.");
                return;
            }

            for (int i = 0; i < cards.Count; i++)
                _rows.Add(CreateTimelineRow(cards[i], i, i == cards.Count - 1));
        }

        public void SetPlainMessage(string message)
        {
            ClearRows();
            CreateMessageRow(message);
        }

        void ClearRows()
        {
            foreach (GameObject row in _rows)
            {
                if (row != null)
                    Destroy(row);
            }

            _rows.Clear();
        }

        void CreateMessageRow(string message)
        {
            var go = new GameObject("Message", typeof(RectTransform));
            go.transform.SetParent(_contentRoot, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 80f;

            TextMeshProUGUI text = RacialUiTheme.CreateText(
                go.transform, "Text", message, RacialUiTheme.MessageFontSize, TextAlignmentOptions.TopLeft);
            text.color = RacialUiTheme.MutedText;
            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);

            _rows.Add(go);
        }

        GameObject CreateTimelineRow(SpiritImprintCardViewModel card, int index, bool isLast)
        {
            bool isGhost = card.Kind == SpiritImprintCardKind.ForeclosedGhost;

            var row = new GameObject($"TimelineRow_{index}", typeof(RectTransform));
            row.transform.SetParent(_contentRoot, false);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = isGhost ? 72f : 96f;

            CreateTimelineRail(row.transform, isGhost, isLast);
            CreateNodeCard(row.transform, card, isGhost);

            if (isGhost)
            {
                var group = row.AddComponent<CanvasGroup>();
                group.alpha = 0.42f;
            }

            return row;
        }

        static void CreateTimelineRail(Transform parent, bool isGhost, bool isLast)
        {
            var rail = new GameObject("Rail", typeof(RectTransform));
            rail.transform.SetParent(parent, false);

            var railLe = rail.AddComponent<LayoutElement>();
            railLe.minWidth = 28f;
            railLe.preferredWidth = 28f;
            railLe.flexibleHeight = 1f;

            var dotGo = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dotGo.transform.SetParent(rail.transform, false);
            RectTransform dotRt = dotGo.GetComponent<RectTransform>();
            dotRt.anchorMin = new Vector2(0.5f, 1f);
            dotRt.anchorMax = new Vector2(0.5f, 1f);
            dotRt.pivot = new Vector2(0.5f, 1f);
            dotRt.anchoredPosition = new Vector2(0f, -4f);
            dotRt.sizeDelta = new Vector2(14f, 14f);

            Image dot = dotGo.GetComponent<Image>();
            dot.sprite = RacialUiTheme.PlaceholderSprite;
            if (isGhost)
            {
                dot.color = RacialUiTheme.GhostDot;
                dot.type = Image.Type.Simple;
            }
            else
            {
                dot.color = RacialUiTheme.TimelineDot;
            }

            if (!isLast)
            {
                var lineGo = new GameObject("Line", typeof(RectTransform), typeof(Image));
                lineGo.transform.SetParent(rail.transform, false);
                RectTransform lineRt = lineGo.GetComponent<RectTransform>();
                lineRt.anchorMin = new Vector2(0.5f, 0f);
                lineRt.anchorMax = new Vector2(0.5f, 1f);
                lineRt.pivot = new Vector2(0.5f, 1f);
                lineRt.anchoredPosition = new Vector2(0f, -10f);
                lineRt.sizeDelta = new Vector2(2f, -14f);
                Image line = lineGo.GetComponent<Image>();
                line.sprite = RacialUiTheme.PlaceholderSprite;
                line.color = isGhost
                    ? new Color(0.35f, 0.38f, 0.42f, 0.35f)
                    : RacialUiTheme.TimelineLine;
            }
        }

        static void CreateNodeCard(Transform parent, SpiritImprintCardViewModel card, bool isGhost)
        {
            var cardGo = new GameObject("NodeCard", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(parent, false);

            var cardLe = cardGo.AddComponent<LayoutElement>();
            cardLe.flexibleWidth = 1f;
            cardLe.minWidth = 120f;

            Image cardBg = cardGo.GetComponent<Image>();
            cardBg.sprite = RacialUiTheme.PlaceholderSprite;
            cardBg.color = isGhost
                ? new Color(0.12f, 0.13f, 0.15f, 0.55f)
                : RacialUiTheme.CardBackground;

            var outline = cardGo.AddComponent<Outline>();
            outline.effectColor = isGhost
                ? new Color(0.45f, 0.48f, 0.52f, 0.65f)
                : RacialUiTheme.CardBorder;
            outline.effectDistance = new Vector2(1f, -1f);

            if (!isGhost)
            {
                var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
                accent.transform.SetParent(cardGo.transform, false);
                RectTransform accentRt = accent.GetComponent<RectTransform>();
                accentRt.anchorMin = new Vector2(0f, 0f);
                accentRt.anchorMax = new Vector2(0f, 1f);
                accentRt.pivot = new Vector2(0f, 0.5f);
                accentRt.sizeDelta = new Vector2(3f, 0f);
                accentRt.anchoredPosition = Vector2.zero;
                accent.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
                accent.GetComponent<Image>().color = RacialUiTheme.ActiveAccent;
            }

            var padding = cardGo.AddComponent<VerticalLayoutGroup>();
            padding.padding = new RectOffset(isGhost ? 10 : 13, 10, 10, 10);
            padding.spacing = 4f;
            padding.childControlWidth = true;
            padding.childControlHeight = true;
            padding.childForceExpandWidth = true;
            padding.childForceExpandHeight = false;

            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(cardGo.transform, false);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(header.transform, false);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.minWidth = 48f;
            iconLe.preferredWidth = 48f;
            iconLe.minHeight = 48f;
            iconLe.preferredHeight = 48f;
            Image icon = iconGo.GetComponent<Image>();
            icon.sprite = RacialUiTheme.ImprintEmblemSprite;
            icon.preserveAspect = true;
            icon.color = isGhost
                ? new Color(1f, 1f, 1f, 0.45f)
                : Color.white;

            var titleBlock = new GameObject("TitleBlock", typeof(RectTransform));
            titleBlock.transform.SetParent(header.transform, false);
            var titleBlockLe = titleBlock.AddComponent<LayoutElement>();
            titleBlockLe.flexibleWidth = 1f;
            var titleLayout = titleBlock.AddComponent<VerticalLayoutGroup>();
            titleLayout.spacing = 0f;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandWidth = true;

            TextMeshProUGUI title = RacialUiTheme.CreateText(
                titleBlock.transform, "Title",
                isGhost ? $"○ {card.Title} — {card.Subtitle}." : card.Title,
                RacialUiTheme.CardTitleFontSize, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            title.color = isGhost ? RacialUiTheme.MutedText : RacialUiTheme.BodyText;

            TextMeshProUGUI subtitle = RacialUiTheme.CreateText(
                titleBlock.transform, "Subtitle", string.Empty, 12f,
                TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            subtitle.gameObject.SetActive(false);

            if (!isGhost)
            {
                var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                badgeGo.transform.SetParent(header.transform, false);
                var badgeLe = badgeGo.AddComponent<LayoutElement>();
                badgeLe.minWidth = 88f;
                badgeLe.preferredWidth = 88f;
                badgeLe.minHeight = 26f;
                badgeGo.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
                badgeGo.GetComponent<Image>().color = new Color(
                    RacialUiTheme.ActiveBadge.r,
                    RacialUiTheme.ActiveBadge.g,
                    RacialUiTheme.ActiveBadge.b,
                    0.25f);

                TextMeshProUGUI badge = RacialUiTheme.CreateText(
                    badgeGo.transform, "Label", card.Subtitle, RacialUiTheme.CardBadgeFontSize,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                RacialUiTheme.Stretch(badge.rectTransform);
                badge.color = RacialUiTheme.ActiveBadge;
            }

            if (!string.IsNullOrWhiteSpace(card.Description))
            {
                TextMeshProUGUI body = RacialUiTheme.CreateText(
                    cardGo.transform, "Body", card.Description, RacialUiTheme.CardBodyFontSize,
                    TextAlignmentOptions.TopLeft);
                body.color = isGhost ? RacialUiTheme.MutedText : new Color(0.78f, 0.82f, 0.86f);
                body.margin = new Vector4(isGhost ? 8f : 56f, 4f, 8f, 4f);
                body.lineSpacing = 2f;
                var bodyLe = body.gameObject.AddComponent<LayoutElement>();
                bodyLe.minHeight = isGhost ? 24f : 32f;
            }
        }
    }
}
