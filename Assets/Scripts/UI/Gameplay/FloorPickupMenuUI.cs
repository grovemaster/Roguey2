using System;
using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Floor;
using JRogue.Manager.Inventory;
using JRogue.Stats;
using JRogue.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    /// <summary>Modal multi-select pickup menu — see Docs/Inventory/Floor-Pickup-And-Auto-Pickup-Requirements.md §7.</summary>
    public sealed class FloorPickupMenuUI : MonoBehaviour
    {
        const int PickupMenuThreshold = 1;
        const float UiScale = 1.35f;
        const int ModalLayoutVersion = 2;

        static FloorPickupMenuUI _instance;
        static TMP_FontAsset _uiFont;

        int _modalLayoutVersion;

        GameObject _modalRoot;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _pickerLineText;
        TextMeshProUGUI _summaryText;
        TextMeshProUGUI _controlsHintText;
        RectTransform _listContent;
        InventoryInspectPaneView _inspectPane;
        readonly List<RowWidgets> _rows = new List<RowWidgets>();

        List<ManualPickupTarget> _targets;
        bool[] _selected;
        BaseActor _picker;
        Vector3Int _tile;
        Action<int> _onClosed;
        int _focusIndex;
        bool _blocking;
        Sprite _placeholderSprite;

        struct RowWidgets
        {
            public GameObject Root;
            public Toggle Toggle;
            public Image Icon;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Qty;
            public TextMeshProUGUI Weight;
            public Image Background;
        }

        public static bool BlocksGameplay => _instance != null && _instance._blocking;

        public static FloorPickupMenuUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(FloorPickupMenuUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FloorPickupMenuUI>();
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
            EnsurePlaceholderSprite();
            EnsureModalBuilt();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Update()
        {
            if (!_blocking || Keyboard.current == null)
                return;

            Keyboard kb = Keyboard.current;
            if (kb.escapeKey.wasPressedThisFrame)
            {
                Cancel();
                return;
            }

            if (kb.upArrowKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame)
                MoveFocus(-1);
            else if (kb.downArrowKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame)
                MoveFocus(1);
            else if (kb.spaceKey.wasPressedThisFrame)
                ToggleFocusedSelection();
            else if (kb.aKey.wasPressedThisFrame)
                SelectAllCarryable();
            else if (kb.enterKey.wasPressedThisFrame)
                ConfirmTakeSelected();
            else if (kb.numpadMultiplyKey.wasPressedThisFrame
                     || (kb.shiftKey.isPressed && kb.digit8Key.wasPressedThisFrame))
                TakeAllCarryable();
        }

        public void Show(
            BaseActor picker,
            Vector3Int tile,
            List<ManualPickupTarget> targets,
            Action<int> onClosed)
        {
            if (targets == null || targets.Count <= PickupMenuThreshold)
                return;

            EnsureModalBuilt();
            _picker = picker;
            _tile = tile;
            _targets = targets;
            _selected = new bool[targets.Count];
            _onClosed = onClosed;
            _focusIndex = 0;
            _blocking = true;

            if (_titleText != null)
                _titleText.text = $"PICK UP — Items at your feet ({tile.x}, {tile.y})";

            RefreshPickerLine();
            RebuildList();
            RefreshSummary();
            RefreshInspectPane();

            Canvas.ForceUpdateCanvases();
            if (_listContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);

            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }

        void RefreshPickerLine()
        {
            if (_pickerLineText == null || _picker == null)
                return;

            CharacterStats stats = _picker.GetComponent<CharacterStats>();
            InventoryManager inv = _picker.GetComponent<InventoryManager>();
            float current = inv != null ? inv.GetTotalWeight() : 0f;
            float max = stats != null ? stats.EncumbranceLimit : 0f;
            _pickerLineText.text =
                $"Picking up as:  <b>{_picker.DisplayName}</b>          Encumbrance:  {current:0.#} / {max}";
            _pickerLineText.fontSize = 18f * UiScale;
        }

        void RebuildList()
        {
            ClearRows();
            if (_listContent == null || _targets == null)
                return;

            InventoryManager inv = _picker != null ? _picker.GetComponent<InventoryManager>() : null;

            for (int i = 0; i < _targets.Count; i++)
            {
                ManualPickupTarget t = _targets[i];
                ItemInstance inst = t.PileEntry?.instance;
                ItemData def = inst?.Definition ?? t.WorldItem?.data;
                if (def == null)
                    continue;

                bool carryable = FloorPickupCoordinator.CanPickupTarget(inv, t);

                RowWidgets row = CreateRow(_listContent, def, inst, carryable, i);
                int index = i;
                row.Toggle.onValueChanged.AddListener(on =>
                {
                    if (!carryable)
                    {
                        row.Toggle.SetIsOnWithoutNotify(false);
                        return;
                    }

                    _selected[index] = on;
                    RefreshSummary();
                });

                row.Root.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _focusIndex = index;
                    RefreshRowFocus();
                    RefreshInspectPane();
                });

                _rows.Add(row);
            }

            if (_targets.Count > 0 && _rows.Count == 0)
                Debug.LogWarning(
                    $"[Pickup] Menu has {_targets.Count} floor target(s) but no list rows were built — check item definitions.");

            RefreshRowFocus();
        }

        void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Root != null)
                    Destroy(_rows[i].Root);
            }

            _rows.Clear();
        }

        RowWidgets CreateRow(Transform parent, ItemData def, ItemInstance inst, bool carryable, int index)
        {
            var root = new GameObject($"Row_{index}", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            root.transform.SetParent(parent, false);

            LayoutElement le = root.GetComponent<LayoutElement>();
            le.minHeight = 48f * UiScale;
            le.preferredHeight = 48f * UiScale;
            le.minWidth = 320f;

            HorizontalLayoutGroup h = root.GetComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 6, 6);
            h.spacing = 12f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0.14f, 0.16f, 0.2f, 0.6f);

            var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
            toggleGo.transform.SetParent(root.transform, false);
            LayoutElement toggleLe = toggleGo.AddComponent<LayoutElement>();
            toggleLe.minWidth = toggleLe.preferredWidth = 36f;
            toggleLe.minHeight = toggleLe.preferredHeight = 36f;

            Toggle toggle = toggleGo.GetComponent<Toggle>();
            toggle.interactable = carryable;
            Image toggleBg = toggleGo.GetComponent<Image>();
            toggleBg.color = new Color(0.2f, 0.22f, 0.28f, 1f);

            var checkGo = new GameObject("Check", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(toggleGo.transform, false);
            Stretch((RectTransform)checkGo.transform);
            Image checkImg = checkGo.GetComponent<Image>();
            checkImg.color = new Color(0.45f, 0.85f, 0.55f, 1f);
            toggle.graphic = checkImg;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            LayoutElement iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.minWidth = iconLe.preferredWidth = 40f;
            iconLe.minHeight = iconLe.preferredHeight = 40f;
            Image icon = iconGo.GetComponent<Image>();
            icon.sprite = def.icon != null ? def.icon : _placeholderSprite;
            icon.preserveAspect = true;
            icon.color = icon.sprite != null ? Color.white : new Color(0.45f, 0.45f, 0.48f, 0.9f);

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(root.transform, false);
            LayoutElement nameLe = nameGo.AddComponent<LayoutElement>();
            nameLe.flexibleWidth = 1f;
            nameLe.minWidth = 120f;
            TextMeshProUGUI nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
            ApplyUiFont(nameTmp);
            nameTmp.fontSize = 17f * UiScale;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            string heavy = carryable ? string.Empty : " <color=#9a6a6a>(too heavy)</color>";
            nameTmp.text = def.itemName + heavy;

            var qtyGo = new GameObject("Qty", typeof(RectTransform), typeof(TextMeshProUGUI));
            qtyGo.transform.SetParent(root.transform, false);
            LayoutElement qtyLe = qtyGo.AddComponent<LayoutElement>();
            qtyLe.minWidth = qtyLe.preferredWidth = 52f;
            TextMeshProUGUI qtyTmp = qtyGo.GetComponent<TextMeshProUGUI>();
            ApplyUiFont(qtyTmp);
            qtyTmp.fontSize = 16f * UiScale;
            qtyTmp.alignment = TextAlignmentOptions.MidlineRight;
            int q = inst != null ? inst.Quantity : 1;
            qtyTmp.text = $"×{q}";

            var weightGo = new GameObject("Weight", typeof(RectTransform), typeof(TextMeshProUGUI));
            weightGo.transform.SetParent(root.transform, false);
            LayoutElement weightLe = weightGo.AddComponent<LayoutElement>();
            weightLe.minWidth = weightLe.preferredWidth = 80f;
            TextMeshProUGUI weightTmp = weightGo.GetComponent<TextMeshProUGUI>();
            ApplyUiFont(weightTmp);
            weightTmp.fontSize = 16f * UiScale;
            weightTmp.alignment = TextAlignmentOptions.MidlineRight;
            if (def.category == ItemCategory.Currency)
                weightTmp.text = "—";
            else
            {
                float w = inst != null ? inst.TotalWeight : def.weight;
                weightTmp.text = $"{w:0.#} kg";
            }

            return new RowWidgets
            {
                Root = root,
                Toggle = toggle,
                Icon = icon,
                Name = nameTmp,
                Qty = qtyTmp,
                Weight = weightTmp,
                Background = bg
            };
        }

        void RefreshRowFocus()
        {
            Color normal = new Color(0.14f, 0.16f, 0.2f, 0.6f);
            Color focused = new Color(0.22f, 0.28f, 0.38f, 0.95f);

            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Background != null)
                    _rows[i].Background.color = i == _focusIndex ? focused : normal;
            }
        }

        void MoveFocus(int delta)
        {
            if (_rows.Count == 0)
                return;

            _focusIndex = Mathf.Clamp(_focusIndex + delta, 0, _rows.Count - 1);
            RefreshRowFocus();
            RefreshInspectPane();
        }

        void ToggleFocusedSelection()
        {
            if (_focusIndex < 0 || _focusIndex >= _rows.Count)
                return;

            RowWidgets row = _rows[_focusIndex];
            if (!row.Toggle.interactable)
                return;

            row.Toggle.isOn = !row.Toggle.isOn;
        }

        void SelectAllCarryable()
        {
            for (int i = 0; i < _rows.Count && i < _selected.Length; i++)
            {
                if (!_rows[i].Toggle.interactable)
                    continue;

                _rows[i].Toggle.isOn = true;
            }
        }

        void TakeAllCarryable()
        {
            int picked = FloorPickupCoordinator.AttemptPickupAllCarryable(_targets, _picker);
            Close(picked);
        }

        void ConfirmTakeSelected()
        {
            int picked = FloorPickupCoordinator.AttemptPickupBatch(_targets, _selected, _picker);
            Close(picked);
        }

        void Cancel() => Close(0);

        void Close(int pickedCount)
        {
            _blocking = false;
            Action<int> cb = _onClosed;
            _onClosed = null;
            _targets = null;
            _selected = null;
            ClearRows();

            if (_modalRoot != null)
                _modalRoot.SetActive(false);

            cb?.Invoke(pickedCount);
        }

        void RefreshSummary()
        {
            if (_summaryText == null || _targets == null || _selected == null)
                return;

            int count = 0;
            float kg = 0f;
            InventoryManager inv = _picker != null ? _picker.GetComponent<InventoryManager>() : null;

            for (int i = 0; i < _targets.Count && i < _selected.Length; i++)
            {
                if (!_selected[i])
                    continue;

                ItemInstance inst = _targets[i].PileEntry?.instance;
                ItemData def = inst?.Definition ?? _targets[i].WorldItem?.data;
                if (def == null)
                    continue;

                if (inst != null && inv != null && !inv.CanCarry(inst))
                    continue;

                count++;
                if (def.category != ItemCategory.Currency)
                    kg += inst != null ? inst.TotalWeight : def.weight;
            }

            _summaryText.text = count > 0
                ? $"{count} selected · +{kg:0.#} kg"
                : "0 selected";
        }

        void RefreshInspectPane()
        {
            if (_inspectPane == null || _targets == null || _focusIndex < 0 || _focusIndex >= _targets.Count)
                return;

            ManualPickupTarget t = _targets[_focusIndex];
            ItemInstance inst = t.PileEntry?.instance;
            ItemData def = inst?.Definition ?? t.WorldItem?.data;
            if (def == null)
            {
                _inspectPane.SetContent(null, string.Empty, string.Empty, 1f);
                return;
            }

            if (inst == null && t.WorldItem?.data != null)
                inst = new ItemInstance(t.WorldItem.data);

            var row = new InventoryViewModel.Row(
                'a',
                inst,
                _picker,
                _picker != null ? _picker.DisplayName : string.Empty,
                isEquipped: false,
                equippedSlot: null,
                carriedListIndex: -1,
                stackedWeight: inst != null ? inst.TotalWeight : def.weight);

            EquipmentManager eq = _picker != null ? _picker.GetComponent<EquipmentManager>() : null;
            ItemData equippedOther = eq != null
                ? eq.GetEquippedInstance(def.slotType)?.Definition
                : null;

            var sb = new StringBuilder();
            sb.AppendLine(InventoryDetailFormatter.FormatInspectBody(def, row));
            sb.AppendLine();
            sb.AppendLine(
                $"<color=#8a97a3>Location:</color> <color=#9aabbe>On ground</color> · ({_tile.x}, {_tile.y})");
            sb.AppendLine();
            sb.AppendLine(InventoryDetailFormatter.FormatCompareEquippedSameSlot(equippedOther, row));

            InventoryManager inv = _picker != null ? _picker.GetComponent<InventoryManager>() : null;
            if (inst != null && inv != null && !inv.CanCarry(inst))
            {
                CharacterStats stats = _picker.GetComponent<CharacterStats>();
                float after = inv.GetTotalWeight() + inst.TotalWeight;
                float max = stats != null ? stats.EncumbranceLimit : 0f;
                sb.AppendLine();
                sb.AppendLine($"<color=#c45a7a>⚠ Too heavy for {_picker.DisplayName} ({after:0.#}/{max})</color>");
            }

            string hero = InventoryDetailFormatter.FormatHeroTitle(def, inst) + "\n" +
                          $"<color=#8a97a3>{InventoryDetailFormatter.FormatHeroSubtitle(def, row)}</color>";

            _inspectPane.SetContent(def.icon, hero, sb.ToString(), UiScale);
        }

        static void ApplyUiFont(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            if (_uiFont == null)
            {
                _uiFont = TMP_Settings.defaultFontAsset;
                if (_uiFont == null)
                {
                    TextMeshProUGUI existing = UnityEngine.Object.FindAnyObjectByType<TextMeshProUGUI>();
                    if (existing != null)
                        _uiFont = existing.font;
                }
            }

            if (_uiFont != null)
                tmp.font = _uiFont;
        }

        void EnsurePlaceholderSprite()
        {
            if (_placeholderSprite != null)
                return;

            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.35f, 0.38f, 0.42f, 1f));
            tex.Apply();
            _placeholderSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        void TearDownModal()
        {
            if (_modalRoot == null)
                return;

            Transform canvas = _modalRoot.transform.parent;
            if (canvas != null)
                Destroy(canvas.gameObject);
            else
                Destroy(_modalRoot);

            _modalRoot = null;
            _listContent = null;
            _titleText = null;
            _pickerLineText = null;
            _summaryText = null;
            _controlsHintText = null;
            _inspectPane = null;
            _modalLayoutVersion = 0;
        }

        void EnsureModalBuilt()
        {
            if (_modalRoot != null && _modalLayoutVersion == ModalLayoutVersion)
                return;

            TearDownModal();

            var canvasGo = new GameObject("FloorPickupMenuCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 510;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _modalRoot = new GameObject("Modal", typeof(RectTransform), typeof(Image));
            _modalRoot.transform.SetParent(canvasGo.transform, false);
            RectTransform modalRt = (RectTransform)_modalRoot.transform;
            modalRt.anchorMin = Vector2.zero;
            modalRt.anchorMax = Vector2.one;
            modalRt.offsetMin = Vector2.zero;
            modalRt.offsetMax = Vector2.zero;
            _modalRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(_modalRoot.transform, false);
            RectTransform panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = new Vector2(0.06f, 0.06f);
            panelRt.anchorMax = new Vector2(0.94f, 0.94f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.1f, 0.11f, 0.14f, 0.98f);

            VerticalLayoutGroup panelV = panel.GetComponent<VerticalLayoutGroup>();
            panelV.padding = new RectOffset(28, 28, 22, 22);
            panelV.spacing = 14f;
            panelV.childControlWidth = true;
            panelV.childForceExpandWidth = true;

            var titleRow = new GameObject("TitleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            titleRow.transform.SetParent(panel.transform, false);
            HorizontalLayoutGroup titleH = titleRow.GetComponent<HorizontalLayoutGroup>();
            titleH.childAlignment = TextAnchor.MiddleLeft;
            titleH.childControlWidth = true;
            titleH.childForceExpandWidth = true;

            _titleText = CreateLine(titleRow.transform, 26f * UiScale, FontStyles.Bold);
            LayoutElement titleLe = _titleText.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;

            CreateTitleCloseButton(titleRow.transform);

            _pickerLineText = CreateLine(panel.transform, 18f * UiScale, FontStyles.Normal);

            var body = new GameObject("Body", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            body.transform.SetParent(panel.transform, false);
            LayoutElement bodyLe = body.GetComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1f;
            bodyLe.minHeight = 480f;

            HorizontalLayoutGroup bodyH = body.GetComponent<HorizontalLayoutGroup>();
            bodyH.spacing = 12f;
            bodyH.childControlWidth = true;
            bodyH.childForceExpandWidth = true;
            bodyH.childForceExpandHeight = true;

            var listCol = new GameObject("ListColumn", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            listCol.transform.SetParent(body.transform, false);
            LayoutElement listLe = listCol.GetComponent<LayoutElement>();
            listLe.flexibleWidth = 1f;
            listLe.minWidth = 420f;

            VerticalLayoutGroup listV = listCol.GetComponent<VerticalLayoutGroup>();
            listV.spacing = 6f;
            listV.childControlWidth = true;

            CreateLine(listCol.transform, 16f * UiScale, FontStyles.Bold, "PICKUP LIST");

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image),
                typeof(LayoutElement));
            scrollGo.transform.SetParent(listCol.transform, false);
            LayoutElement scrollLe = scrollGo.GetComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.flexibleWidth = 1f;
            scrollLe.minHeight = 360f;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scrollGo.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.09f, 0.95f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            RectTransform vpRt = (RectTransform)viewport.transform;
            Stretch(vpRt);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            _listContent = content.GetComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0f, 1f);
            _listContent.anchorMax = new Vector2(1f, 1f);
            _listContent.pivot = new Vector2(0.5f, 1f);
            _listContent.anchoredPosition = Vector2.zero;
            _listContent.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup contentV = content.GetComponent<VerticalLayoutGroup>();
            contentV.padding = new RectOffset(4, 4, 4, 4);
            contentV.spacing = 6f;
            contentV.childControlWidth = true;
            contentV.childControlHeight = true;
            contentV.childForceExpandWidth = true;
            contentV.childForceExpandHeight = false;

            ContentSizeFitter contentCsf = content.GetComponent<ContentSizeFitter>();
            contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRt;
            scroll.content = _listContent;

            _summaryText = CreateLine(listCol.transform, 15f * UiScale, FontStyles.Italic);

            var examineCol = new GameObject("ExamineColumn", typeof(RectTransform), typeof(LayoutElement));
            examineCol.transform.SetParent(body.transform, false);
            LayoutElement examineLe = examineCol.GetComponent<LayoutElement>();
            examineLe.flexibleWidth = 1f;
            examineLe.minWidth = 380f;
            _inspectPane = InventoryInspectPaneView.Create(examineCol.transform, _placeholderSprite);

            var footer = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            footer.transform.SetParent(panel.transform, false);
            HorizontalLayoutGroup footerH = footer.GetComponent<HorizontalLayoutGroup>();
            footerH.spacing = 12f;
            footerH.childAlignment = TextAnchor.MiddleCenter;

            CreateFooterButton(footer.transform, "Take All (*)", TakeAllCarryable);
            CreateFooterButton(footer.transform, "Take Selected (Enter)", ConfirmTakeSelected);
            CreateFooterButton(footer.transform, "Select All (A)", SelectAllCarryable);
            CreateFooterButton(footer.transform, "Cancel (Esc)", Cancel);

            _controlsHintText = CreateLine(panel.transform, 14f * UiScale, FontStyles.Normal);
            _controlsHintText.color = new Color(0.65f, 0.72f, 0.8f, 1f);
            _controlsHintText.lineSpacing = 2f;
            _controlsHintText.text = BuildControlsHintText();

            _modalRoot.SetActive(false);
            _modalLayoutVersion = ModalLayoutVersion;
        }

        static string BuildControlsHintText() =>
            "<b>Controls</b>\n" +
            "↑ ↓  or  j / k — move focus (updates examine pane)\n" +
            "Space — toggle check on focused row\n" +
            "Enter — pick up checked items (Take Selected)\n" +
            "* — pick up all carryable items on this tile (Take All)\n" +
            "A — check all carryable rows (Select All); does not pick up until Enter or *\n" +
            "Esc — close menu without spending a turn\n" +
            "<size=11><i>Browsing does not consume a turn until you Take All or Take Selected.</i></size>";

        static TextMeshProUGUI CreateLine(Transform parent, float size, FontStyles style, string text = null)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            ApplyUiFont(tmp);
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            if (!string.IsNullOrEmpty(text))
                tmp.text = text;
            return tmp;
        }

        void CreateTitleCloseButton(Transform parent)
        {
            var go = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.3f, 1f);
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 44f;
            le.minHeight = le.preferredHeight = 44f;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            ApplyUiFont(tmp);
            tmp.text = "×";
            tmp.fontSize = 28f * UiScale;
            tmp.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)textGo.transform);

            go.GetComponent<Button>().onClick.AddListener(Cancel);
        }

        void CreateFooterButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.3f, 1f);
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minWidth = 180f;
            le.preferredHeight = 44f;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            ApplyUiFont(tmp);
            tmp.text = label;
            tmp.fontSize = 16f * UiScale;
            tmp.alignment = TextAlignmentOptions.Center;
            RectTransform textRt = (RectTransform)textGo.transform;
            Stretch(textRt);

            go.GetComponent<Button>().onClick.AddListener(onClick);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
