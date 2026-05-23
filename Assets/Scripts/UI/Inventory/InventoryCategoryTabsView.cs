using System;
using System.Collections.Generic;
using JRogue.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    /// <summary>Horizontal category tabs (All + registry categories).</summary>
    public sealed class InventoryCategoryTabsView : MonoBehaviour
    {
        readonly List<Button> _tabButtons = new List<Button>();
        readonly List<TextMeshProUGUI> _tabLabels = new List<TextMeshProUGUI>();
        readonly List<RectTransform> _tabRects = new List<RectTransform>();
        Action<int> _onSelected;
        int _activeIndex;
        ScrollRect _scrollRect;

        public static InventoryCategoryTabsView Create(Transform parent, Action<int> onSelected)
        {
            Transform existing = parent.Find("CategoryTabs");
            InventoryCategoryTabsView view;
            if (existing != null)
            {
                view = existing.GetComponent<InventoryCategoryTabsView>() ??
                       existing.gameObject.AddComponent<InventoryCategoryTabsView>();
                view.EnsureScrollLayout();
            }
            else
            {
                var root = new GameObject("CategoryTabs", typeof(RectTransform));
                root.transform.SetParent(parent, false);
                view = root.AddComponent<InventoryCategoryTabsView>();
                view.EnsureScrollLayout();
            }

            view._onSelected = onSelected;
            return view;
        }

        Transform _content;

        void EnsureScrollLayout()
        {
            var rootLe = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            rootLe.minHeight = 34f;
            rootLe.preferredHeight = 38f;
            rootLe.flexibleWidth = 1f;
            rootLe.minWidth = 120f;

            _scrollRect = GetComponent<ScrollRect>();
            if (_scrollRect == null)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                    Destroy(transform.GetChild(i).gameObject);

                _scrollRect = gameObject.AddComponent<ScrollRect>();
            }

            _scrollRect.horizontal = true;
            _scrollRect.vertical = false;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 30f;
            _scrollRect.inertia = true;
            _scrollRect.decelerationRate = 0.135f;

            Transform viewport = transform.Find("Viewport");
            if (viewport == null)
            {
                var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
                viewportGo.transform.SetParent(transform, false);
                viewport = viewportGo.transform;
                var vpImg = viewportGo.GetComponent<Image>();
                vpImg.color = new Color(0f, 0f, 0f, 0.01f);
            }

            var vpRt = viewport.GetComponent<RectTransform>();
            SetStretch(vpRt);
            _scrollRect.viewport = vpRt;

            Transform content = viewport.Find("Content");
            if (content == null)
            {
                var contentGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                contentGo.transform.SetParent(viewport, false);
                content = contentGo.transform;
            }

            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var hlg = content.GetComponent<HorizontalLayoutGroup>() ?? content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset(4, 4, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;

            var csf = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            _scrollRect.content = contentRt;
            _content = content;

            Scrollbar existingBar = transform.Find("HorizontalScrollbar")?.GetComponent<Scrollbar>();
            if (existingBar == null)
            {
                var barGo = new GameObject("HorizontalScrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image));
                barGo.transform.SetParent(transform, false);
                var barRt = barGo.GetComponent<RectTransform>();
                barRt.anchorMin = new Vector2(0f, 0f);
                barRt.anchorMax = new Vector2(1f, 0f);
                barRt.pivot = new Vector2(0.5f, 0f);
                barRt.sizeDelta = new Vector2(0f, 10f);
                barRt.anchoredPosition = new Vector2(0f, 0f);

                barGo.GetComponent<Image>().color = new Color(0.12f, 0.125f, 0.14f, 0.95f);

                var sliding = new GameObject("Sliding Area", typeof(RectTransform));
                sliding.transform.SetParent(barGo.transform, false);
                SetStretch(sliding.GetComponent<RectTransform>());

                var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
                handle.transform.SetParent(sliding.transform, false);
                var handleRt = handle.GetComponent<RectTransform>();
                handleRt.sizeDelta = new Vector2(20f, 0f);
                handle.GetComponent<Image>().color = new Color(0.35f, 0.4f, 0.46f, 0.95f);

                var bar = barGo.GetComponent<Scrollbar>();
                bar.handleRect = handleRt;
                bar.targetGraphic = handle.GetComponent<Image>();
                bar.direction = Scrollbar.Direction.LeftToRight;
                existingBar = bar;
            }

            _scrollRect.horizontalScrollbar = existingBar;
            _scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        }

        public void Rebuild(IReadOnlyList<ItemCategory> categories, int activeCategoryCycleIndex, float fontScale)
        {
            EnsureScrollLayout();

            if (_content == null)
                _content = transform.Find("Viewport/Content") ?? transform;

            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            _tabButtons.Clear();
            _tabLabels.Clear();
            _tabRects.Clear();
            _activeIndex = activeCategoryCycleIndex;

            AddTab(0, "All", fontScale);
            for (int c = 0; c < categories.Count; c++)
            {
                ItemCategory cat = categories[c];
                string label = ItemCategoryRegistry.Get(cat).HeaderLabel;
                AddTab(c + 1, label, fontScale);
            }

            RefreshTabVisuals();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
            ScrollActiveTabIntoView();
        }

        void AddTab(int cycleIndex, string label, float fontScale)
        {
            var go = new GameObject($"Tab_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_content, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.14f, 0.15f, 0.165f, 0.95f);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 28f;
            le.preferredHeight = 28f;
            le.minWidth = 56f;
            le.preferredWidth = Mathf.Max(56f, label.Length * 8f + 24f);

            var tmpGo = new GameObject("Label", typeof(RectTransform));
            tmpGo.transform.SetParent(go.transform, false);
            var rt = tmpGo.GetComponent<RectTransform>();
            SetStretch(rt);

            var tmp = tmpGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 12f * fontScale;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.82f, 0.86f, 0.9f);
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            int captured = cycleIndex;
            btn.onClick.AddListener(() => _onSelected?.Invoke(captured));

            _tabButtons.Add(btn);
            _tabLabels.Add(tmp);
            _tabRects.Add(go.GetComponent<RectTransform>());
        }

        public void SetActiveIndex(int categoryCycleIndex)
        {
            _activeIndex = categoryCycleIndex;
            RefreshTabVisuals();
            ScrollActiveTabIntoView();
        }

        void ScrollActiveTabIntoView()
        {
            if (_scrollRect == null || _activeIndex < 0 || _activeIndex >= _tabRects.Count)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);

            RectTransform tab = _tabRects[_activeIndex];
            RectTransform viewport = _scrollRect.viewport;
            if (tab == null || viewport == null)
                return;

            Bounds tabBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, tab);
            Bounds viewBounds = new Bounds(viewport.rect.center, viewport.rect.size);

            Vector2 contentPos = _scrollRect.content.anchoredPosition;
            if (tabBounds.min.x < viewBounds.min.x)
                contentPos.x += viewBounds.min.x - tabBounds.min.x;
            else if (tabBounds.max.x > viewBounds.max.x)
                contentPos.x += viewBounds.max.x - tabBounds.max.x;

            _scrollRect.content.anchoredPosition = contentPos;
        }

        void RefreshTabVisuals()
        {
            Color normal = new Color(0.14f, 0.15f, 0.165f, 0.95f);
            Color active = new Color(0.22f, 0.285f, 0.34f, 0.98f);
            Color normalText = new Color(0.78f, 0.82f, 0.86f);
            Color activeText = new Color(0.95f, 0.97f, 1f);

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                bool on = i == _activeIndex;
                if (_tabButtons[i].TryGetComponent(out Image img))
                    img.color = on ? active : normal;
                if (i < _tabLabels.Count)
                    _tabLabels[i].color = on ? activeText : normalText;
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
