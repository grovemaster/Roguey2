using System;
using System.Collections.Generic;
using JRogue.Stats;
using JRogue.UI.Racial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Proficiency
{
    public sealed class ProficiencyDetailPaneView : MonoBehaviour
    {
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _bodyText;

        public static ProficiencyDetailPaneView Create(Transform parent)
        {
            var root = new GameObject("ProficiencyDetailPane", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);

            Image bg = root.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = RacialUiTheme.CardBackground;

            var le = root.AddComponent<LayoutElement>();
            le.minHeight = 220f;
            le.preferredHeight = 280f;
            le.flexibleHeight = 0.3f;

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var view = root.AddComponent<ProficiencyDetailPaneView>();
            view._titleText = CreateText(root.transform, "Title", "DETAILS", 20f, FontStyles.Bold, 28f);
            view._titleText.color = RacialUiTheme.SectionLabel;

            var scrollHost = new GameObject("BodyScroll", typeof(RectTransform));
            scrollHost.transform.SetParent(root.transform, false);
            var scrollLe = scrollHost.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 120f;

            ScrollRect scroll = scrollHost.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollHost.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            viewport.GetComponent<Image>().color = Color.clear;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            view._bodyText = CreateText(content.transform, "Body", string.Empty, 16f, FontStyles.Normal, 0f);
            view._bodyText.alignment = TextAlignmentOptions.TopLeft;
            view._bodyText.color = RacialUiTheme.BodyText;
            ContentSizeFitter fitter = view._bodyText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;

            return view;
        }

        public void Refresh(ProficiencyRowViewModel row)
        {
            if (_titleText != null)
                _titleText.text = ProficiencyDetailFormatter.BuildTitle(row);

            if (_bodyText != null)
                _bodyText.text = ProficiencyDetailFormatter.BuildBody(row);
        }

        static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            float size,
            FontStyles style,
            float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (height > 0f)
            {
                var le = go.AddComponent<LayoutElement>();
                le.minHeight = height;
                le.preferredHeight = height;
            }

            TextMeshProUGUI tmp = RacialUiTheme.CreateText(go.transform, "Label", value, size,
                TextAlignmentOptions.MidlineLeft, style);
            RacialUiTheme.Stretch(tmp.rectTransform);
            return tmp;
        }
    }
}
