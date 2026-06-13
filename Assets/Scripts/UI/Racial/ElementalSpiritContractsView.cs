using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public sealed class ElementalSpiritContractsView : MonoBehaviour
    {
        Transform _contentRoot;
        readonly List<GameObject> _rows = new List<GameObject>();

        public static ElementalSpiritContractsView Create(Transform scrollContentParent)
        {
            Transform existing = scrollContentParent.Find("SpiritContractsContent");
            ElementalSpiritContractsView view;
            if (existing != null)
            {
                view = existing.GetComponent<ElementalSpiritContractsView>() ??
                       existing.gameObject.AddComponent<ElementalSpiritContractsView>();
                view._contentRoot = existing;
            }
            else
            {
                var content = new GameObject("SpiritContractsContent", typeof(RectTransform));
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

                view = content.AddComponent<ElementalSpiritContractsView>();
                view._contentRoot = content.transform;
            }

            return view;
        }

        public void Rebuild(IReadOnlyList<ElfElementalSpiritContractCard> cards)
        {
            ClearRows();

            if (cards == null || cards.Count == 0)
            {
                CreateMessageRow(ElfElementalSpiritViewModel.EmptyRosterMessage);
                return;
            }

            for (int i = 0; i < cards.Count; i++)
                _rows.Add(CreateContractRow(cards[i]));
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
            var row = new GameObject("Message", typeof(RectTransform));
            row.transform.SetParent(_contentRoot, false);
            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 80f;

            TextMeshProUGUI text = RacialUiTheme.CreateText(
                row.transform,
                "Text",
                message,
                RacialUiTheme.MessageFontSize,
                TextAlignmentOptions.TopLeft);
            RacialUiTheme.Stretch(text.rectTransform);
            text.color = RacialUiTheme.MutedText;
            _rows.Add(row);
        }

        GameObject CreateContractRow(ElfElementalSpiritContractCard card)
        {
            var row = new GameObject("ContractRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_contentRoot, false);
            row.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            row.GetComponent<Image>().color = new Color(0.14f, 0.15f, 0.17f, 0.95f);

            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 72f;

            var layout = row.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            TextMeshProUGUI title = RacialUiTheme.CreateText(
                row.transform,
                "Title",
                card.Title ?? "Spirit",
                RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            title.color = RacialUiTheme.TitleText;

            TextMeshProUGUI progress = RacialUiTheme.CreateText(
                row.transform,
                "Progress",
                card.ProgressLine ?? string.Empty,
                RacialUiTheme.MessageFontSize,
                TextAlignmentOptions.MidlineLeft);
            progress.color = RacialUiTheme.BodyText;

            TextMeshProUGUI cap = RacialUiTheme.CreateText(
                row.transform,
                "Cap",
                card.CapLine ?? string.Empty,
                RacialUiTheme.FooterFontSize,
                TextAlignmentOptions.MidlineLeft);
            cap.color = RacialUiTheme.MutedText;

            return row;
        }
    }
}
