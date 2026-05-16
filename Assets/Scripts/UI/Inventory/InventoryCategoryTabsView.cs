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
        Action<int> _onSelected;
        int _activeIndex;

        public static InventoryCategoryTabsView Create(Transform parent, Action<int> onSelected)
        {
            Transform existing = parent.Find("CategoryTabs");
            InventoryCategoryTabsView view;
            if (existing != null)
            {
                view = existing.GetComponent<InventoryCategoryTabsView>() ??
                       existing.gameObject.AddComponent<InventoryCategoryTabsView>();
            }
            else
            {
                var root = new GameObject("CategoryTabs", typeof(RectTransform));
                root.transform.SetParent(parent, false);

                var le = root.AddComponent<LayoutElement>();
                le.minHeight = 34f;
                le.preferredHeight = 38f;
                le.flexibleWidth = 1f;

                var scroll = root.AddComponent<ScrollRect>();
                scroll.horizontal = true;
                scroll.vertical = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;

                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
                viewport.transform.SetParent(root.transform, false);
                var vpRt = viewport.GetComponent<RectTransform>();
                SetStretch(vpRt);

                var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                content.transform.SetParent(viewport.transform, false);
                var contentRt = content.GetComponent<RectTransform>();
                contentRt.anchorMin = new Vector2(0f, 0f);
                contentRt.anchorMax = new Vector2(0f, 1f);
                contentRt.pivot = new Vector2(0f, 0.5f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = new Vector2(0f, 0f);

                var hlg = content.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 6;
                hlg.padding = new RectOffset(4, 4, 4, 4);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = true;
                hlg.childForceExpandWidth = false;

                var csf = content.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

                scroll.viewport = vpRt;
                scroll.content = contentRt;

                view = root.AddComponent<InventoryCategoryTabsView>();
                view._content = content.transform;
            }

            view._onSelected = onSelected;
            return view;
        }

        Transform _content;

        public void Rebuild(IReadOnlyList<ItemCategory> categories, int activeCategoryCycleIndex, float fontScale)
        {
            if (_content == null)
                _content = transform.Find("Viewport/Content") ?? transform;

            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            _tabButtons.Clear();
            _tabLabels.Clear();
            _activeIndex = activeCategoryCycleIndex;

            AddTab(0, "All", fontScale);
            for (int c = 0; c < categories.Count; c++)
            {
                ItemCategory cat = categories[c];
                string label = ItemCategoryRegistry.Get(cat).HeaderLabel;
                AddTab(c + 1, label, fontScale);
            }

            RefreshTabVisuals();
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
        }

        public void SetActiveIndex(int categoryCycleIndex)
        {
            _activeIndex = categoryCycleIndex;
            RefreshTabVisuals();
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
