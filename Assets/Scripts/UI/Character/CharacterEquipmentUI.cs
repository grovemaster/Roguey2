using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Party;
using JRogue.UI.Inventory;
using JRogue.UI.Quest;
using JRogue.UI.Proficiency;
using JRogue.UI.Racial;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Character
{
    public sealed class CharacterEquipmentUI : MonoBehaviour
    {
        public static CharacterEquipmentUI Instance { get; private set; }

        static CharacterEquipmentUI _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => EnsureInstance();

        public static CharacterEquipmentUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var existing = FindAnyObjectByType<CharacterEquipmentUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                _instance = existing;
                Instance = existing;
                return existing;
            }

            var go = new GameObject("CharacterEquipmentUI");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CharacterEquipmentUI>();
            Instance = _instance;
            return _instance;
        }

        public static bool BlocksGameplay =>
            _instance != null &&
            _instance._panelRoot != null &&
            _instance._panelRoot.activeSelf;

        public static void ForceCloseIfOpen()
        {
            if (_instance?._panelRoot != null && _instance._panelRoot.activeSelf)
                _instance._panelRoot.SetActive(false);
        }

        public static void TogglePanelFromGameplayInput()
        {
            if (GameOverService.IsGameOver)
                return;

            EnsureInstance().TogglePanel();
        }

        GameObject _panelRoot;
        RacialAbilitiesPartyStripView _partyStrip;
        GameObject _permanentRoot;
        TextMeshProUGUI _permanentText;
        EquipmentSlotGridView _equipmentGrid;
        EssenceSlotPanelView _essencePanel;
        EquipmentDetailPaneView _detailPane;

        readonly List<BaseActor> _partyActors = new List<BaseActor>();
        CharacterEquipmentSheetModel _sheet;
        int _focusedPartyIndex;
        CharacterEquipmentSelection _selection = CharacterEquipmentSelection.None;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUi();
        }

        void Update()
        {
            if (_panelRoot == null || !_panelRoot.activeSelf || Keyboard.current == null)
                return;

            Keyboard kb = Keyboard.current;
            if (kb.escapeKey.wasPressedThisFrame)
            {
                _panelRoot.SetActive(false);
                return;
            }

            if (kb.f1Key.wasPressedThisFrame)
                SetFocusedPartyIndex(0);
            else if (kb.f2Key.wasPressedThisFrame)
                SetFocusedPartyIndex(1);
            else if (kb.f3Key.wasPressedThisFrame)
                SetFocusedPartyIndex(2);
            else if (kb.f4Key.wasPressedThisFrame)
                SetFocusedPartyIndex(3);
            else if (kb.f5Key.wasPressedThisFrame)
                SetFocusedPartyIndex(4);
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("CharacterEquipmentCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _panelRoot = new GameObject("CharacterEquipmentPanel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            _panelRoot.transform.SetParent(canvasGo.transform, false);
            Image panelImage = _panelRoot.GetComponent<Image>();
            panelImage.sprite = RacialUiTheme.PlaceholderSprite;
            panelImage.color = RacialUiTheme.PanelBackground;

            RectTransform panelRt = _panelRoot.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var outer = _panelRoot.AddComponent<VerticalLayoutGroup>();
            outer.padding = new RectOffset(12, 12, 12, 12);
            outer.spacing = 6f;
            outer.childControlWidth = true;
            outer.childControlHeight = true;
            outer.childForceExpandWidth = true;
            outer.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateLayoutText("Title", "CHARACTER · EQUIPMENT",
                RacialUiTheme.TitleFontSize, FontStyles.Bold, 36f);
            title.color = RacialUiTheme.TitleText;

            _partyStrip = RacialAbilitiesPartyStripView.Create(_panelRoot.transform, SetFocusedPartyIndex);

            _permanentRoot = new GameObject("PermanentBonuses", typeof(RectTransform));
            _permanentRoot.transform.SetParent(_panelRoot.transform, false);
            var permanentLe = _permanentRoot.AddComponent<LayoutElement>();
            permanentLe.minHeight = 48f;
            permanentLe.preferredHeight = 72f;
            permanentLe.flexibleWidth = 1f;
            _permanentText = RacialUiTheme.CreateText(
                _permanentRoot.transform,
                "PermanentLabel",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.TopLeft,
                FontStyles.Normal);
            RacialUiTheme.Stretch(_permanentText.rectTransform);
            _permanentText.color = RacialUiTheme.BodyText;
            _permanentRoot.SetActive(false);

            var middleBand = new GameObject("MiddleBand", typeof(RectTransform));
            middleBand.transform.SetParent(_panelRoot.transform, false);
            var middleLe = middleBand.AddComponent<LayoutElement>();
            middleLe.flexibleHeight = 1f;
            middleLe.minHeight = 280f;

            var middleLayout = middleBand.AddComponent<HorizontalLayoutGroup>();
            middleLayout.spacing = 10f;
            middleLayout.childControlWidth = true;
            middleLayout.childControlHeight = true;
            middleLayout.childForceExpandWidth = true;
            middleLayout.childForceExpandHeight = true;

            _equipmentGrid = EquipmentSlotGridView.Create(middleBand.transform, SelectEquipmentSlot);
            var equipLe = _equipmentGrid.GetComponent<LayoutElement>() ??
                          _equipmentGrid.gameObject.AddComponent<LayoutElement>();
            equipLe.flexibleWidth = 0.55f;
            equipLe.flexibleHeight = 1f;

            _essencePanel = EssenceSlotPanelView.Create(middleBand.transform, SelectEssenceSlot);
            var essenceLe = _essencePanel.GetComponent<LayoutElement>() ??
                            _essencePanel.gameObject.AddComponent<LayoutElement>();
            essenceLe.flexibleWidth = 0.45f;
            essenceLe.flexibleHeight = 1f;

            _detailPane = EquipmentDetailPaneView.Create(_panelRoot.transform);

            TextMeshProUGUI hint = CreateLayoutText("Hint",
                "C — character · Esc — close · F1–F5 — focus member",
                RacialUiTheme.FooterFontSize, FontStyles.Normal, 26f);
            hint.color = RacialUiTheme.FooterText;

            _panelRoot.SetActive(false);
        }

        static TextMeshProUGUI CreateLayoutText(string name, string value, float size, FontStyles style, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            TextMeshProUGUI tmp = RacialUiTheme.CreateText(go.transform, "Label", value, size,
                TextAlignmentOptions.MidlineLeft, style);
            RacialUiTheme.Stretch(tmp.rectTransform);
            return tmp;
        }

        void TogglePanel()
        {
            if (_panelRoot == null)
                return;

            if (_panelRoot.activeSelf)
                _panelRoot.SetActive(false);
            else
                Open();
        }

        void Open()
        {
            InventoryUI.ForceCloseIfOpen();
            QuestJournalUI.ForceCloseIfOpen();
            RacialAbilitiesUI.ForceCloseIfOpen();
            ProficienciesUI.ForceCloseIfOpen();

            RefreshPartyActors();
            if (_partyActors.Count == 0)
            {
                _partyStrip.Rebuild(_partyActors, 0);
                _sheet = null;
                _selection = CharacterEquipmentSelection.None;
                _detailPane.Refresh(null, _selection);
            }
            else
            {
                _focusedPartyIndex = ResolveDefaultFocusIndex();
                RefreshSheet();
            }

            _panelRoot.SetActive(true);
        }

        void RefreshPartyActors()
        {
            _partyActors.Clear();
            if (PartyManager.Instance == null)
                return;

            foreach (BaseActor member in PartyManager.Instance.partyMembers)
            {
                if (member != null && member.gameObject.activeInHierarchy)
                    _partyActors.Add(member);
            }
        }

        int ResolveDefaultFocusIndex()
        {
            if (PartyManager.Instance == null)
                return 0;

            BaseActor active = PartyManager.Instance.GetActiveMember();
            if (active == null)
                return 0;

            int index = _partyActors.IndexOf(active);
            return index >= 0 ? index : 0;
        }

        void SetFocusedPartyIndex(int index)
        {
            if (index < 0 || index >= _partyActors.Count)
                return;

            _focusedPartyIndex = index;
            RefreshSheet();
        }

        void SelectEquipmentSlot(EquipmentSlot slot)
        {
            _selection = CharacterEquipmentSelection.ForEquipment(slot);
            RefreshViews();
        }

        void SelectEssenceSlot(int slotIndex)
        {
            _selection = CharacterEquipmentSelection.ForEssence(slotIndex);
            RefreshViews();
        }

        void RefreshSheet()
        {
            BaseActor actor = _partyActors[_focusedPartyIndex];
            _sheet = CharacterEquipmentViewModel.Build(actor);
            _selection = _sheet.DefaultSelection;
            RefreshViews();
        }

        void RefreshViews()
        {
            _partyStrip.Rebuild(_partyActors, _focusedPartyIndex);

            if (_sheet == null)
            {
                if (_permanentRoot != null)
                    _permanentRoot.SetActive(false);
                _detailPane.Refresh(null, CharacterEquipmentSelection.None);
                return;
            }

            RefreshPermanentSection(_sheet);

            EquipmentSlot selectedEquip = _selection.Kind == CharacterEquipmentSelectionKind.Equipment
                ? _selection.EquipmentSlot
                : EquipmentSlot.MainHand;

            int selectedEssence = _selection.Kind == CharacterEquipmentSelectionKind.Essence
                ? _selection.EssenceSlotIndex
                : -1;

            _equipmentGrid.Rebuild(_sheet.EquipmentSlots, selectedEquip);
            _essencePanel.Rebuild(_sheet, selectedEssence);
            _detailPane.Refresh(_sheet, _selection);
        }

        void RefreshPermanentSection(CharacterEquipmentSheetModel sheet)
        {
            if (_permanentRoot == null || _permanentText == null)
                return;

            if (sheet?.PermanentLines == null || sheet.PermanentLines.Count == 0)
            {
                _permanentRoot.SetActive(false);
                _permanentText.text = string.Empty;
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>PERMANENT</b>");
            for (int i = 0; i < sheet.PermanentLines.Count; i++)
                sb.AppendLine($"  {sheet.PermanentLines[i]}");

            _permanentText.text = sb.ToString().TrimEnd();
            _permanentRoot.SetActive(true);

            LayoutElement le = _permanentRoot.GetComponent<LayoutElement>();
            if (le != null)
            {
                float height = 28f + sheet.PermanentLines.Count * 22f;
                le.minHeight = height;
                le.preferredHeight = height;
            }
        }
    }
}
