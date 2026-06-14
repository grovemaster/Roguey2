using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.UI.Character;
using JRogue.UI.Inventory;
using JRogue.UI.Quest;
using JRogue.UI.Racial;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Proficiency
{
    public sealed class ProficienciesUI : MonoBehaviour
    {
        public static ProficienciesUI Instance { get; private set; }

        static ProficienciesUI _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => EnsureInstance();

        public static ProficienciesUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var existing = FindAnyObjectByType<ProficienciesUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                _instance = existing;
                Instance = existing;
                return existing;
            }

            var go = new GameObject("ProficienciesUI");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ProficienciesUI>();
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
        TextMeshProUGUI _bannerText;
        TextMeshProUGUI _summaryText;
        TextMeshProUGUI _capWarningText;
        RacialAbilitiesPartyStripView _partyStrip;
        ProficiencyListBodyView _listBody;
        ProficiencyDetailPaneView _detailPane;

        readonly List<BaseActor> _partyActors = new();
        ProficiencySheetModel _sheet;
        int _focusedPartyIndex;
        ProficiencyKind _selectedKind = ProficiencyKind.Fighting;

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
            var canvasGo = new GameObject("ProficienciesCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _panelRoot = new GameObject("ProficienciesPanel", typeof(RectTransform), typeof(CanvasRenderer),
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

            TextMeshProUGUI title = CreateLayoutText("Title", "PROFICIENCIES",
                RacialUiTheme.TitleFontSize, FontStyles.Bold, 36f);
            title.color = RacialUiTheme.TitleText;

            _bannerText = CreateLayoutText("Banner",
                "Practice in the field — levels rise from use, not party kill XP.",
                RacialUiTheme.BannerFontSize, FontStyles.Italic, 26f);
            _bannerText.color = RacialUiTheme.BannerText;

            _partyStrip = RacialAbilitiesPartyStripView.Create(_panelRoot.transform, SetFocusedPartyIndex);

            _summaryText = CreateLayoutText("Summary", string.Empty, 17f, FontStyles.Normal, 24f);
            _summaryText.color = RacialUiTheme.BodyText;

            _capWarningText = CreateLayoutText("CapWarning", string.Empty, 15f, FontStyles.Italic, 0f);
            _capWarningText.color = RacialUiTheme.BannerText;
            _capWarningText.gameObject.SetActive(false);
            LayoutElement capLe = _capWarningText.gameObject.GetComponent<LayoutElement>() ??
                                  _capWarningText.gameObject.AddComponent<LayoutElement>();
            capLe.minHeight = 22f;
            capLe.preferredHeight = 22f;

            _listBody = ProficiencyListBodyView.Create(_panelRoot.transform, SelectProficiency);
            _detailPane = ProficiencyDetailPaneView.Create(_panelRoot.transform);

            TextMeshProUGUI hint = CreateLayoutText("Hint",
                "P — proficiencies · Esc — close · F1–F5 — focus member",
                RacialUiTheme.FooterFontSize, FontStyles.Normal, 26f);
            hint.color = RacialUiTheme.FooterText;

            _panelRoot.SetActive(false);
        }

        static TextMeshProUGUI CreateLayoutText(string name, string value, float size, FontStyles style, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            LayoutElement le = go.AddComponent<LayoutElement>();
            if (height > 0f)
            {
                le.minHeight = height;
                le.preferredHeight = height;
            }

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
            CharacterEquipmentUI.ForceCloseIfOpen();

            RefreshPartyActors();
            if (_partyActors.Count == 0)
            {
                _partyStrip.Rebuild(_partyActors, 0);
                _sheet = null;
                _summaryText.text = string.Empty;
                _capWarningText.gameObject.SetActive(false);
                _listBody.Refresh(null, ProficiencyKind.Fighting);
                _detailPane.Refresh(null);
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
            BaseActor active = PartyManager.Instance != null ? PartyManager.Instance.GetActiveMember() : null;
            if (active == null)
                return 0;

            for (int i = 0; i < _partyActors.Count; i++)
            {
                if (_partyActors[i] == active)
                    return i;
            }

            return 0;
        }

        void SetFocusedPartyIndex(int index)
        {
            if (_partyActors.Count == 0)
                return;

            _focusedPartyIndex = Mathf.Clamp(index, 0, _partyActors.Count - 1);
            RefreshSheet();
        }

        void SelectProficiency(ProficiencyKind kind)
        {
            _selectedKind = kind;
            RefreshPresentation();
        }

        void RefreshSheet()
        {
            if (_partyActors.Count == 0)
                return;

            BaseActor actor = _partyActors[_focusedPartyIndex];
            _sheet = ProficiencyListBodyViewModel.Build(actor);
            _selectedKind = _sheet.ResolveDefaultSelection();
            _partyStrip.Rebuild(_partyActors, _focusedPartyIndex);
            RefreshPresentation();
        }

        void RefreshPresentation()
        {
            if (_sheet == null)
                return;

            _summaryText.text = _sheet.SummaryLine;

            bool showCapWarning = !string.IsNullOrEmpty(_sheet.CapWarningLine);
            _capWarningText.gameObject.SetActive(showCapWarning);
            if (showCapWarning)
                _capWarningText.text = _sheet.CapWarningLine;

            _listBody.Refresh(_sheet, _selectedKind);
            _detailPane.Refresh(_sheet.FindRow(_selectedKind));
        }
    }
}
