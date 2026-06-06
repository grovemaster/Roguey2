using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    /// <summary>Right-hand inspect pane: hero row (icon + title) + scrollable body.</summary>
    public sealed class InventoryInspectPaneView : MonoBehaviour
    {
        Image _heroIcon;
        TextMeshProUGUI _heroText;
        TextMeshProUGUI _bodyText;
        ScrollRect _scroll;

        public static InventoryInspectPaneView Create(Transform parent, Sprite placeholder)
        {
            Transform existing = parent.Find("InspectPane");
            if (existing != null)
                return existing.GetComponent<InventoryInspectPaneView>() ??
                       existing.gameObject.AddComponent<InventoryInspectPaneView>();

            var root = new GameObject("InspectPane", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rootLe = root.AddComponent<LayoutElement>();
            rootLe.flexibleWidth = 1f;
            rootLe.flexibleHeight = 1f;
            rootLe.minWidth = 280f;

            var v = root.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(10, 10, 12, 10);
            v.spacing = 8;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            // Hero row
            var hero = new GameObject("Hero", typeof(RectTransform));
            hero.transform.SetParent(root.transform, false);
            var heroLe = hero.AddComponent<LayoutElement>();
            heroLe.minHeight = 100f;
            heroLe.preferredHeight = 112f;
            heroLe.flexibleWidth = 1f;

            var heroH = hero.AddComponent<HorizontalLayoutGroup>();
            heroH.spacing = 12;
            heroH.childAlignment = TextAnchor.UpperLeft;
            heroH.childControlWidth = true;
            heroH.childControlHeight = true;
            heroH.childForceExpandWidth = true;
            heroH.childForceExpandHeight = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(hero.transform, false);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 96f;
            iconLe.preferredHeight = 96f;
            iconLe.minWidth = 96f;
            iconLe.minHeight = 96f;
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.sprite = placeholder;
            iconImg.color = Color.white;

            var heroTextGo = new GameObject("HeroText", typeof(RectTransform));
            heroTextGo.transform.SetParent(hero.transform, false);
            var heroTextLe = heroTextGo.AddComponent<LayoutElement>();
            heroTextLe.flexibleWidth = 1f;
            heroTextLe.flexibleHeight = 1f;
            heroTextLe.minWidth = 220f;
            var heroTmp = heroTextGo.AddComponent<TextMeshProUGUI>();
            heroTmp.richText = true;
            heroTmp.fontSize = 14f;
            heroTmp.alignment = TextAlignmentOptions.TopLeft;
            heroTmp.textWrappingMode = TextWrappingModes.Normal;
            heroTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Scroll body
            var scrollGo = new GameObject("BodyScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(root.transform, false);
            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.flexibleWidth = 1f;
            scrollLe.minHeight = 80f;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            SetStretch(vpRt);

            var content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var bodyLe = content.AddComponent<LayoutElement>();
            bodyLe.flexibleWidth = 1f;
            bodyLe.minWidth = 200f;

            var bodyTmp = content.AddComponent<TextMeshProUGUI>();
            bodyTmp.richText = true;
            bodyTmp.fontSize = 13f;
            bodyTmp.alignment = TextAlignmentOptions.TopLeft;
            bodyTmp.textWrappingMode = TextWrappingModes.Normal;
            bodyTmp.margin = new Vector4(4, 4, 4, 8);

            scroll.viewport = vpRt;
            scroll.content = contentRt;

            var view = root.AddComponent<InventoryInspectPaneView>();
            view._heroIcon = iconImg;
            view._heroText = heroTmp;
            view._bodyText = bodyTmp;
            view._scroll = scroll;
            return view;
        }

        public void SetContent(Sprite icon, string heroRich, string bodyRich, float detailFontScale)
        {
            if (_heroIcon != null)
            {
                _heroIcon.sprite = icon;
                _heroIcon.color = icon != null ? Color.white : new Color(0.45f, 0.45f, 0.48f, 0.9f);
                LayoutElement iconLe = _heroIcon.GetComponent<LayoutElement>();
                if (iconLe != null)
                {
                    float iconSize = 96f * detailFontScale;
                    iconLe.minWidth = iconLe.preferredWidth = iconSize;
                    iconLe.minHeight = iconLe.preferredHeight = iconSize;
                }
            }

            if (_heroText != null)
            {
                _heroText.fontSize = 14f * detailFontScale;
                _heroText.text = heroRich ?? string.Empty;
            }

            if (_bodyText != null)
            {
                _bodyText.fontSize = 13f * detailFontScale;
                _bodyText.text = bodyRich ?? string.Empty;
            }

            if (_scroll != null && _scroll.content != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
                _scroll.verticalNormalizedPosition = 1f;
            }
        }

        static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
