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

        [Header("Phase 3 — optional profiles")]
        [SerializeField] InventoryHotkeyProfile phase3HotkeyProfile;

        [SerializeField] InventoryAccessibilitySettings accessibilitySettings;

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
        bool _searchFocusMode;
        bool _inscriptionFocusMode;
        string _inscriptionDraft = string.Empty;

        InventorySortMode _sortMode = InventorySortMode.CategoryThenName;

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

        TextMeshProUGUI _searchPromptText;

        InventoryPartyStripView _partyStrip;
        InventoryCategoryTabsView _categoryTabs;
        Transform _itemListPane;
        InventoryActionsBarView _actionsBar;
        InventoryInspectPaneView _inspectPane;
        Image _encumbranceFill;
        TextMeshProUGUI _titleText;

        bool _footerExpanded;
        Button _footerHelpToggle;
        TextMeshProUGUI _footerCollapsedLine;

        const string FooterExpandedPrefKey = "JRogue.Inv.FooterExpanded";

        public static bool BlocksGameplay =>
            _instance != null && _instance.inventoryPanel != null && _instance.inventoryPanel.activeSelf;

        /// <summary>Inventory is open and a text field (search or inscription) has focus — do not treat ToggleInventory (e.g. <c>i</c>) as closing the panel.</summary>
        public static bool IsOpenInSearchFocus() =>
            _instance != null &&
            _instance.inventoryPanel != null &&
            _instance.inventoryPanel.activeSelf &&
            (_instance._searchFocusMode || _instance._inscriptionFocusMode);

        public bool IsOpen =>
            inventoryPanel != null && inventoryPanel.activeSelf;

        Color EffectiveRowNormal =>
            accessibilitySettings != null && accessibilitySettings.highContrastRows
                ? accessibilitySettings.highContrastRowNormal
                : rowNormalTint;

        Color EffectiveRowSelected =>
            accessibilitySettings != null && accessibilitySettings.highContrastRows
                ? accessibilitySettings.highContrastRowSelected
                : rowSelectedTint;

        float ListFontScale => accessibilitySettings != null ? accessibilitySettings.listAndFooterFontScale : 1f;

        float DetailFontScale => accessibilitySettings != null ? accessibilitySettings.detailPaneFontScale : 1f;

        static bool InCombatContext =>
            CombatThreatCoordinator.Instance != null && CombatThreatCoordinator.Instance.IsInCombat;

        /// <summary>Called by <see cref="JRogue.Input.InputHandler"/> via the PlayerInput action asset (preferred path).</summary>
        public static void TogglePanelFromGameplayInput()
        {
            if (_instance == null)
                return;

            // Search focus uses the same keys as gameplay (e.g. i); never close the panel from Toggle here.
            if (_instance.IsOpen && _instance._searchFocusMode)
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

            _panelImage = inventoryPanel.GetComponent<Image>();
            if (weightText != null)
                _weightBarRoot = weightText.transform.parent;

            ConfigureOuterPanelLayout();
            EnsureTitleBar();
            EnsurePartyStrip();
            NormalizeWeightHeaderLayout();
            EnsureEncumbranceBarFill();
            EnsureSearchPromptLine();
            EnsureCategoryTabs();
            EnsureItemListPaneAndScroll();
            EnsureInventoryBodySplitAndDetails();
            EnsureActionsBar();
            EnsureFooterCollapseChrome();
            EnsureDestructiveModalRoot();
            ReorderChromeChildren();

            _footerExpanded = PlayerPrefs.GetInt(FooterExpandedPrefKey, 0) == 1;

            ApplyFooterCopy();
            ApplyDarkPanelTheme();
            ApplyAccessibilityToUiChrome();
        }

        void ApplyAccessibilityToUiChrome()
        {
            float s = ListFontScale;
            if (footerText != null)
                footerText.fontSize = 12f * s;

            if (weightText != null)
            {
                weightText.fontSize = 16f * s;
                weightText.textWrappingMode = TextWrappingModes.Normal;
            }

            if (_searchPromptText != null)
                _searchPromptText.fontSize = 15f * s;

            if (_inspectPane != null)
            { }

            if (_titleText != null)
                _titleText.fontSize = 18f * ListFontScale;
        }

        void ConfigureOuterPanelLayout()
        {
            if (inventoryPanel == null)
                return;

            if (inventoryPanel.TryGetComponent(out VerticalLayoutGroup outerVlg))
            {
                outerVlg.padding = new RectOffset(12, 12, 12, 12);
                outerVlg.spacing = 6;
                outerVlg.childForceExpandWidth = true;
                outerVlg.childControlWidth = true;
                outerVlg.childControlHeight = true;
            }
        }

        void EnsureTitleBar()
        {
            if (inventoryPanel == null)
                return;

            Transform existing = inventoryPanel.transform.Find("InventoryTitle");
            if (existing != null)
            {
                _titleText = existing.GetComponent<TextMeshProUGUI>();
                return;
            }

            var go = new GameObject("InventoryTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(inventoryPanel.transform, false);
            _titleText = go.GetComponent<TextMeshProUGUI>();
            _titleText.text = "INVENTORY";
            _titleText.fontSize = 18f;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = new Color(0.9f, 0.92f, 0.95f);
            _titleText.alignment = TextAlignmentOptions.MidlineLeft;
            _titleText.margin = new Vector4(4, 0, 0, 4);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 28f;
            le.preferredHeight = 32f;
        }

        void EnsurePartyStrip()
        {
            if (inventoryPanel == null)
                return;

            _partyStrip = InventoryPartyStripView.Create(
                inventoryPanel.transform,
                OnPartyMemberClicked,
                OnBrowseModeToggleClicked);
        }

        void OnPartyMemberClicked(int index)
        {
            _browseMode = BrowseMode.FocusedMember;
            _memberCarouselIndex = index;
            InventoryTelemetry.RecordAction("party_member_click");
            RefreshInventoryDisplay();
        }

        void OnBrowseModeToggleClicked()
        {
            _browseMode = _browseMode == BrowseMode.PartyAggregate
                ? BrowseMode.FocusedMember
                : BrowseMode.PartyAggregate;
            InventoryTelemetry.RecordAction("browse_scope_toggle");
            RefreshInventoryDisplay();
        }

        void OnCategoryTabSelected(int categoryCycleIndex)
        {
            _categoryCycleIndex = categoryCycleIndex;
            InventoryTelemetry.RecordAction("category_tab");
            RefreshInventoryDisplay();
        }

        void EnsureCategoryTabs()
        {
            if (inventoryPanel == null)
                return;

            _categoryTabs = InventoryCategoryTabsView.Create(inventoryPanel.transform, OnCategoryTabSelected);
        }

        void EnsureEncumbranceBarFill()
        {
            if (weightText == null)
                return;

            Transform wt = weightText.transform;
            if (wt.Find("EncumbranceFill") != null)
            {
                _encumbranceFill = wt.Find("EncumbranceFill").GetComponent<Image>();
                return;
            }

            var track = new GameObject("EncumbranceTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(wt, false);
            track.transform.SetAsFirstSibling();
            var trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0f, 0f);
            trackRt.anchorMax = new Vector2(0.35f, 0.35f);
            trackRt.offsetMin = new Vector2(4f, 4f);
            trackRt.offsetMax = new Vector2(-4f, -2f);
            track.GetComponent<Image>().color = new Color(0.12f, 0.125f, 0.14f, 0.95f);

            var fill = new GameObject("EncumbranceFill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            _encumbranceFill = fill.GetComponent<Image>();
            _encumbranceFill.color = new Color(0.35f, 0.72f, 0.48f, 0.95f);
            _encumbranceFill.type = Image.Type.Filled;
            _encumbranceFill.fillMethod = Image.FillMethod.Horizontal;
            _encumbranceFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _encumbranceFill.fillAmount = 0.5f;
        }

        void EnsureItemListPaneAndScroll()
        {
            if (inventoryPanel == null)
                return;

            Transform existingPane = inventoryPanel.transform.Find("InventoryBodyColumns/ItemListPane");
            if (existingPane == null)
                existingPane = inventoryPanel.transform.Find("ItemListPane");

            if (existingPane != null)
            {
                _itemListPane = existingPane;
                ResolveItemContainer();
                if (_itemsScrollContent == null)
                    EnsureItemListScrollView();
                return;
            }

            _itemListPane = new GameObject("ItemListPane", typeof(RectTransform)).transform;
            _itemListPane.SetParent(inventoryPanel.transform, false);

            var le = _itemListPane.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.flexibleHeight = 1f;
            le.minWidth = 120f;

            var v = _itemListPane.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 0;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            InventoryListColumnHeaderView.Create(_itemListPane, ListFontScale);
            EnsureItemListScrollView();
        }

        void EnsureActionsBar()
        {
            if (inventoryPanel == null)
                return;

            _actionsBar = InventoryActionsBarView.Create(
                inventoryPanel.transform,
                () => { InventoryTelemetry.RecordAction("equip_ui"); TryEquipOrUnequipSelection(); },
                () => { InventoryTelemetry.RecordAction("use_ui"); TryUseConsumeStub(); },
                () => { InventoryTelemetry.RecordAction("drop_ui"); BeginDropFlow(); },
                () => { InventoryTelemetry.RecordAction("give_ui"); GiveToStub(); });
        }

        void TryEquipOrUnequipSelection()
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            InventoryViewModel.Row row =
                _presentation.ItemRows[Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1)];

            if (row.IsEquipped)
                TryUnequipSelection();
            else
                TryEquipSelection();
        }

        void EnsureFooterCollapseChrome()
        {
            if (inventoryPanel == null)
                return;

            Transform row = inventoryPanel.transform.Find("FooterRow");
            if (row == null)
            {
                var rowGo = new GameObject("FooterRow", typeof(RectTransform));
                rowGo.transform.SetParent(inventoryPanel.transform, false);
                var rowLe = rowGo.AddComponent<LayoutElement>();
                rowLe.minHeight = 28f;
                rowLe.preferredHeight = _footerExpanded ? 72f : 28f;
                rowLe.flexibleWidth = 1f;

                var h = rowGo.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 8;
                h.padding = new RectOffset(4, 4, 4, 4);
                h.childAlignment = TextAnchor.UpperLeft;
                h.childControlWidth = true;
                h.childForceExpandWidth = false;

                var toggleGo = new GameObject("HelpToggle", typeof(RectTransform), typeof(Image), typeof(Button));
                toggleGo.transform.SetParent(rowGo.transform, false);
                var toggleLe = toggleGo.AddComponent<LayoutElement>();
                toggleLe.minWidth = 32f;
                toggleLe.preferredWidth = 36f;

                toggleGo.GetComponent<Image>().color = new Color(0.16f, 0.17f, 0.19f, 0.95f);
                var toggleLabel = new GameObject("Label", typeof(RectTransform));
                toggleLabel.transform.SetParent(toggleGo.transform, false);
                var tlRt = toggleLabel.GetComponent<RectTransform>();
                tlRt.anchorMin = Vector2.zero;
                tlRt.anchorMax = Vector2.one;
                tlRt.offsetMin = Vector2.zero;
                tlRt.offsetMax = Vector2.zero;
                var tlTmp = toggleLabel.AddComponent<TextMeshProUGUI>();
                tlTmp.text = "?";
                tlTmp.fontSize = 16f;
                tlTmp.alignment = TextAlignmentOptions.Center;
                tlTmp.color = new Color(0.85f, 0.88f, 0.92f);

                _footerHelpToggle = toggleGo.GetComponent<Button>();
                _footerHelpToggle.onClick.AddListener(ToggleFooterExpanded);

                var hintGo = new GameObject("CollapsedHint", typeof(RectTransform), typeof(TextMeshProUGUI));
                hintGo.transform.SetParent(rowGo.transform, false);
                var hintLe = hintGo.AddComponent<LayoutElement>();
                hintLe.flexibleWidth = 1f;
                _footerCollapsedLine = hintGo.GetComponent<TextMeshProUGUI>();
                _footerCollapsedLine.fontSize = 12f;
                _footerCollapsedLine.color = new Color(0.68f, 0.71f, 0.74f);
                _footerCollapsedLine.alignment = TextAlignmentOptions.MidlineLeft;

                if (footerText != null)
                    footerText.transform.SetParent(rowGo.transform, false);
                else
                {
                    var footGo = new GameObject("FooterHints", typeof(RectTransform), typeof(TextMeshProUGUI));
                    footGo.transform.SetParent(rowGo.transform, false);
                    footerText = footGo.GetComponent<TextMeshProUGUI>();
                    footerText.fontSize = 12;
                    footerText.textWrappingMode = TextWrappingModes.Normal;
                    footerText.overflowMode = TextOverflowModes.Overflow;
                    footerText.color = new Color(0.68f, 0.71f, 0.74f);
                    var footerLayout = footGo.AddComponent<LayoutElement>();
                    footerLayout.flexibleWidth = 1f;
                    footerLayout.minHeight = 36;
                }

                row = rowGo.transform;
            }
            else
            {
                _footerHelpToggle = row.Find("HelpToggle")?.GetComponent<Button>();
                _footerCollapsedLine = row.Find("CollapsedHint")?.GetComponent<TextMeshProUGUI>();
                if (_footerHelpToggle != null)
                {
                    _footerHelpToggle.onClick.RemoveAllListeners();
                    _footerHelpToggle.onClick.AddListener(ToggleFooterExpanded);
                }
            }
        }

        void ToggleFooterExpanded()
        {
            _footerExpanded = !_footerExpanded;
            PlayerPrefs.SetInt(FooterExpandedPrefKey, _footerExpanded ? 1 : 0);
            PlayerPrefs.Save();
            ApplyFooterCopy();
        }

        void ReorderChromeChildren()
        {
            if (inventoryPanel == null)
                return;

            int order = 0;
            void Place(Transform t)
            {
                if (t != null && t.parent == inventoryPanel.transform)
                    t.SetSiblingIndex(order++);
            }

            Place(_titleText != null ? _titleText.transform : null);
            Place(_partyStrip != null ? _partyStrip.transform : null);
            if (weightText != null)
                Place(weightText.transform);
            Place(_searchPromptText != null ? _searchPromptText.transform : null);
            Place(_categoryTabs != null ? _categoryTabs.transform : null);
            Place(_bodyColumnsParent != null ? _bodyColumnsParent : _inventoryBodyColumnsRt);
            Place(_actionsBar != null ? _actionsBar.transform : null);
            Transform footerRow = inventoryPanel.transform.Find("FooterRow");
            Place(footerRow);
        }

        void EnsureSearchPromptLine()
        {
            if (_searchPromptText != null || inventoryPanel == null)
                return;

            Transform existing = inventoryPanel.transform.Find("SearchPromptLine");
            if (existing != null)
            {
                _searchPromptText = existing.GetComponent<TextMeshProUGUI>();
                return;
            }

            var go = new GameObject("SearchPromptLine", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(inventoryPanel.transform, false);

            _searchPromptText = go.GetComponent<TextMeshProUGUI>();
            _searchPromptText.richText = false;
            _searchPromptText.fontSize = 15f;
            _searchPromptText.margin = new Vector4(6f, 2f, 6f, 4f);
            _searchPromptText.textWrappingMode = TextWrappingModes.Normal;
            _searchPromptText.overflowMode = TextOverflowModes.Ellipsis;
            _searchPromptText.alignment = TextAlignmentOptions.MidlineLeft;
            _searchPromptText.color = new Color(0.78f, 0.82f, 0.88f);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 26f;
            le.preferredHeight = 30f;
            le.flexibleWidth = 1f;

            if (weightText != null)
                go.transform.SetSiblingIndex(weightText.transform.GetSiblingIndex() + 1);
        }

        void UpdateSearchPromptVisual()
        {
            if (_searchPromptText == null)
                return;

            if (_inscriptionFocusMode)
            {
                string d = _inscriptionDraft ?? string.Empty;
                const int maxInscriptionDisplay = 120;
                if (d.Length > maxInscriptionDisplay)
                    d = d.Substring(0, maxInscriptionDisplay) + "…";

                bool blink = (Mathf.FloorToInt(Time.unscaledTime * 2f) & 1) == 0;
                string caret = blink ? "_" : " ";
                _searchPromptText.text = string.IsNullOrEmpty(d)
                    ? $"Inscription [editing]: {caret}"
                    : $"Inscription [editing]: {d}{caret}";
                _searchPromptText.color = new Color(0.55f, 1f, 0.78f);
                return;
            }

            string q = _plainSearchNeedle ?? string.Empty;
            const int maxDisplay = 120;
            if (q.Length > maxDisplay)
                q = q.Substring(0, maxDisplay) + "…";

            if (_searchFocusMode)
            {
                bool blink = (Mathf.FloorToInt(Time.unscaledTime * 2f) & 1) == 0;
                string caret = blink ? "_" : " ";
                _searchPromptText.text = string.IsNullOrEmpty(q)
                    ? $"Search [editing]: {caret}"
                    : $"Search [editing]: {q}{caret}";
                _searchPromptText.color = new Color(0.55f, 0.82f, 1f);
            }
            else
            {
                _searchPromptText.text = string.IsNullOrEmpty(q)
                    ? "Search: (empty)   Press / to type a filter"
                    : $"Search: {q}   Press / to edit";
                _searchPromptText.color = new Color(0.78f, 0.82f, 0.88f);
            }
        }

        void OnInventoryToggleShortcut()
        {
            if (inventoryPanel == null)
                return;

            bool willOpen = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(willOpen);

            if (IsOpen)
            {
                ApplyInventoryPanelFullScreenLayout();
                InventoryTelemetry.NotifyInventoryOpened();

                InventorySessionPersistence.Load(
                    out int browseMode,
                    out int memberCarousel,
                    out int catIdx,
                    out bool usableOnly,
                    out string needle,
                    out int savedSelection,
                    out InventorySortMode sort);

                _browseMode = Enum.IsDefined(typeof(BrowseMode), browseMode)
                    ? (BrowseMode)browseMode
                    : BrowseMode.FocusedMember;
                _memberCarouselIndex = memberCarousel;
                _categoryCycleIndex = catIdx;
                _usableOnlyFilter = usableOnly;
                _plainSearchNeedle = needle ?? string.Empty;
                _sortMode = sort;
                _selection = savedSelection;
                _searchFocusMode = false;
                _inscriptionFocusMode = false;
                _inscriptionDraft = string.Empty;

                RefreshInventoryDisplay();
            }
            else
            {
                _searchFocusMode = false;
                _inscriptionFocusMode = false;
                SaveInventorySessionState();
                InventoryTelemetry.NotifyInventoryClosed();
            }
        }

        void SaveInventorySessionState()
        {
            InventorySessionPersistence.Save(
                (int)_browseMode,
                _memberCarouselIndex,
                _categoryCycleIndex,
                _usableOnlyFilter,
                _plainSearchNeedle ?? string.Empty,
                _selection,
                _sortMode);
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

            Transform listParent = _itemListPane != null ? _itemListPane : inventoryPanel.transform;
            Transform existing = listParent.Find("ItemListScroll");
            if (existing == null)
                existing = inventoryPanel.transform.Find("ItemListScroll");
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
            scrollGo.transform.SetParent(listParent, false);

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
                ApplyFiftyFiftySplit();
                EnsureInspectPaneOnBodyColumn();
                return;
            }

            if (_itemListPane == null)
                EnsureItemListPaneAndScroll();

            if (_itemListPane != null && scrollTf.parent != _itemListPane)
            {
                _itemListPane.SetParent(inventoryPanel.transform, false);
                scrollTf.SetParent(_itemListPane, false);
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

            if (_itemListPane == null)
                EnsureItemListPaneAndScroll();

            _itemListPane.SetParent(wrapper.transform, false);
            ApplyListPaneLayoutElement(_itemListPane.gameObject);

            _inventoryBodyColumnsRt = hzRt;
            _bodyColumnsParent = wrapper.transform;

            EnsureInspectPaneOnBodyColumn();
            ApplyFiftyFiftySplit();
        }

        static void ApplyListPaneLayoutElement(GameObject listPaneGo)
        {
            LayoutElement le = ScrollLayoutElement(listPaneGo);
            le.flexibleWidth = 1f;
            le.flexibleHeight = 1f;
            le.minWidth = 120f;
            le.preferredWidth = -1f;
        }

        void EnsureInspectPaneOnBodyColumn()
        {
            if (_bodyColumnsParent == null)
                return;

            Transform legacy = _bodyColumnsParent.Find("DetailsPane");
            if (legacy != null && legacy.GetComponent<InventoryInspectPaneView>() == null)
            {
                _detailPane = legacy.GetComponent<TextMeshProUGUI>();
                Destroy(legacy.gameObject);
            }

            _inspectPane = InventoryInspectPaneView.Create(_bodyColumnsParent, _placeholderSprite);
            ApplyListPaneLayoutElement(_inspectPane.gameObject);
        }

        void ApplyFiftyFiftySplit()
        {
            if (_bodyColumnsParent == null)
                return;

            for (int i = 0; i < _bodyColumnsParent.childCount; i++)
            {
                Transform child = _bodyColumnsParent.GetChild(i);
                LayoutElement le = ScrollLayoutElement(child.gameObject);
                le.flexibleWidth = 1f;
                le.flexibleHeight = 1f;
                le.preferredWidth = -1f;
                le.minWidth = 120f;
            }
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

            weightText.fontSize = 16f * ListFontScale;
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
            if (IsOpen)
                SaveInventorySessionState();

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

        void LateUpdate()
        {
            if (!IsOpen || (!_searchFocusMode && !_inscriptionFocusMode))
                return;

            UpdateSearchPromptVisual();
        }

        void ApplyFooterCopy()
        {
            UpdateSearchPromptVisual();

            if (_footerCollapsedLine != null)
                _footerCollapsedLine.text = "Press ? for controls";

            if (!footerText)
                return;

            Transform footerRow = footerText.transform.parent;
            if (footerRow != null && footerRow.TryGetComponent(out LayoutElement footerRowLe))
                footerRowLe.preferredHeight = _footerExpanded ? 88f : 28f;

            footerText.gameObject.SetActive(_footerExpanded);
            if (!_footerExpanded)
                return;

            string catLbl = "(all)";
            if (_categoryCycleIndex > 0 && _categoryCycleIndex <= _categoryCycle.Count)
            {
                ItemCategory picked = _categoryCycle[_categoryCycleIndex - 1];
                catLbl = ItemCategoryRegistry.Get(picked).HeaderLabel;
            }

            BaseActor mb = ResolvedFocusedMemberDisplay();
            string who = mb != null ? mb.DisplayName : "—";

            string searchUi = _searchFocusMode
                ? "<color=#9bbdff>SEARCH</color> (see line above · Backspace clears · Esc exits focus, keeps text)"
                : "<color=#8ae68a>/</color> search focus";

            string sortLbl = _sortMode.ToString();
            string hotkeyNote =
                "<color=#7a8a9a>Hotkeys from profile + PlayerPrefs JRogue.Inv.Key.* (see InventoryHotkeyRuntimeOverrides).</color>";

            footerText.text =
                $"Mode: {_browseMode}   ·   Scope: {( _browseMode == BrowseMode.FocusedMember ? $"Member {who}" : "All party aggregate")}"
                + $"\n[ ] category: {catLbl}   ·   {searchUi}   ·   browse toggle · usable filter · sort: <b>{sortLbl}</b>"
                + "\nNav: ↑↓/WS · item letters · Tab/shift-tab party · [ ] category (profile keys)"
                + "\nActs: Enter/E equip · U unequip · D drop (+confirm) · C use stub · G give stub · X inspect"
                + "\n<color=#9bbdff>Phase 3:</color> ` inscription · 0 cycle sort · Ctrl+1/2/3 fav/prot/junk · "
                + hotkeyNote
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
            if (_inspectPane == null && _detailPane == null)
                return;

            if (_presentation == null || _presentation.ItemRows.Count == 0 ||
                _selection < 0 || _selection >= _presentation.ItemRows.Count)
            {
                if (_inspectPane != null)
                    _inspectPane.SetContent(null,
                        "<color=#6a7380>Select an item row.</color>",
                        string.Empty,
                        DetailFontScale);
                else if (_detailPane != null)
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
            sb.AppendLine(InventoryDetailFormatter.FormatInspectBody(item, sel));
            sb.AppendLine();
            sb.AppendLine(InventoryDetailFormatter.FormatCompareEquippedSameSlot(equippedOther, sel));

            if (_inscriptionFocusMode)
            {
                sb.AppendLine();
                sb.AppendLine(
                    "<color=#8ae68a>Editing inscription</color> — type, <b>Enter</b> save, <b>Esc</b> cancel. "
                    + $"<color=#cfd6dd>{(_inscriptionDraft.Length > 0 ? _inscriptionDraft : "(empty)")}</color>");
            }

            string hero = item != null
                ? InventoryDetailFormatter.FormatHeroTitle(item, sel.Instance) + "\n" +
                  $"<color=#8a97a3>{InventoryDetailFormatter.FormatHeroSubtitle(item, sel)}</color>"
                : string.Empty;

            Sprite icon = item != null ? item.icon : null;

            if (_inspectPane != null)
            {
                _inspectPane.SetContent(icon, hero, sb.ToString(), DetailFontScale);
                return;
            }

            if (_detailPane != null)
            {
                _detailPane.text = hero + "\n\n" + sb;
            }
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
            InventoryTelemetry.RecordAction(delta < 0 ? "category_prev" : "category_next");
            SyncCategoryTabs();
            RefreshInventoryDisplay();
        }

        void SyncCategoryTabs()
        {
            if (_categoryTabs != null)
                _categoryTabs.SetActiveIndex(_categoryCycleIndex);
        }

        bool RequiresConfirmDestructiveDrop(ItemData item, ItemInstance instance)
        {
            if (item == null)
                return false;

            if (instance != null && (instance.UserMarks & ItemUserMark.Protected) != 0)
                return true;

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
            if (!_searchFocusMode)
                return false;

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

            for (int letterIndex = 0; letterIndex < 26; letterIndex++)
            {
                Key key = (Key)((int)Key.A + letterIndex);
                if (!kb[key].wasPressedThisFrame || _plainSearchNeedle.Length >= maxLen)
                    continue;

                char ch = shift ? (char)('A' + letterIndex) : (char)('a' + letterIndex);
                _plainSearchNeedle += ch;
                return true;
            }

            return false;
        }

        static bool MarkModifierHeld(Keyboard kb) =>
            kb[Key.LeftCtrl].isPressed || kb[Key.RightCtrl].isPressed;

        bool TryHandleMarkHotkeys(Keyboard kb)
        {
            if (_searchFocusMode || _inscriptionFocusMode || _presentation == null ||
                _presentation.ItemRows.Count == 0 || !MarkModifierHeld(kb))
                return false;

            Key kFav = InventoryHotkeyRuntimeOverrides.ToggleFavoriteMark(phase3HotkeyProfile);
            Key kProt = InventoryHotkeyRuntimeOverrides.ToggleProtectedMark(phase3HotkeyProfile);
            Key kJunk = InventoryHotkeyRuntimeOverrides.ToggleJunkMark(phase3HotkeyProfile);

            if (kb[kFav].wasPressedThisFrame)
            {
                ToggleMarkOnSelection(ItemUserMark.Favorite);
                return true;
            }

            if (kb[kProt].wasPressedThisFrame)
            {
                ToggleMarkOnSelection(ItemUserMark.Protected);
                return true;
            }

            if (kb[kJunk].wasPressedThisFrame)
            {
                ToggleMarkOnSelection(ItemUserMark.Junk);
                return true;
            }

            return false;
        }

        void ToggleMarkOnSelection(ItemUserMark mark)
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            InventoryViewModel.Row row =
                _presentation.ItemRows[Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1)];

            if (row.Instance == null)
                return;

            row.Instance.ToggleMark(mark);
            InventoryTelemetry.RecordAction($"mark_{mark}");
            RefreshInventoryDisplay();
        }

        void TryBeginInscriptionEdit()
        {
            if (_presentation == null || _presentation.ItemRows.Count == 0)
                return;

            InventoryViewModel.Row row =
                _presentation.ItemRows[Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1)];

            if (row.Instance == null)
                return;

            _inscriptionDraft = row.Instance.UserInscription ?? string.Empty;
            _inscriptionFocusMode = true;
            _searchFocusMode = false;
            InventoryTelemetry.RecordAction("inscription_focus");
        }

        bool HandleInscriptionTyping(Keyboard kb)
        {
            const int maxLen = ItemInstance.MaxInscriptionLength;

            if (kb.backspaceKey.wasPressedThisFrame && _inscriptionDraft.Length > 0)
            {
                _inscriptionDraft = _inscriptionDraft[..^1];
                return true;
            }

            if (kb.spaceKey.wasPressedThisFrame && _inscriptionDraft.Length < maxLen)
            {
                _inscriptionDraft += " ";
                return true;
            }

            if (kb.minusKey.wasPressedThisFrame && _inscriptionDraft.Length < maxLen)
            {
                _inscriptionDraft += "-";
                return true;
            }

            if (kb.periodKey.wasPressedThisFrame && _inscriptionDraft.Length < maxLen)
            {
                _inscriptionDraft += ".";
                return true;
            }

            if (kb.commaKey.wasPressedThisFrame && _inscriptionDraft.Length < maxLen)
            {
                _inscriptionDraft += ",";
                return true;
            }

            if (kb.slashKey.wasPressedThisFrame && _inscriptionDraft.Length < maxLen)
            {
                _inscriptionDraft += "/";
                return true;
            }

            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

            if (kb[Key.Digit1].wasPressedThisFrame && shift && _inscriptionDraft.Length < maxLen)
            {
                _inscriptionDraft += "!";
                return true;
            }

            var digitKeys = new[]
            {
                Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
                Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
            };

            for (int d = 0; d < digitKeys.Length; d++)
            {
                if (d == 1 && shift)
                    continue;

                if (!kb[digitKeys[d]].wasPressedThisFrame || _inscriptionDraft.Length >= maxLen)
                    continue;
                _inscriptionDraft += (char)('0' + d);
                return true;
            }

            for (int letterIndex = 0; letterIndex < 26; letterIndex++)
            {
                Key key = (Key)((int)Key.A + letterIndex);
                if (!kb[key].wasPressedThisFrame || _inscriptionDraft.Length >= maxLen)
                    continue;

                char ch = shift ? (char)('A' + letterIndex) : (char)('a' + letterIndex);
                _inscriptionDraft += ch;
                return true;
            }

            return false;
        }

        void CommitInscriptionDraft()
        {
            if (!_inscriptionFocusMode || _presentation == null || _presentation.ItemRows.Count == 0)
                return;

            InventoryViewModel.Row row =
                _presentation.ItemRows[Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1)];

            if (row.Instance != null)
                row.Instance.UserInscription = _inscriptionDraft ?? string.Empty;

            _inscriptionFocusMode = false;
            _inscriptionDraft = string.Empty;
            InventoryTelemetry.RecordAction("inscription_commit");
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

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (_searchFocusMode)
                {
                    _searchFocusMode = false;
                    ApplyFooterCopy();
                    return;
                }

                if (_inscriptionFocusMode)
                {
                    _inscriptionFocusMode = false;
                    _inscriptionDraft = string.Empty;
                    ApplyFooterCopy();
                    return;
                }

                SaveInventorySessionState();
                inventoryPanel.SetActive(false);
                CancelDestructive();
                _searchFocusMode = false;
                InventoryTelemetry.NotifyInventoryClosed();
                return;
            }

            if (kb.tabKey.wasPressedThisFrame)
            {
                List<BaseActor> party = GatherPartyActors();
                if (party.Count != 0)
                {
                    int dir = kb.leftShiftKey.isPressed ? -1 : 1;
                    _memberCarouselIndex = (_memberCarouselIndex + dir + party.Count) % party.Count;
                    InventoryTelemetry.RecordAction("party_member_tab");
                    RefreshInventoryDisplay();
                }

                return;
            }

            Key browseKey = InventoryHotkeyRuntimeOverrides.ToggleBrowseScope(phase3HotkeyProfile);
            if (kb[browseKey].wasPressedThisFrame && !_searchFocusMode && !_inscriptionFocusMode)
            {
                _browseMode = _browseMode == BrowseMode.PartyAggregate
                    ? BrowseMode.FocusedMember
                    : BrowseMode.PartyAggregate;
                InventoryTelemetry.RecordAction("browse_scope_toggle");
                RefreshInventoryDisplay();
                return;
            }

            Key catPrev = InventoryHotkeyRuntimeOverrides.CategoryPrevious(phase3HotkeyProfile);
            if (kb[catPrev].wasPressedThisFrame && !_inscriptionFocusMode)
            {
                CycleCategoryFilter(-1);
                return;
            }

            Key catNext = InventoryHotkeyRuntimeOverrides.CategoryNext(phase3HotkeyProfile);
            if (kb[catNext].wasPressedThisFrame && !_inscriptionFocusMode)
            {
                CycleCategoryFilter(1);
                return;
            }

            Key sortKey = InventoryHotkeyRuntimeOverrides.CycleSortMode(phase3HotkeyProfile);
            if (kb[sortKey].wasPressedThisFrame && !_searchFocusMode && !_inscriptionFocusMode)
            {
                _sortMode = (InventorySortMode)(((int)_sortMode + 1) % 4);
                InventoryTelemetry.RecordAction("sort_mode_cycle");
                RefreshInventoryDisplay();
                return;
            }

            if (TryHandleMarkHotkeys(kb))
                return;

            if (kb[Key.Backquote].wasPressedThisFrame && !_searchFocusMode)
            {
                if (_inscriptionFocusMode)
                {
                    _inscriptionFocusMode = false;
                    _inscriptionDraft = string.Empty;
                }
                else
                    TryBeginInscriptionEdit();

                ApplyFooterCopy();
                return;
            }

            if (kb.slashKey.wasPressedThisFrame)
            {
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
                {
                    if (!_searchFocusMode && !_inscriptionFocusMode)
                    {
                        ToggleFooterExpanded();
                        return;
                    }
                }

                if (!_searchFocusMode)
                {
                    _inscriptionFocusMode = false;
                    _searchFocusMode = true;
                    ApplyFooterCopy();
                    return;
                }

                if (_plainSearchNeedle.Length < 48)
                {
                    _plainSearchNeedle += "/";
                    RefreshInventoryDisplay();
                }

                ApplyFooterCopy();
                return;
            }

            Key usableKey = InventoryHotkeyRuntimeOverrides.ToggleUsableFilter(phase3HotkeyProfile);
            if (kb[usableKey].wasPressedThisFrame && !_inscriptionFocusMode)
            {
                _usableOnlyFilter = !_usableOnlyFilter;
                InventoryTelemetry.RecordAction("usable_filter_toggle");
                RefreshInventoryDisplay();
                return;
            }

            if (_inscriptionFocusMode)
            {
                if (kb.enterKey.wasPressedThisFrame)
                {
                    CommitInscriptionDraft();
                    RefreshInventoryDisplay();
                    return;
                }

                if (HandleInscriptionTyping(kb))
                    UpdateDetailPane();

                return;
            }

            if (_searchFocusMode)
            {
                bool searchDirty = HandlePlainSearchTyping(kb);
                if (searchDirty)
                    RefreshInventoryDisplay();
                return;
            }

            HandleInventoryCommands(kb);
            PollArrowMovement(kb);
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
                // Only consume the key when it actually jumped to a row; otherwise let search / other handlers see it.
                return idx >= 0;
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
            {
                InventoryTelemetry.RecordAction("equip_try");
                TryEquipSelection();
            }
            else if (kb.uKey.wasPressedThisFrame)
            {
                InventoryTelemetry.RecordAction("unequip_try");
                TryUnequipSelection();
            }
            else if (kb.dKey.wasPressedThisFrame)
            {
                InventoryTelemetry.RecordAction("drop_try");
                BeginDropFlow();
            }
            else if (kb.cKey.wasPressedThisFrame)
            {
                InventoryTelemetry.RecordAction("use_try");
                TryUseConsumeStub();
            }
            else if (kb.gKey.wasPressedThisFrame)
            {
                InventoryTelemetry.RecordAction("give_try");
                GiveToStub();
            }
            else if (kb.xKey.wasPressedThisFrame)
            {
                int i = Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1);
                InventoryTelemetry.RecordAction("inspect");
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
            UpdateActionsBarState();
        }

        void ApplySelectionVisuals()
        {
            for (int i = 0; i < _selectableRowViews.Count; i++)
            {
                bool sel = i == _selection;
                _selectableRowViews[i].SetSelected(sel, EffectiveRowSelected, EffectiveRowNormal);
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

            bool legal = EquipmentLegalityEvaluator.CanEquip(row.Owner.gameObject, row.Instance.Definition,
                row.Item.slotType, out string equipDenyReason);
            if (!legal)
            {
                Debug.LogWarning($"[Inventory] Cannot equip {row.Item.itemName}: {equipDenyReason}");
                return;
            }

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

            if (RequiresConfirmDestructiveDrop(item, snapshot.Instance))
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
            if (itemContainer == null || itemRowPrefab == null)
                return;

            // Party mode uses PartyManager + per-member InventoryManager; playerInventory is only for solo / legacy.
            if (GatherPartyActors().Count == 0 && playerInventory == null)
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
                InCombatContext,
                _sortMode);

            int itemCount = _presentation.ItemRows.Count;
            _selection = Mathf.Clamp(_selection, 0, Mathf.Max(0, itemCount - 1));

            bool showOwnerSubtitle = _browseMode == BrowseMode.PartyAggregate;
            bool hideCategoryHeaders = _categoryCycleIndex > 0;

            foreach (InventoryPresentationModel.PresentationLine line in _presentation.Lines)
            {
                if (line.IsSectionHeader)
                {
                    if (!hideCategoryHeaders)
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
                    showOwnerSubtitle,
                    () => SetSelection(captured),
                    prow.Item ? prow.Item.icon : null,
                    _placeholderSprite,
                    ListFontScale);

                _selectableRowViews.Add(view);
            }

            List<BaseActor> party = GatherPartyActors();
            _partyStrip?.Rebuild(party, _memberCarouselIndex, _browseMode, ListFontScale);
            _categoryTabs?.Rebuild(_categoryCycle, _categoryCycleIndex, ListFontScale);
            SyncCategoryTabs();

            if (_itemListPane != null)
                InventoryListColumnHeaderView.Create(_itemListPane, ListFontScale);

            UpdateActionsBarState();

            ApplyFooterCopy();
            ApplyDarkPanelTheme();
            ApplyAccessibilityToUiChrome();
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

            if (_encumbranceFill != null)
            {
                float ratio = sumCap > 0.01f ? Mathf.Clamp01(sumW / sumCap) : 0f;
                _encumbranceFill.fillAmount = ratio;
                _encumbranceFill.color = ratio > 1f
                    ? new Color(0.9f, 0.32f, 0.32f, 0.95f)
                    : ratio > 0.85f
                        ? new Color(0.95f, 0.72f, 0.28f, 0.95f)
                        : new Color(0.35f, 0.72f, 0.48f, 0.95f);
            }
        }

        void UpdateActionsBarState()
        {
            if (_actionsBar == null || _presentation == null || _presentation.ItemRows.Count == 0)
            {
                _actionsBar?.SetState(false, false, false, false, false, ListFontScale);
                return;
            }

            InventoryViewModel.Row row =
                _presentation.ItemRows[Mathf.Clamp(_selection, 0, _presentation.ItemRows.Count - 1)];

            bool canEquip = !row.IsEquipped && row.CarriedListIndex >= 0 && row.Item != null &&
                            row.Instance != null && row.Owner != null;
            bool canUnequip = row.IsEquipped && row.EquippedSlot.HasValue && row.Owner != null;
            bool canUse = InventoryUsability.AppearsUsableNow(row, InCombatContext);
            bool canDrop = !row.IsEquipped && row.CarriedListIndex >= 0 && row.Owner != null;
            bool canGive = row.Owner != null && row.Item != null;

            _actionsBar.SetState(canEquip, canUnequip, canUse, canDrop, canGive, ListFontScale);
        }
    }
}
