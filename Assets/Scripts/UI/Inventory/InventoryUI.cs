using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Combat;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        static InventoryUI _instance;

        public enum BrowseMode
        {
            FocusedMember,
            PartyAggregate
        }

        [Header("System Links")]
        public InventoryManager playerInventory;

        public GameObject inventoryPanel;
        [SerializeField] Transform itemContainer;
        [SerializeField] TextMeshProUGUI weightText;
        [SerializeField] GameObject itemRowPrefab;

        [Header("Optional")]
        [SerializeField] TextMeshProUGUI footerText;
        [SerializeField] DestructiveInventoryActionConfig destructiveActionRules;

        [Header("Dark theme")]
        [SerializeField] Color panelBackgroundColor = new Color(0.08f, 0.085f, 0.095f, 0.96f);

        [SerializeField] Color rowNormalTint = new Color(0.16f, 0.166f, 0.177f, 0.94f);

        [SerializeField] Color rowSelectedTint = new Color(0.22f, 0.285f, 0.34f, 0.96f);

        InventoryPresentationModel _presentation;
        BrowseMode _browseMode = BrowseMode.FocusedMember;
        int _memberCarouselIndex;

        readonly List<ItemCategory> _categoryCycle = ItemCategoryRegistry.CategoriesForFilterCycle().ToList();
        int _categoryCycleIndex;

        bool _usableOnlyFilter;
        string _plainSearchNeedle = string.Empty;

        bool _destructiveBlocking;
        string _destructivePrompt;
        Action _destructiveConfirmed;

        int _selection;
        readonly List<InventoryItemRowView> _selectableRowViews = new List<InventoryItemRowView>();

        Sprite _placeholderSprite;
        Image _panelImage;
        Transform _weightBarRoot;
        TextMeshProUGUI _detailPane;

        Transform _bodyColumnsParent;
        RectTransform _inventoryBodyColumnsRt;

        ScrollRect _itemScrollRect;
        RectTransform _itemsScrollContent;
        GameObject _modalRoot;
        TextMeshProUGUI _modalBody;

        public static bool BlocksGameplay =>
            _instance != null && _instance.inventoryPanel != null && _instance.inventoryPanel.activeSelf;

        public bool IsOpen =>
            inventoryPanel != null && inventoryPanel.activeSelf;

        static bool InCombatContext =>
            CombatThreatCoordinator.Instance != null && CombatThreatCoordinator.Instance.IsInCombat;

        /// <summary>Called by <see cref="JRogue.Input.InputHandler"/> via the PlayerInput action asset (preferred path).</summary>
        public static void TogglePanelFromGameplayInput()
        {
            if (_instance == null)
                return;

            _instance.OnInventoryToggleShortcut();
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"{nameof(InventoryUI)} duplicate on '{gameObject.name}' ignored.");
                enabled = false;
                return;
            }

            _instance = this;

            EnsurePlaceholderSprite();
            ApplyInventoryPanelFullScreenLayout();

            if (!footerText)
            {
                var footGo = new GameObject("FooterHints", typeof(RectTransform), typeof(TextMeshProUGUI));
                footGo.transform.SetParent(inventoryPanel.transform, false);

                footerText = footGo.GetComponent<TextMeshProUGUI>();
                footerText.fontSize = 12;
                footerText.textWrappingMode = TextWrappingModes.Normal;
                footerText.overflowMode = TextOverflowModes.Overflow;
                footerText.margin = new Vector4(0, 10, 0, 10);
                footerText.color = new Color(0.68f, 0.71f, 0.74f);

                LayoutElement footerLayout = footGo.AddComponent<LayoutElement>();
                footerLayout.minHeight = 36;
                footerLayout.preferredHeight = 72;
                footerLayout.flexibleWidth = 1;
            }

            _panelImage = inventoryPanel.GetComponent<Image>();
            if (weightText != null)
                _weightBarRoot = weightText.transform.parent;

            if (inventoryPanel.TryGetComponent<VerticalLayoutGroup>(out var outerVlg))
                outerVlg.childForceExpandWidth = true;

            ResolveItemContainer();
            EnsureItemListScrollView();
            EnsureInventoryBodySplitAndDetails();
            NormalizeWeightHeaderLayout();
            EnsureDestructiveModalRoot();

            ApplyFooterCopy();
            ApplyDarkPanelTheme();
        }

        void OnInventoryToggleShortcut()
        {
            if (inventoryPanel == null)
                return;

            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            if (IsOpen)
            {
                ApplyInventoryPanelFullScreenLayout();
                _selection = 0;
                _categoryCycleIndex = 0;
                _plainSearchNeedle = string.Empty;
                RefreshInventoryDisplay();
            }
        }

        void ResolveItemContainer()
        {
            // Inspector often binds the panel; list rows live under ScrollRect Content instead.
            if (inventoryPanel != null && itemContainer == inventoryPanel.transform)
                itemContainer = null;
        }

        void EnsureItemListScrollView()
        {
            if (_itemsScrollContent != null)
            {
                itemContainer = _itemsScrollContent;
                return;
            }

            Transform existing = inventoryPanel.transform.Find("ItemListScroll");
            if (existing != null)
            {
                _itemScrollRect = existing.GetComponent<ScrollRect>();
                Transform contentTf = existing.Find("Viewport/Content");
                if (_itemScrollRect != null && contentTf != null)
                {
                    if (_itemScrollRect.TryGetComponent<Image>(out var scrollBackdrop))
                        scrollBackdrop.color = new Color(0.06f, 0.062f, 0.07f, 0.92f);

                    _itemsScrollContent = (RectTransform)contentTf;
                    itemContainer = _itemsScrollContent;
                    return;
                }
            }

            var scrollGo = new GameObject("ItemListScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(inventoryPanel.transform, false);

            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.flexibleWidth = 1f;
            scrollLe.minHeight = 120f;

            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0.06f, 0.062f, 0.07f, 0.92f);
            scrollBg.raycastTarget = true;

            _itemScrollRect = scrollGo.GetComponent<ScrollRect>();
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.sizeDelta = Vector2.zero;

            var viewportGo =
                new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            var vpImg = viewportGo.GetComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0f);
            vpImg.raycastTarget = true;

            var contentGo =
                new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _itemsScrollContent = contentGo.GetComponent<RectTransform>();
            _itemsScrollContent.anchorMin = new Vector2(0f, 1f);
            _itemsScrollContent.anchorMax = new Vector2(1f, 1f);
            _itemsScrollContent.pivot = new Vector2(0.5f, 1f);
            _itemsScrollContent.anchoredPosition = Vector2.zero;
            _itemsScrollContent.sizeDelta = Vector2.zero;

            var listVlg = contentGo.GetComponent<VerticalLayoutGroup>();
            listVlg.childAlignment = TextAnchor.UpperCenter;
            listVlg.childControlWidth = true;
            listVlg.childControlHeight = true;
            listVlg.childForceExpandWidth = true;
            listVlg.childForceExpandHeight = false;
            listVlg.spacing = 5;
            listVlg.padding = new RectOffset(0, 0, 0, 0);

            var csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _itemScrollRect.viewport = viewportRt;
            _itemScrollRect.content = _itemsScrollContent;
            _itemScrollRect.horizontal = false;
            _itemScrollRect.vertical = true;
            _itemScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _itemScrollRect.scrollSensitivity = 28f;

            itemContainer = _itemsScrollContent;

            int insertAfterWeight = weightText != null ? weightText.transform.GetSiblingIndex() + 1 : 0;
            scrollGo.transform.SetSiblingIndex(insertAfterWeight);
        }

        void EnsureInventoryBodySplitAndDetails()
        {
            if (_inventoryBodyColumnsRt != null)
                return;

            if (_itemScrollRect == null)
                return;

            Transform scrollTf = _itemScrollRect.transform;
            if (scrollTf.parent != null && scrollTf.parent.name == "InventoryBodyColumns")
            {
                _bodyColumnsParent = scrollTf.parent;
                _inventoryBodyColumnsRt = (RectTransform)_bodyColumnsParent.transform;
                _detailPane = scrollTf.parent.Find("DetailsPane")?.GetComponent<TextMeshProUGUI>();
                return;
            }

            int idx = scrollTf.GetSiblingIndex();
            var wrapper = new GameObject("InventoryBodyColumns", typeof(RectTransform));
            wrapper.transform.SetParent(inventoryPanel.transform, false);

            RectTransform hzRt = wrapper.GetComponent<RectTransform>();
            hzRt.anchorMin = Vector2.zero;
            hzRt.anchorMax = Vector2.one;
            hzRt.sizeDelta = Vector2.zero;

            var hz = wrapper.gameObject.AddComponent<HorizontalLayoutGroup>();
            hz.childAlignment = TextAnchor.MiddleLeft;
            hz.childForceExpandHeight = true;
            hz.spacing = 8;
            hz.padding = new RectOffset(0, 0, 0, 0);
            hz.childControlWidth = true;

            LayoutElement hzLe = wrapper.AddComponent<LayoutElement>();
            hzLe.flexibleHeight = 1f;
            hzLe.flexibleWidth = 1f;
            hzLe.minHeight = 140f;

            wrapper.transform.SetSiblingIndex(idx);

            scrollTf.SetParent(wrapper.transform, false);
            ScrollLayoutElement(scrollTf.gameObject).flexibleWidth = 1f;
            ScrollLayoutElement(scrollTf.gameObject).flexibleHeight = 1f;

            var detailGo = new GameObject("DetailsPane", typeof(RectTransform));
            detailGo.transform.SetParent(wrapper.transform, false);
            _detailPane = detailGo.AddComponent<TextMeshProUGUI>();
            _detailPane.margin = new Vector4(10, 16, 10, 14);
            _detailPane.richText = true;
            _detailPane.fontSize = 13f;
            _detailPane.alignment = TextAlignmentOptions.TopJustified;
            _detailPane.textWrappingMode = TextWrappingModes.Normal;
            _detailPane.overflowMode = TextOverflowModes.Overflow;

            LayoutElement dl = detailGo.AddComponent<LayoutElement>();
            dl.preferredWidth = 270f;
            dl.flexibleWidth = 0f;
            dl.flexibleHeight = 1f;

            RectTransform dlRt = detailGo.GetComponent<RectTransform>();
            dlRt.anchorMin = Vector2.zero;
            dlRt.anchorMax = Vector2.one;
            dlRt.offsetMin = Vector2.zero;
            dlRt.offsetMax = Vector2.zero;

            _inventoryBodyColumnsRt = hzRt;
            _bodyColumnsParent = wrapper.transform;

            _detailPane.text = "<color=#6a7380>Open inventory to browse.</color>";
        }

        static LayoutElement ScrollLayoutElement(GameObject go) =>
            go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();

        void ApplyInventoryPanelFullScreenLayout()
        {
            if (inventoryPanel == null)
                return;

            RectTransform rt = inventoryPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        void EnsureDestructiveModalRoot()
        {
            if (_modalRoot != null || inventoryPanel == null)
                return;

            Transform existing = inventoryPanel.transform.Find("InventoryModal");
            if (existing != null)
            {
                _modalRoot = existing.gameObject;
                _modalBody = ResolveModalBodyTransform(existing);
                ConfigureDestructiveModalChrome();
                return;
            }

            _modalRoot = new GameObject("InventoryModal",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            Transform mt = _modalRoot.transform;
            mt.SetParent(inventoryPanel.transform, false);
            SetStretch((RectTransform)mt);
            LayoutElement blocker = _modalRoot.AddComponent<LayoutElement>();
            blocker.ignoreLayout = true;

            Image dim = _modalRoot.GetComponent<Image>();
            dim.sprite = _placeholderSprite;
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;

            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            bubble.transform.SetParent(mt, false);
            var bubbleRt = (RectTransform)bubble.transform;
            bubbleRt.anchorMin = bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRt.pivot = new Vector2(0.5f, 0.5f);
            bubbleRt.anchoredPosition = Vector2.zero;
            bubbleRt.sizeDelta = new Vector2(480f, 220f);

            Image bubbleImg = bubble.GetComponent<Image>();
            bubbleImg.sprite = _placeholderSprite;
            bubbleImg.color = panelBackgroundColor;
            bubbleImg.raycastTarget = true;

            Outline border = bubble.GetComponent<Outline>() ?? bubble.AddComponent<Outline>();
            border.effectDistance = Vector2.one;
            border.effectColor = new Color(1f, 1f, 1f, 0.06f);

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(bubble.transform, false);
            RectTransform bt = bodyGo.GetComponent<RectTransform>();
            bt.anchorMin = Vector2.zero;
            bt.anchorMax = Vector2.one;
            bt.offsetMin = new Vector2(26, 30);
            bt.offsetMax = new Vector2(-26, -30);

            _modalBody = bodyGo.GetComponent<TextMeshProUGUI>();
            _modalBody.fontSize = 17f;
            _modalBody.margin = Vector4.one * 14f;
            _modalBody.richText = true;
            _modalBody.alignment = TextAlignmentOptions.Center;
            _modalBody.overflowMode = TextOverflowModes.Overflow;
            _modalBody.verticalAlignment = VerticalAlignmentOptions.Middle;

            if (bubble.GetComponent<CanvasGroup>() == null)
                bubble.AddComponent<CanvasGroup>();

            mt.SetAsLastSibling();
            ConfigureDestructiveModalChrome();
        }

        static TextMeshProUGUI ResolveModalBodyTransform(Transform modalRoot)
        {
            return modalRoot.Find("Bubble/Body")?.GetComponent<TextMeshProUGUI>()
                ?? modalRoot.Find("Body")?.GetComponent<TextMeshProUGUI>();
        }

        /// <summary>Dim overlay + confirm bubble (runs after <see cref="_placeholderSprite"/> exists).</summary>
        void ConfigureDestructiveModalChrome()
        {
            if (_modalRoot == null || _placeholderSprite == null)
                return;

            RectTransform modalRt = (RectTransform)_modalRoot.transform;
            SetStretch(modalRt);

            LayoutElement le = _modalRoot.GetComponent<LayoutElement>() ?? _modalRoot.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            if (_modalRoot.TryGetComponent<Image>(out Image dim))
            {
                dim.sprite = _placeholderSprite;
                dim.color = new Color(0f, 0f, 0f, 0.55f);
                dim.raycastTarget = true;
            }

            Transform bubbleTf = _modalRoot.transform.Find("Bubble");
            if (bubbleTf != null && bubbleTf.TryGetComponent(out Image bubbleImg))
            {
                bubbleImg.sprite = _placeholderSprite;
                bubbleImg.color = panelBackgroundColor;
                bubbleImg.raycastTarget = true;

                RectTransform bubbleRt = (RectTransform)bubbleTf;
                bubbleRt.anchorMin = bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
                bubbleRt.pivot = new Vector2(0.5f, 0.5f);
                bubbleRt.anchoredPosition = Vector2.zero;
                if (bubbleRt.sizeDelta.sqrMagnitude < 100f)
                    bubbleRt.sizeDelta = new Vector2(480f, 220f);
            }

            if (_modalBody == null)
                _modalBody = ResolveModalBodyTransform(_modalRoot.transform);

            if (!_destructiveBlocking)
                _modalRoot.SetActive(false);
        }

        static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void NormalizeWeightHeaderLayout()
        {
            if (weightText == null)
                return;

            GameObject go = weightText.gameObject;
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = 56f;
            le.preferredHeight = 74f;
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;

            RectTransform rt = weightText.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 74f);

            weightText.fontSize = 16f;
            weightText.margin = new Vector4(4f, 4f, 4f, 2f);
            weightText.textWrappingMode = TextWrappingModes.Normal;
            weightText.overflowMode = TextOverflowModes.Overflow;
            weightText.verticalAlignment = VerticalAlignmentOptions.Top;
            weightText.horizontalAlignment = HorizontalAlignmentOptions.Left;
        }

        void ScrollSelectedRowIntoView()
        {
            if (_itemScrollRect == null || _selectableRowViews.Count == 0)
                return;

            RectTransform viewport = _itemScrollRect.viewport;
            RectTransform content = _itemScrollRect.content;
            RectTransform row =
                _selectableRowViews[Mathf.Clamp(_selection, 0, _selectableRowViews.Count - 1)]
                    .GetComponent<RectTransform>();

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            Bounds rowInView = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, row);
            float vTop = viewport.rect.yMax;
            float vBottom = viewport.rect.yMin;
            const float pad = 4f;

            float shift = 0f;
            if (rowInView.max.y > vTop - pad)
                shift = rowInView.max.y - (vTop - pad);
            else if (rowInView.min.y < vBottom + pad)
                shift = rowInView.min.y - (vBottom + pad);

            if (Mathf.Abs(shift) < 0.5f)
                return;

            float excess = Mathf.Max(0f, content.rect.height - viewport.rect.height);
            if (excess < 1f)
                return;

            Vector2 ap = content.anchoredPosition;
            ap.y += shift;
            ap.y = Mathf.Clamp(ap.y, -excess, 0f);
            content.anchoredPosition = ap;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void ApplyDarkPanelTheme()
        {
            if (_panelImage != null)
            {
                _panelImage.sprite = _placeholderSprite;
                _panelImage.color = panelBackgroundColor;
            }

            if (weightText != null)
                weightText.color = new Color(0.88f, 0.91f, 0.93f);

            ApplyFooterCopy();
            if (footerText != null)
                footerText.color = new Color(0.7f, 0.735f, 0.76f);

            ConfigureDestructiveModalChrome();
        }

        void EnsurePlaceholderSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _placeholderSprite =
                Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        void ApplyFooterCopy()
        {
            if (!footerText) return;

            string catLbl = "(all)";
            if (_categoryCycleIndex > 0 && _categoryCycleIndex <= _categoryCycle.Count)
            {
                ItemCategory picked = _categoryCycle[_categoryCycleIndex - 1];
                catLbl = ItemCategoryRegistry.Get(picked).HeaderLabel;
            }

            BaseActor mb = ResolvedFocusedMemberDisplay();
            string who = mb != null ? mb.DisplayName : "—";

            footerText.text =
                $"Mode: {_browseMode}   ·   Scope: {( _browseMode == BrowseMode.FocusedMember ? $"Member {who}" : "All party aggregate")}"
                + $"\n[ ] filter: {catLbl}   ·   [ / ] clear search ({(_plainSearchNeedle.Length > 0 ? _plainSearchNeedle : "—")})   ·   Semicolon ';' pivot browse mode   ·   F usable-only ({(_usableOnlyFilter ? "ON" : "off")})"
                + "\nNav: ↑↓/WS · letters · Tab/shift-tab party · [ / ] adj category strip"
                + "\nActs: Enter/E equip · U unequip · D drop (+confirm) · C use/consume stub · G give stub · X log inspect"
                + "\n<color=#6a7a84>In combat: ally bag use-policy enforced; exchanges still stub (initiator consumes turn).</color>";
        }

        BaseActor ResolvedFocusedMemberDisplay()
        {
            List<BaseActor> p = GatherPartyActors();
            if (p.Count == 0 || _browseMode != BrowseMode.FocusedMember)
                return null;
            _memberCarouselIndex = Mathf.Clamp(_memberCarouselIndex, 0, p.Count - 1);
            return p[_memberCarouselIndex];
        }

        static Color ResolveNameTint(ItemData item, bool equipped)
        {
            if (equipped)
                return new Color(0.6f, 0.93f, 1f);

            bool weapon = item.damageModules != null && item.damageModules.Count > 0;
            if (weapon)
                return new Color(0.98f, 0.75f, 0.45f);

            switch (item.slotType)
            {
                case EquipmentSlot.Head:
                case EquipmentSlot.Torso:
                case EquipmentSlot.Legs:
                case EquipmentSlot.Feet:
                    return new Color(0.7f, 0.82f, 1f);

                default:
                    if (item.activeAbilities != null && item.activeAbilities.Count > 0)
                        return new Color(0.65f, 1f, 0.78f);

                    break;
            }

            return new Color(0.86f, 0.895f, 0.92f);
        }

        static void LogInspect(ItemData item)
        {
            if (!item)
            {
                Debug.Log("[Inspect] (no item)");
                return;
            }

            Debug.Log(
                $"[Inspect] <b>{item.itemName}</b> | slot:{item.slotType} | wt:{item.weight:0.#} dmg:{item.damageModules?.Count ?? 0} mods:{item.statModifiers?.Count ?? 0} passives:{item.passiveEffects?.Count ?? 0} actives:{item.activeAbilities?.Count ?? 0}");
        }

        List<BaseActor> GatherPartyActors()
        {
            var list = new List<BaseActor>();

            if (PartyManager.Instance != null)
            {
                foreach (BaseActor m in PartyManager.Instance.partyMembers)
                {
                    if (m != null && m.gameObject.activeInHierarchy)
                        list.Add(m);
                }
                return list;
            }

            if (playerInventory != null)
            {
                BaseActor solo = playerInventory.GetComponent<BaseActor>();
                if (solo != null)
                    list.Add(solo);
            }

            return list;
        }

        InventoryViewModel AcquireRawViewModel()
        {
            List<BaseActor> party = GatherPartyActors();
            _memberCarouselIndex = Mathf.Clamp(_memberCarouselIndex, 0, Mathf.Max(0, party.Count - 1));

            if (party.Count == 0)
                return InventoryViewModel.BuildPartyAggregate(new List<BaseActor>());

            if (_browseMode == BrowseMode.FocusedMember)
                return InventoryViewModel.BuildPartyMember(party, party[_memberCarouselIndex]);

            return InventoryViewModel.BuildPartyAggregate(party);
        }

        void UpdateDetailPane()
        {
            if (_detailPane == null)
                return;

            if (_presentation == null || _presentation.ItemRows.Count == 0 ||
                _selection < 0 || _selection >= _presentation.ItemRows.Count)
            {
                _detailPane.text = "<color=#6a7380>Select an item row.</color>";
                return;
            }

            InventoryViewModel.Row sel = _presentation.ItemRows[_selection];
            ItemData item = sel.Item;
            EquipmentManager eq = sel.Owner?.GetComponent<EquipmentManager>();

            ItemData equippedOther = eq != null
                ? eq.GetEquippedInstance(item.slotType)?.Definition
                : null;

            var sb = new StringBuilder();
            sb.AppendLine(InventoryDetailFormatter.Format(item, sel));
            sb.AppendLine();
            sb.AppendLine(InventoryDetailFormatter.FormatCompareEquippedSameSlot(equippedOther, sel));
            _detailPane.text = sb.ToString();
        }

        ItemCategory? CurrentCategoryFilter()
        {
            if (_categoryCycleIndex <= 0)
                return null;
            int idx = Mathf.Clamp(_categoryCycleIndex - 1, 0, Mathf.Max(0, _categoryCycle.Count - 1));
            return _categoryCycle[idx];
        }

        void CycleCategoryFilter(int delta)
        {
            int modes = Mathf.Max(1, _categoryCycle.Count + 1);
            _categoryCycleIndex = (_categoryCycleIndex + delta + modes) % modes;
            RefreshInventoryDisplay();
        }

        bool RequiresConfirmDestructiveDrop(ItemData item)
        {
            if (item == null)
                return false;

            if (destructiveActionRules != null)
                return destructiveActionRules.ShouldConfirmDrop(item);

            const ItemInventoryRiskHint defaults =
                ItemInventoryRiskHint.StoryTagged |
                ItemInventoryRiskHint.Rare |
                ItemInventoryRiskHint.Cursed |
                ItemInventoryRiskHint.HighValue;

            return (item.inventoryRiskHints & defaults) != 0;
        }

        void BeginDestructive(string message, Action onYes)
        {
            _destructiveBlocking = true;
            _destructivePrompt = message;
            _destructiveConfirmed = onYes;
            ApplyFooterCopy();

            if (_modalRoot != null)
            {
                ConfigureDestructiveModalChrome();
                if (_modalBody != null)
                    _modalBody.text =
                        $"{message}\n<size=14><color=#9bbdff>Y</color> confirm   ·   <color=#ffb28a>N</color> cancel</size>";
                _modalRoot.transform.SetAsLastSibling();
                _modalRoot.SetActive(true);
            }
        }

        void CancelDestructive()
        {
            _destructiveBlocking = false;
            _destructiveConfirmed = null;
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
        }

        void CommitDestructive()
        {
            Action act = _destructiveConfirmed;
            CancelDestructive();
            act?.Invoke();
        }

        bool HandlePlainSearchTyping(Keyboard kb)
        {
            const int maxLen = 48;
            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

            if (kb.spaceKey.wasPressedThisFrame && _plainSearchNeedle.Length < maxLen)
            {
                _plainSearchNeedle += " ";
                return true;
            }

            if (kb.backspaceKey.wasPressedThisFrame && _plainSearchNeedle.Length > 0)
            {
                _plainSearchNeedle = _plainSearchNeedle[..^1];
                return true;
            }

            var digitKeys = new[]
            {
                Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
                Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
            };

            for (int d = 0; d < digitKeys.Length; d++)
            {
                if (!kb[digitKeys[d]].wasPressedThisFrame || _plainSearchNeedle.Length >= maxLen)
                    continue;
                _plainSearchNeedle += (char)('0' + d);
                return true;
            }

            if (shift)
                return false;

            for (int letterIndex = 0; letterIndex < 26; letterIndex++)
            {
                Key key = (Key)((int)Key.A + letterIndex);
                if (!kb[key].wasPressedThisFrame || _plainSearchNeedle.Length >= maxLen)
                    continue;
                _plainSearchNeedle += (char)('a' + letterIndex);
                return true;
            }

            return false;
        }

        void Update()
        {
            if (Keyboard.current == null)
                return;

            Keyboard kb = Keyboard.current;

            if (!IsOpen)
                return;

            if (_destructiveBlocking)
            {
                if (kb.yKey.wasPressedThisFrame)
                    CommitDestructive();
                else if (kb.nKey.wasPressedThisFrame)
                    CancelDestructive();
                return;
            }

            bool searchDirty = HandlePlainSearchTyping(kb);

            if (kb.escapeKey.wasPressedThisFrame)
            {
                inventoryPanel.SetActive(false);
                CancelDestructive();
                return;
            }

            if (kb.tabKey.wasPressedThisFrame)
            {
                List<BaseActor> party = GatherPartyActors();
                if (party.Count != 0)
                {
                    int dir = kb.leftShiftKey.isPressed ? -1 : 1;
                    _memberCarouselIndex = (_memberCarouselIndex + dir + party.Count) % party.Count;
                    RefreshInventoryDisplay();
                }
                else if (searchDirty)
                    RefreshInventoryDisplay();
                return;
            }

            if (kb.semicolonKey.wasPressedThisFrame)
            {
                _browseMode = _browseMode == BrowseMode.PartyAggregate
                    ? BrowseMode.FocusedMember
                    : BrowseMode.PartyAggregate;
                RefreshInventoryDisplay();
                return;
            }

            if (kb.leftBracketKey.wasPressedThisFrame)
            {
                CycleCategoryFilter(-1);
                return;
            }

            if (kb.rightBracketKey.wasPressedThisFrame)
            {
                CycleCategoryFilter(1);
                return;
            }

            if (kb.slashKey.wasPressedThisFrame)
            {
                _plainSearchNeedle = string.Empty;
                RefreshInventoryDisplay();
                return;
            }

            if (kb.fKey.wasPressedThisFrame)
            {
                _usableOnlyFilter = !_usableOnlyFilter;
                RefreshInventoryDisplay();
                return;
            }

            HandleInventoryCommands(kb);
            PollArrowMovement(kb);

            if (searchDirty)
                RefreshInventoryDisplay();
        }

        static int LetterRowIndexExact(IReadOnlyList<InventoryViewModel.Row> rows, char needle)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Letter == needle)
                    return i;
            }

            return -1;
        }

        bool TryConsumeLetterShortcuts(Keyboard kb)
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return false;

            for (int letterIndex = 0; letterIndex < 26; letterIndex++)
            {
                Key key = (Key)((int)Key.A + letterIndex);
                if (!kb[key].wasPressedThisFrame)
                    continue;

                bool shiftHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                char needle = shiftHeld ? (char)('A' + letterIndex) : (char)('a' + letterIndex);
                int idx = LetterRowIndexExact(_presentation.ItemRows, needle);
                if (idx >= 0)
                    SetSelection(idx);
                return true;
            }

            return false;
        }

        void HandleInventoryCommands(Keyboard kb)
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            if (TryConsumeLetterShortcuts(kb))
                return;

            if (kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
                TryEquipSelection();
            else if (kb.uKey.wasPressedThisFrame)
                TryUnequipSelection();
            else if (kb.dKey.wasPressedThisFrame)
                BeginDropFlow();
            else if (kb.cKey.wasPressedThisFrame)
                TryUseConsumeStub();
            else if (kb.gKey.wasPressedThisFrame)
                GiveToStub();
            else if (kb.xKey.wasPressedThisFrame)
            {
                int i = Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1);
                LogInspect(_presentation.ItemRows[i].Item);
            }
        }

        void PollArrowMovement(Keyboard kb)
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            int delta = 0;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                delta = -1;
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                delta = 1;

            if (delta == 0)
                return;

            SetSelection(Mathf.Clamp(_selection + delta, 0, _presentation.ItemRows.Count - 1));
        }

        void SetSelection(int index)
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            _selection = Mathf.Clamp(index, 0, _presentation.ItemRows.Count - 1);
            ApplySelectionVisuals();
            ScrollSelectedRowIntoView();
            UpdateDetailPane();
        }

        void ApplySelectionVisuals()
        {
            for (int i = 0; i < _selectableRowViews.Count; i++)
            {
                bool sel = i == _selection;
                _selectableRowViews[i].SetSelected(sel, rowSelectedTint, rowNormalTint);
            }
        }

        void TryEquipSelection()
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            InventoryViewModel.Row row =
                _presentation.ItemRows[Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1)];

            if (row.IsEquipped || row.CarriedListIndex < 0 || row.Item == null || row.Instance == null ||
                row.Owner == null)
                return;

            EquipmentManager eq = row.Owner.GetComponent<EquipmentManager>();
            eq?.EquipItem(row.Item.slotType, row.Instance);
            RefreshInventoryDisplay();
        }

        void TryUnequipSelection()
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            InventoryViewModel.Row row =
                _presentation.ItemRows[Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1)];

            if (!row.IsEquipped || !row.EquippedSlot.HasValue || row.Owner == null)
                return;

            EquipmentManager eq = row.Owner.GetComponent<EquipmentManager>();
            if (eq == null || !eq.TryUnequipToBag(row.EquippedSlot.Value))
                return;

            RefreshInventoryDisplay();
        }

        void BeginDropFlow()
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            InventoryViewModel.Row snapshot =
                _presentation.ItemRows[Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1)];

            if (snapshot.IsEquipped || snapshot.CarriedListIndex < 0 || snapshot.Owner == null)
                return;

            ItemData item = snapshot.Item;
            Action dropCore = DropCore;

            if (RequiresConfirmDestructiveDrop(item))
                BeginDestructive($"Drop <b>{item.itemName}</b> from <b>{snapshot.OwnerDisplayName}</b>?", dropCore);
            else
                dropCore.Invoke();

            void DropCore()
            {
                InventoryManager inv = snapshot.Owner?.GetComponent<InventoryManager>();
                if (inv == null)
                    return;

                if (snapshot.Instance != null && snapshot.Instance.Quantity > 1)
                    Debug.Log(
                        $"[Inventory Phase2 stub] Partial drop / qty prompt not wired — removing full stack (qty={snapshot.Instance.Quantity}) for {snapshot.Item?.itemName}.");

                if (!inv.TryRemoveCarriedAt(snapshot.CarriedListIndex))
                    return;

                Debug.Log(
                    $"[Drop] Removed {snapshot.Item?.itemName} from {snapshot.OwnerDisplayName}'s bag (world drop Phase 3).");

                RefreshInventoryDisplay();
            }
        }

        void TryUseConsumeStub()
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            InventoryViewModel.Row row =
                _presentation.ItemRows[Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1)];

            if (row.Owner == null || row.Item == null)
                return;

            if (!InventoryUsability.AppearsUsableNow(row, InCombatContext))
            {
                Debug.Log($"[Use] Cannot use <b>{row.Item.itemName}</b> right now.");
                return;
            }

            if (row.Instance != null && row.Instance.Quantity > 1)
                Debug.Log($"[Inventory Phase2 stub] Partial consume qty UI not wired ({row.Instance.Quantity}).");

            Debug.Log($"[Use stub] Consume/activate pathway for <b>{row.Item.itemName}</b> ({row.Owner.DisplayName}).");
        }

        void GiveToStub()
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            InventoryViewModel.Row row =
                _presentation.ItemRows[Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1)];

            if (row.Owner == null)
                return;

            InventoryPolicy.LogCombatTransferStub(row.Owner);

            Debug.Log(
                $"[Give stub] <b>{row.Item?.itemName}</b>; party transfers + turn-cost still Phase 3 (see InventoryPolicy).");
        }

        public void RefreshInventoryDisplay()
        {
            if (itemContainer == null || itemRowPrefab == null || playerInventory == null)
                return;

            ClearItemListChildrenOnly();
            _selectableRowViews.Clear();

            InventoryViewModel raw = AcquireRawViewModel();

            ItemCategory? cat = CurrentCategoryFilter();
            string needle = string.IsNullOrWhiteSpace(_plainSearchNeedle) ? string.Empty : _plainSearchNeedle.Trim();

            _presentation = InventoryPresentationModel.BuildFiltered(
                raw,
                cat,
                needle,
                _usableOnlyFilter,
                InCombatContext);

            int itemCount = _presentation.ItemRows.Count;
            _selection = Mathf.Clamp(_selection, 0, Mathf.Max(0, itemCount - 1));

            foreach (InventoryPresentationModel.PresentationLine line in _presentation.Lines)
            {
                if (line.IsSectionHeader)
                {
                    InventorySectionHeaderView.Create(itemContainer, line.HeaderRichText);
                    continue;
                }

                InventoryViewModel.Row prow = line.Row;

                GameObject rowGo = Instantiate(itemRowPrefab, itemContainer);

                var view = rowGo.GetComponent<InventoryItemRowView>() ?? rowGo.AddComponent<InventoryItemRowView>();
                view.EnsureLayoutBuilt();

                var btn = view.Button;
                btn.transition = Selectable.Transition.None;

                int captured = _selectableRowViews.Count;
                view.Bind(
                    prow,
                    ResolveNameTint(prow.Item, prow.IsEquipped),
                    () => SetSelection(captured),
                    prow.Item ? prow.Item.icon : null,
                    _placeholderSprite);

                _selectableRowViews.Add(view);
            }

            if (footerText != null)
                footerText.transform.SetAsLastSibling();

            ApplyFooterCopy();
            ApplyDarkPanelTheme();
            ApplySelectionVisuals();

            BuildWeightAndCurrencyLine();
            UpdateDetailPane();

            if (_itemScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_itemScrollRect.content);
                _itemScrollRect.verticalNormalizedPosition = 1f;
                Canvas.ForceUpdateCanvases();
                ScrollSelectedRowIntoView();
            }
        }

        void ClearItemListChildrenOnly()
        {
            if (itemContainer == null)
                return;

            if (inventoryPanel != null && itemContainer == inventoryPanel.transform)
            {
                Debug.LogError(
                    $"{nameof(InventoryUI)}: itemContainer points at the panel — refusing to clear. Reassign itemContainer to ScrollRect/Content in the inspector or clear it so Awake can wire it.");
                return;
            }

            for (int i = itemContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = itemContainer.GetChild(i);
                if (footerText && child == footerText.transform)
                    continue;
                Destroy(child.gameObject);
            }
        }

        void BuildWeightAndCurrencyLine()
        {
            if (weightText == null)
                return;

            float sumW = 0f;
            float sumCap = 0f;
            if (PartyManager.Instance != null)
            {
                foreach (BaseActor m in PartyManager.Instance.partyMembers)
                {
                    if (m == null)
                        continue;
                    InventoryManager im = m.GetComponent<InventoryManager>();
                    CharacterStats st = m.GetComponent<CharacterStats>();
                    if (im != null)
                        sumW += im.GetTotalWeight();
                    if (st != null)
                        sumCap += st.EncumbranceLimit;
                }
            }
            else if (playerInventory != null)
            {
                sumW = playerInventory.GetTotalWeight();
                CharacterStats st = playerInventory.GetComponent<CharacterStats>();
                if (st != null)
                    sumCap = st.EncumbranceLimit;
            }

            string line = $"Party weight: {sumW:0.#} / {sumCap:0.#}";
            if (PartyCurrencyLedger.Instance != null)
            {
                foreach (var kv in PartyCurrencyLedger.Instance.Snapshot)
                {
                    if (kv.Key != null && kv.Value > 0)
                        line += $"   ·   {kv.Key.itemName}: {kv.Value}";
                }
            }

            weightText.text = line;
            weightText.color = sumCap > 0 && sumW > sumCap
                ? new Color(1f, 0.35f, 0.35f)
                : new Color(0.88f, 0.91f, 0.93f);
        }
    }
}
