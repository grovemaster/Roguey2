using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Input;
using JRogue.Manager.Combat;
using JRogue.Manager.Party;
using JRogue.Racial;
using JRogue.UI.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JRogue.UI.Hotbar
{
    public sealed class AbilityHotbarUI : MonoBehaviour
    {
        public static readonly string[] MainSlotKeyLabels =
            { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        static AbilityHotbarUI _instance;

        readonly List<HotbarSlotWidget> _mainSlots = new List<HotbarSlotWidget>();
        readonly List<HotbarSlotWidget> _overflowSlots = new List<HotbarSlotWidget>();

        GameObject _canvasRoot;
        GameObject _barRoot;
        GameObject _overflowRoot;
        TextMeshProUGUI _headerText;
        Button _editButton;
        Button _overflowToggleButton;
        TextMeshProUGUI _overflowToggleLabel;
        Canvas _canvas;
        HotbarTooltipUI _tooltip;
        InputHandler _inputHandler;
        bool _editMode;
        bool _overflowOpen;
        bool _autoEditModeFromOverflow;
        BaseActor _lastActor;

        public static AbilityHotbarUI Instance => _instance;

        public static bool IsEditMode => _instance != null && _instance._editMode;

        public static bool IsOverflowOpen => _instance != null && _instance._overflowOpen;

        public static AbilityHotbarUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(AbilityHotbarUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AbilityHotbarUI>();
            return _instance;
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUi();
            SetOverflowOpen(false);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Update()
        {
            if (_canvasRoot == null)
                return;

            if (_inputHandler == null)
                _inputHandler = Object.FindAnyObjectByType<InputHandler>();

            BaseActor active = PartyManager.Instance?.GetActiveMember();
            if (active != _lastActor)
            {
                _lastActor = active;
                RefreshAll();
            }
            else
            {
                RefreshUsability(active);
            }

            if (_overflowOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetOverflowOpen(false);
        }

        public void RefreshAll()
        {
            BaseActor active = PartyManager.Instance?.GetActiveMember();
            if (active == null)
            {
                if (_headerText != null)
                    _headerText.text = "ABILITY HOTBAR";

                for (int i = 0; i < _mainSlots.Count; i++)
                {
                    BindMainSlot(
                        _mainSlots[i],
                        null,
                        new HotbarEntry(),
                        default,
                        i);
                }

                RebuildOverflow(null, null);
                return;
            }

            HotbarLayout layout = HotbarLayout.EnsureOn(active);
            if (_headerText != null)
                _headerText.text = $"ABILITY HOTBAR — {active.DisplayName}";

            for (int i = 0; i < _mainSlots.Count; i++)
            {
                HotbarEntry entry = layout.GetSlot(i);
                HotbarResolvedAction resolved = HotbarResolver.Resolve(active, entry);
                if (resolved.IsStale && !entry.IsEmpty())
                {
                    layout.SetSlot(i, new HotbarEntry());
                    entry = layout.GetSlot(i);
                    resolved = HotbarResolver.Resolve(active, entry);
                }

                BindMainSlot(_mainSlots[i], active, entry, resolved, i);
            }

            RebuildOverflow(active, layout);
        }

        void RefreshUsability(BaseActor active)
        {
            if (active == null)
                return;

            foreach (HotbarSlotWidget slot in _mainSlots)
                slot.RefreshUsability(active);

            foreach (HotbarSlotWidget slot in _overflowSlots)
                slot.RefreshUsability(active);
        }

        void RebuildOverflow(BaseActor active, HotbarLayout layout)
        {
            ClearOverflowSlots();

            if (active == null || layout == null)
                return;

            List<(HotbarEntry entry, string displayName, string group)> pool =
                HotbarAssignabilityService.BuildPool(active);

            var onMainRow = new HashSet<string>();
            for (int i = 0; i < HotbarLayout.HotbarMainSlotCount; i++)
            {
                HotbarEntry mainEntry = layout.GetSlot(i);
                if (!mainEntry.IsEmpty())
                    onMainRow.Add(mainEntry.EntryKey());
            }

            Transform content = _overflowRoot.transform.Find("Scroll/Viewport/Content");
            if (content == null)
                return;

            string currentGroup = null;
            foreach ((HotbarEntry entry, string displayName, string group) in pool)
            {
                if (group != currentGroup)
                {
                    currentGroup = group;
                    CreateOverflowHeader(content, group);
                }

                bool dimmed = onMainRow.Contains(entry.EntryKey());
                HotbarSlotWidget widget = CreateSlotWidget(content, isMainRow: false);
                HotbarResolvedAction resolved = HotbarResolver.Resolve(active, entry);
                widget.Bind(
                    this,
                    active,
                    entry,
                    resolved,
                    mainSlotIndex: -1,
                    keyLabel: null,
                    dimmedDuplicate: dimmed,
                    displayName: displayName);
                _overflowSlots.Add(widget);
            }
        }

        void BindMainSlot(
            HotbarSlotWidget widget,
            BaseActor actor,
            HotbarEntry entry,
            HotbarResolvedAction resolved,
            int slotIndex)
        {
            widget.Bind(
                this,
                actor,
                entry,
                resolved,
                slotIndex,
                MainSlotKeyLabels[slotIndex],
                dimmedDuplicate: false,
                displayName: ResolveDisplayName(actor, resolved, entry));
        }

        static string ResolveDisplayName(BaseActor actor, HotbarResolvedAction resolved, HotbarEntry entry)
        {
            if (entry.IsEmpty())
                return null;

            if (!string.IsNullOrWhiteSpace(resolved.Ability?.abilityName))
                return resolved.Ability.abilityName.Trim();

            if (resolved.Ability != null && !string.IsNullOrWhiteSpace(resolved.Ability.name))
                return resolved.Ability.name.Trim();

            if (entry.Kind == HotbarEntryKind.ElementalSpiritSummon)
                return ResolveElementalSpiritSummonDisplayName(actor, entry);

            return entry.Kind.ToString();
        }

        static string ResolveElementalSpiritSummonDisplayName(BaseActor actor, HotbarEntry entry)
        {
            if (actor == null || string.IsNullOrEmpty(entry.contractInstanceId))
                return "Spirit";

            ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (contracts == null || !contracts.TryGetPreset(entry.contractInstanceId, out ElementalSpiritContractPreset preset))
                return "Spirit";

            bool summoned = contracts.IsInstanceSummoned(entry.contractInstanceId);
            return ElementalSpiritDisplayNames.BuildSummonHotbarLabel(
                preset,
                contracts.ContractedSpirits,
                summoned);
        }

        public void ToggleEditMode()
        {
            SetEditMode(!_editMode, fromOverflowAuto: false);
        }

        void SetEditMode(bool enabled, bool fromOverflowAuto)
        {
            _editMode = enabled;
            if (!enabled)
                _autoEditModeFromOverflow = false;
            else if (!fromOverflowAuto)
                _autoEditModeFromOverflow = false;

            if (_editButton != null)
            {
                TextMeshProUGUI label = _editButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = _editMode ? "Done" : "Edit";
            }

            RefreshAll();
        }

        public void ToggleOverflow()
        {
            SetOverflowOpen(!_overflowOpen);
        }

        public void SetOverflowOpen(bool open)
        {
            if (open && !_overflowOpen && !_editMode)
            {
                _editMode = true;
                _autoEditModeFromOverflow = true;
                if (_editButton != null)
                {
                    TextMeshProUGUI label = _editButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                        label.text = "Done";
                }
            }
            else if (!open && _autoEditModeFromOverflow)
            {
                _editMode = false;
                _autoEditModeFromOverflow = false;
                if (_editButton != null)
                {
                    TextMeshProUGUI label = _editButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                        label.text = "Edit";
                }
            }

            _overflowOpen = open;
            if (_overflowRoot != null)
                _overflowRoot.SetActive(open);

            if (_overflowToggleLabel != null)
                _overflowToggleLabel.text = open ? "▼" : "▲";

            if (open)
                RefreshAll();
        }

        void TryActivate(HotbarSlotWidget widget)
        {
            if (GameplayModalGate.BlocksFloorGameplay)
                return;

            if (_inputHandler == null)
                _inputHandler = Object.FindAnyObjectByType<InputHandler>();

            if (_inputHandler == null || widget.Actor == null || widget.Entry.IsEmpty())
                return;

            HotbarResolvedAction resolved = HotbarResolver.Resolve(widget.Actor, widget.Entry);
            (bool usable, _, _) = HotbarUsabilityService.Evaluate(widget.Actor, resolved);
            if (!usable)
                return;

            if (widget.MainSlotIndex >= 0)
                _inputHandler.CommandProcessor.TryActivateHotbarMainSlot(widget.MainSlotIndex);
            else
                _inputHandler.CommandProcessor.TryActivateHotbarEntry(widget.Actor, widget.Entry);
        }

        public void AssignEntryToMainSlot(int targetSlotIndex, HotbarEntry entry)
        {
            if (!_editMode || targetSlotIndex < 0 || targetSlotIndex >= HotbarLayout.HotbarMainSlotCount)
                return;

            BaseActor active = PartyManager.Instance?.GetActiveMember();
            if (active == null)
                return;

            if (CombatThreatCoordinator.Instance != null
                && CombatThreatCoordinator.Instance.IsInCombat
                && !entry.IsEmpty()
                && entry.Kind is HotbarEntryKind.InventoryActive or HotbarEntryKind.InventoryUse)
            {
                Debug.Log("[Hotbar] Cannot assign inventory items to the hotbar during combat.");
                return;
            }

            HotbarLayout layout = HotbarLayout.EnsureOn(active);
            layout.SetSlot(targetSlotIndex, entry?.Clone() ?? new HotbarEntry());
            RefreshAll();
        }

        public void SwapMainSlots(int indexA, int indexB)
        {
            if (!_editMode)
                return;

            BaseActor active = PartyManager.Instance?.GetActiveMember();
            HotbarLayout layout = active != null ? HotbarLayout.EnsureOn(active) : null;
            layout?.SwapSlots(indexA, indexB);
            RefreshAll();
        }

        public void ClearMainSlot(int index)
        {
            if (!_editMode)
                return;

            AssignEntryToMainSlot(index, new HotbarEntry());
        }

        void BuildUi()
        {
            var canvasGo = new GameObject(
                "AbilityHotbarCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 45;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _canvasRoot = canvasGo;
            _tooltip = HotbarTooltipUI.EnsureInstance(transform);

            _barRoot = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            _barRoot.transform.SetParent(canvasGo.transform, false);
            RectTransform barRt = (RectTransform)_barRoot.transform;
            barRt.anchorMin = new Vector2(0.5f, 0f);
            barRt.anchorMax = new Vector2(0.5f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.sizeDelta = new Vector2(PlayfieldLayout.HotbarWidthPixels, PlayfieldLayout.HotbarHeightPixels);
            barRt.anchoredPosition = new Vector2(0f, PlayfieldLayout.ConsoleHeightPixels);

            _barRoot.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.88f);

            var headerRow = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            headerRow.transform.SetParent(_barRoot.transform, false);
            RectTransform headerRt = (RectTransform)headerRow.transform;
            StretchTop(headerRt, 30f);
            HorizontalLayoutGroup headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 10, 4, 0);
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandWidth = true;

            _headerText = CreateText(headerRow.transform, "Title", 18f, FontStyles.Bold);
            LayoutElement titleLe = _headerText.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;

            _editButton = CreateSmallButton(headerRow.transform, "Edit", ToggleEditMode);
            _overflowToggleButton = CreateSmallButton(headerRow.transform, "▲", ToggleOverflow);
            _overflowToggleLabel = _overflowToggleButton.GetComponentInChildren<TextMeshProUGUI>();

            var slotRow = new GameObject("Slots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            slotRow.transform.SetParent(_barRoot.transform, false);
            RectTransform slotRowRt = (RectTransform)slotRow.transform;
            slotRowRt.anchorMin = new Vector2(0f, 0f);
            slotRowRt.anchorMax = new Vector2(1f, 1f);
            slotRowRt.offsetMin = new Vector2(10f, 8f);
            slotRowRt.offsetMax = new Vector2(-10f, -32f);

            HorizontalLayoutGroup slotLayout = slotRow.GetComponent<HorizontalLayoutGroup>();
            slotLayout.spacing = 6f;
            slotLayout.childAlignment = TextAnchor.MiddleCenter;
            slotLayout.childControlWidth = slotLayout.childControlHeight = true;
            slotLayout.childForceExpandWidth = slotLayout.childForceExpandHeight = true;

            for (int i = 0; i < HotbarLayout.HotbarMainSlotCount; i++)
            {
                HotbarSlotWidget widget = CreateSlotWidget(slotRow.transform, isMainRow: true);
                widget.MainSlotIndex = i;
                _mainSlots.Add(widget);
            }

            BuildOverflowPanel(canvasGo.transform);
        }

        void BuildOverflowPanel(Transform parent)
        {
            _overflowRoot = new GameObject("Overflow", typeof(RectTransform), typeof(Image));
            _overflowRoot.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)_overflowRoot.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(PlayfieldLayout.HotbarWidthPixels, 280f);
            rt.anchoredPosition = new Vector2(
                0f,
                PlayfieldLayout.ConsoleHeightPixels + PlayfieldLayout.HotbarHeightPixels);

            _overflowRoot.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.1f, 0.95f);

            CreateText(_overflowRoot.transform, "OverflowHint", 14f, FontStyles.Normal).text =
                "Overflow — drag abilities onto 1–0 · click to use · Esc to close";

            var scrollGo = new GameObject(
                "Scroll",
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(Image));
            scrollGo.transform.SetParent(_overflowRoot.transform, false);
            RectTransform scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(8f, 8f);
            scrollRt.offsetMax = new Vector2(-8f, -28f);
            scrollGo.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull((RectTransform)viewport.transform);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
        }

        static void CreateOverflowHeader(Transform parent, string group)
        {
            TextMeshProUGUI text = CreateText(parent, "Group", 14f, FontStyles.Bold);
            text.text = group.ToUpperInvariant();
            text.margin = new Vector4(4f, 8f, 0f, 2f);
        }

        static HotbarSlotWidget CreateSlotWidget(Transform parent, bool isMainRow)
        {
            var go = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            TextMeshProUGUI nameLabel = null;
            Transform widgetHost = go.transform;

            if (isMainRow)
            {
                go.transform.SetParent(parent, false);
                LayoutElement le = go.AddComponent<LayoutElement>();
                le.minWidth = le.minHeight = 72f;
                le.preferredWidth = le.preferredHeight = 80f;

                nameLabel = CreateText(go.transform, "Name", 11f, FontStyles.Normal);
                nameLabel.alignment = TextAlignmentOptions.Bottom;
                nameLabel.enableAutoSizing = true;
                nameLabel.fontSizeMin = 8f;
                nameLabel.fontSizeMax = 11f;
                nameLabel.raycastTarget = false;
                RectTransform nameRt = (RectTransform)nameLabel.transform;
                nameRt.anchorMin = new Vector2(0f, 0f);
                nameRt.anchorMax = new Vector2(1f, 0.38f);
                nameRt.offsetMin = new Vector2(2f, 2f);
                nameRt.offsetMax = new Vector2(-2f, 0f);
            }
            else
            {
                var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image));
                row.transform.SetParent(parent, false);
                LayoutElement rowLe = row.AddComponent<LayoutElement>();
                rowLe.minHeight = 56f;
                row.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

                HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
                h.spacing = 8f;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = false;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = true;

                go.transform.SetParent(row.transform, false);
                LayoutElement le = go.AddComponent<LayoutElement>();
                le.minWidth = le.minHeight = 56f;
                le.preferredWidth = 56f;

                nameLabel = CreateText(row.transform, "Name", 15f, FontStyles.Normal);
                nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
                nameLabel.textWrappingMode = TextWrappingModes.Normal;
                nameLabel.raycastTarget = false;
                LayoutElement nameLe = nameLabel.gameObject.AddComponent<LayoutElement>();
                nameLe.flexibleWidth = 1f;
                nameLe.minHeight = 56f;

                widgetHost = row.transform;
            }

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.11f, 0.14f, 0.95f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            StretchFull((RectTransform)iconGo.transform);
            Image icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI keyLabel = CreateKeyLabel(go.transform);

            var widgetHostGo = widgetHost.gameObject;
            if (widgetHostGo.GetComponent<CanvasGroup>() == null)
                widgetHostGo.AddComponent<CanvasGroup>();

            HotbarSlotWidget widget = widgetHostGo.AddComponent<HotbarSlotWidget>();
            widget.Initialize(icon, bg, keyLabel, nameLabel);
            widget.SetVisualRoot(go.transform);
            return widget;
        }

        static Button CreateSmallButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = 64f;
            le.preferredHeight = 26f;
            go.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.2f, 1f);
            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            TextMeshProUGUI text = CreateText(go.transform, "Label", 14f, FontStyles.Normal);
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            StretchFull((RectTransform)text.transform);
            return button;
        }

        static TextMeshProUGUI CreateKeyLabel(Transform parent)
        {
            var badgeGo = new GameObject("KeyBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(parent, false);
            RectTransform badgeRt = (RectTransform)badgeGo.transform;
            badgeRt.anchorMin = new Vector2(0f, 1f);
            badgeRt.anchorMax = new Vector2(0f, 1f);
            badgeRt.pivot = new Vector2(0f, 1f);
            badgeRt.anchoredPosition = new Vector2(4f, -4f);
            badgeRt.sizeDelta = new Vector2(26f, 22f);

            Image badgeBg = badgeGo.GetComponent<Image>();
            badgeBg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            badgeBg.raycastTarget = false;

            TextMeshProUGUI text = CreateText(badgeGo.transform, "Key", 14f, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            StretchFull((RectTransform)text.transform);
            StyleKeyLabel(text);
            return text;
        }

        static void StyleKeyLabel(TextMeshProUGUI text)
        {
            text.color = Color.white;
            text.outlineWidth = 0.22f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        void ClearOverflowSlots()
        {
            Transform content = _overflowRoot != null
                ? _overflowRoot.transform.Find("Scroll/Viewport/Content")
                : null;

            if (content != null)
            {
                for (int i = content.childCount - 1; i >= 0; i--)
                    Destroy(content.GetChild(i).gameObject);
            }

            _overflowSlots.Clear();
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void StretchTop(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }

        sealed class HotbarSlotWidget :
            MonoBehaviour,
            IPointerClickHandler,
            IPointerEnterHandler,
            IPointerExitHandler,
            IBeginDragHandler,
            IDragHandler,
            IEndDragHandler,
            IDropHandler
        {
            Image _icon;
            Image _frame;
            TextMeshProUGUI _keyLabel;
            TextMeshProUGUI _nameLabel;
            CanvasGroup _canvasGroup;
            AbilityHotbarUI _host;
            ScrollRect _scrollRectWhileDragging;
            Transform _visualRoot;
            static HotbarEntry _dragEntry;
            static int _dragMainIndex = -1;

            public BaseActor Actor { get; private set; }
            public HotbarEntry Entry { get; private set; } = new HotbarEntry();
            public HotbarResolvedAction Resolved { get; private set; }
            public int MainSlotIndex { get; set; } = -1;

            public void Initialize(Image icon, Image frame, TextMeshProUGUI keyLabel, TextMeshProUGUI nameLabel)
            {
                _icon = icon;
                _frame = frame;
                _keyLabel = keyLabel;
                _nameLabel = nameLabel;
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            public void SetVisualRoot(Transform visualRoot) => _visualRoot = visualRoot;

            public void Bind(
                AbilityHotbarUI host,
                BaseActor actor,
                HotbarEntry entry,
                HotbarResolvedAction resolved,
                int mainSlotIndex,
                string keyLabel,
                bool dimmedDuplicate,
                string displayName)
            {
                _host = host;
                Actor = actor;
                Entry = entry ?? new HotbarEntry();
                Resolved = resolved;
                MainSlotIndex = mainSlotIndex;

                if (_keyLabel != null)
                {
                    _keyLabel.text = keyLabel ?? string.Empty;
                    _keyLabel.gameObject.SetActive(!string.IsNullOrEmpty(keyLabel));
                }

                string label = ResolveSlotLabel(displayName, resolved, entry);
                if (_nameLabel != null)
                {
                    _nameLabel.text = label ?? string.Empty;
                    _nameLabel.gameObject.SetActive(!string.IsNullOrEmpty(label));
                }

                if (Entry.IsEmpty())
                {
                    _icon.sprite = null;
                    _icon.color = new Color(0.25f, 0.27f, 0.3f, 0.35f);
                }
                else
                {
                    _icon.sprite = HotbarIconResolver.GetIcon(resolved, actor);
                    _icon.color = Color.white;
                }

                if (dimmedDuplicate)
                    _frame.color = new Color(0.08f, 0.09f, 0.11f, 0.7f);
                else
                    _frame.color = new Color(0.1f, 0.11f, 0.14f, 0.95f);

                if (_visualRoot != null)
                {
                    bool greyFrame = dimmedDuplicate;
                    _visualRoot.GetComponent<Image>().color = greyFrame
                        ? new Color(0.08f, 0.09f, 0.11f, 0.7f)
                        : new Color(0.1f, 0.11f, 0.14f, 0.95f);
                }

                RefreshUsability(actor);
            }

            static string ResolveSlotLabel(string displayName, HotbarResolvedAction resolved, HotbarEntry entry)
            {
                if (entry.IsEmpty())
                    return null;

                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName.Trim();

                if (!string.IsNullOrWhiteSpace(resolved.Ability?.abilityName))
                    return resolved.Ability.abilityName.Trim();

                if (resolved.Ability != null && !string.IsNullOrWhiteSpace(resolved.Ability.name))
                    return resolved.Ability.name.Trim();

                return entry.Kind.ToString();
            }

            public void RefreshUsability(BaseActor actor)
            {
                if (Entry.IsEmpty())
                {
                    _canvasGroup.alpha = 1f;
                    return;
                }

                (bool usable, bool stale, _) = HotbarUsabilityService.Evaluate(actor, Resolved);
                bool grey = !usable || stale;
                _canvasGroup.alpha = grey ? 0.45f : 1f;
                _icon.color = grey ? new Color(0.65f, 0.68f, 0.72f, 1f) : Color.white;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData.button != PointerEventData.InputButton.Left)
                    return;

                if (_host._editMode && eventData.clickCount >= 2 && MainSlotIndex >= 0)
                {
                    _host.ClearMainSlot(MainSlotIndex);
                    return;
                }

                _host.TryActivate(this);
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (Entry.IsEmpty() || HotbarTooltipUI.Instance == null || _host._canvas == null)
                    return;

                string title = !string.IsNullOrWhiteSpace(_nameLabel?.text)
                    ? _nameLabel.text
                    : Resolved.Ability != null
                        ? Resolved.Ability.abilityName
                        : Entry.Kind.ToString();
                string description = Resolved.Ability?.description;
                string footer = HotbarTooltipUI.BuildFooter(
                    Resolved,
                    MainSlotIndex >= 0 ? $"[{MainSlotKeyLabels[MainSlotIndex]}]" : null);

                HotbarTooltipUI.Instance.ShowDelayed(
                    (RectTransform)transform,
                    title,
                    description,
                    footer,
                    _host._canvas);
            }

            public void OnPointerExit(PointerEventData eventData) =>
                HotbarTooltipUI.Instance?.HideImmediate();

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (!_host._editMode || Entry.IsEmpty())
                    return;

                _dragEntry = Entry.Clone();
                _dragMainIndex = MainSlotIndex;
                _scrollRectWhileDragging = GetComponentInParent<ScrollRect>();
                if (_scrollRectWhileDragging != null)
                    _scrollRectWhileDragging.enabled = false;
            }

            public void OnDrag(PointerEventData eventData) { }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (_scrollRectWhileDragging != null)
                {
                    _scrollRectWhileDragging.enabled = true;
                    _scrollRectWhileDragging = null;
                }

                _dragEntry = null;
                _dragMainIndex = -1;
            }

            public void OnDrop(PointerEventData eventData)
            {
                if (!_host._editMode || _dragEntry == null || MainSlotIndex < 0)
                    return;

                if (_dragMainIndex >= 0 && _dragMainIndex != MainSlotIndex)
                    _host.SwapMainSlots(_dragMainIndex, MainSlotIndex);
                else
                    _host.AssignEntryToMainSlot(MainSlotIndex, _dragEntry);
            }
        }
    }
}
