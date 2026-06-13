using JRogue.Actors;
using JRogue.Racial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public sealed class TieflingImplantBodyView : MonoBehaviour
    {
        TextMeshProUGUI _folkBaselineText;
        TieflingImplantSlotGridView _grid;
        Image _heroIcon;
        TextMeshProUGUI _heroText;
        TextMeshProUGUI _leftColumnText;
        TextMeshProUGUI _rightColumnText;
        RectTransform _scrollContent;
        ScrollRect _bodyScroll;

        BaseActor _focusedActor;
        ImplantSlot _selectedSlot = ImplantSlot.LeftArm;
        TieflingImplantBodyViewModel _viewModel;

        public static TieflingImplantBodyView Create(Transform parent)
        {
            Transform existing = parent.Find("TieflingImplantBodyContent");
            if (existing != null)
            {
                Object.Destroy(existing.gameObject);
            }

            var root = new GameObject("TieflingImplantBodyContent", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var folkGo = new GameObject("FolkBaseline", typeof(RectTransform));
            folkGo.transform.SetParent(root.transform, false);
            var folkLe = folkGo.AddComponent<LayoutElement>();
            folkLe.minHeight = 44f;
            folkLe.preferredHeight = 44f;
            TextMeshProUGUI folkText = RacialUiTheme.CreateText(
                folkGo.transform, "Text", string.Empty, RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft);
            folkText.color = RacialUiTheme.MutedText;
            RacialUiTheme.Stretch(folkText.rectTransform);

            var middleBand = new GameObject("MiddleBand", typeof(RectTransform));
            middleBand.transform.SetParent(root.transform, false);
            var middleLe = middleBand.AddComponent<LayoutElement>();
            middleLe.flexibleHeight = 1f;
            middleLe.minHeight = 280f;

            var view = root.AddComponent<TieflingImplantBodyView>();
            view._folkBaselineText = folkText;
            view._grid = TieflingImplantSlotGridView.Create(middleBand.transform, view.SelectSlot);
            view.BuildDetailPane(root.transform);
            return view;
        }

        void BuildDetailPane(Transform parent)
        {
            var root = new GameObject("DetailPane", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Image bg = root.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = new Color(0.12f, 0.125f, 0.135f, 0.92f);

            var le = root.AddComponent<LayoutElement>();
            le.minHeight = 240f;
            le.preferredHeight = 300f;
            le.flexibleHeight = 0f;

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI section = RacialUiTheme.CreateText(
                root.transform, "Section", "DETAILS", RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            section.color = new Color(0.88f, 0.55f, 0.38f, 1f);
            section.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            var scrollGo = new GameObject("BodyScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(root.transform, false);
            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 180f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
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
            contentLayout.padding = new RectOffset(0, 0, 0, 4);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentLe = content.AddComponent<LayoutElement>();
            contentLe.flexibleWidth = 1f;

            var mainRow = new GameObject("MainRow", typeof(RectTransform));
            mainRow.transform.SetParent(content.transform, false);
            var mainRowLe = mainRow.AddComponent<LayoutElement>();
            mainRowLe.flexibleWidth = 1f;
            mainRowLe.minHeight = 160f;

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
            iconLe.minWidth = iconLe.preferredWidth = 96f;
            iconLe.minHeight = iconLe.preferredHeight = 96f;
            iconLe.flexibleWidth = 0f;
            _heroIcon = iconGo.GetComponent<Image>();
            _heroIcon.preserveAspect = true;
            _heroIcon.sprite = RacialUiTheme.PlaceholderSprite;

            var textStack = new GameObject("TextStack", typeof(RectTransform));
            textStack.transform.SetParent(mainRow.transform, false);
            var textStackLe = textStack.AddComponent<LayoutElement>();
            textStackLe.flexibleWidth = 1f;
            textStackLe.minWidth = 400f;

            var textStackLayout = textStack.AddComponent<VerticalLayoutGroup>();
            textStackLayout.spacing = 10f;
            textStackLayout.childControlWidth = true;
            textStackLayout.childControlHeight = true;
            textStackLayout.childForceExpandWidth = true;
            textStackLayout.childForceExpandHeight = false;

            var heroTextGo = new GameObject("HeroText", typeof(RectTransform));
            heroTextGo.transform.SetParent(textStack.transform, false);
            heroTextGo.AddComponent<LayoutElement>().preferredHeight = 52f;
            _heroText = RacialUiTheme.CreateText(
                heroTextGo.transform, "Text", string.Empty, RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.TopLeft);
            RacialUiTheme.Stretch(_heroText.rectTransform);

            var columnsRow = new GameObject("Columns", typeof(RectTransform));
            columnsRow.transform.SetParent(textStack.transform, false);
            var columnsLe = columnsRow.AddComponent<LayoutElement>();
            columnsLe.flexibleWidth = 1f;
            columnsLe.minHeight = 120f;

            var columnsLayout = columnsRow.AddComponent<HorizontalLayoutGroup>();
            columnsLayout.spacing = 24f;
            columnsLayout.childAlignment = TextAnchor.UpperLeft;
            columnsLayout.childControlWidth = true;
            columnsLayout.childControlHeight = true;
            columnsLayout.childForceExpandWidth = true;
            columnsLayout.childForceExpandHeight = false;

            _leftColumnText = CreateColumnText(columnsRow.transform, "LeftColumn");
            _rightColumnText = CreateColumnText(columnsRow.transform, "RightColumn");

            _bodyScroll = scrollGo.GetComponent<ScrollRect>();
            _bodyScroll.viewport = viewport.GetComponent<RectTransform>();
            _bodyScroll.content = _scrollContent;
            _bodyScroll.horizontal = false;
            _bodyScroll.vertical = true;
            _bodyScroll.movementType = ScrollRect.MovementType.Clamped;

            content.AddComponent<ScrollContentWidthSync>().Initialize(viewport.GetComponent<RectTransform>(), _scrollContent);
        }

        static TextMeshProUGUI CreateColumnText(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minWidth = 180f;

            TextMeshProUGUI text = RacialUiTheme.CreateText(
                go.transform, "Text", string.Empty, RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.TopLeft);
            text.richText = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            RacialUiTheme.Stretch(text.rectTransform);

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return text;
        }

        public void Rebuild(BaseActor tiefling)
        {
            _focusedActor = tiefling;
            _selectedSlot = TieflingImplantBodyViewModel.ResolveDefaultSelection(
                tiefling != null ? tiefling.GetComponent<TieflingImplantsRuntime>() : null);
            _viewModel = TieflingImplantBodyViewModel.Build(tiefling, _selectedSlot);
            RefreshViews();
        }

        void SelectSlot(ImplantSlot slot)
        {
            _selectedSlot = slot;
            if (_focusedActor == null)
                return;

            _viewModel = TieflingImplantBodyViewModel.Build(_focusedActor, _selectedSlot);
            RefreshViews();
        }

        void RefreshViews()
        {
            if (_viewModel == null)
                return;

            _folkBaselineText.text = _viewModel.FolkBaselineText;
            _grid.Rebuild(_viewModel.Cells, _selectedSlot);
            PopulateDetailPane(_viewModel.Detail);

            if (_bodyScroll != null)
                _bodyScroll.verticalNormalizedPosition = 1f;
        }

        void PopulateDetailPane(TieflingImplantDetailModel detail)
        {
            if (detail == null)
                return;

            _heroIcon.sprite = RacialUiTheme.ImprintEmblemSprite;
            _heroIcon.color = detail.Occupied ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            _heroText.text =
                $"<size=22><b>{detail.HeroTitle}</b></size>\n" +
                $"<color=#8a97a3>{detail.HeroSubtitle}</color>";

            _leftColumnText.text = detail.LeftColumnText;
            _rightColumnText.text = detail.RightColumnText;
        }

        sealed class ScrollContentWidthSync : MonoBehaviour
        {
            RectTransform _viewport;
            RectTransform _content;

            public void Initialize(RectTransform viewport, RectTransform content)
            {
                _viewport = viewport;
                _content = content;
            }

            void LateUpdate()
            {
                if (_viewport == null || _content == null)
                    return;

                float width = _viewport.rect.width;
                if (width <= 0f)
                    return;

                _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }
        }
    }
}
