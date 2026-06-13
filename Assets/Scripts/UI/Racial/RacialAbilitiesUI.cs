using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Inventory;
using JRogue.UI.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public class RacialAbilitiesUI : MonoBehaviour
    {
        public static RacialAbilitiesUI Instance { get; private set; }

        static RacialAbilitiesUI _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => EnsureInstance();

        public static RacialAbilitiesUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var existing = FindAnyObjectByType<RacialAbilitiesUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                _instance = existing;
                Instance = existing;
                return existing;
            }

            var go = new GameObject("RacialAbilitiesUI");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<RacialAbilitiesUI>();
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
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _bannerText;
        TextMeshProUGUI _sectionLabel;
        TextMeshProUGUI _hintText;
        TextMeshProUGUI _placeholderBody;
        RacialAbilitiesPartyStripView _partyStrip;
        SpiritImprintTimelineView _timeline;
        ElementalSpiritContractsView _spiritContracts;
        TieflingImplantBodyView _tieflingImplants;
        BeastmanSoulBeastBodyView _beastmanSoulBeast;
        DragonianSpellBodyView _dragonianSpells;
        HumanMageSpellBodyView _humanMageSpells;
        RectTransform _scrollContent;
        RectTransform _elfScrollContent;
        GameObject _barbarianBodyRoot;
        GameObject _elfBodyRoot;
        GameObject _tieflingBodyRoot;
        GameObject _beastmanBodyRoot;
        GameObject _dragonianBodyRoot;
        GameObject _humanMageBodyRoot;
        GameObject _defaultBodyRoot;

        readonly List<BaseActor> _partyActors = new List<BaseActor>();
        int _focusedPartyIndex;

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
                if (_spiritContracts != null && _spiritContracts.IsNicknameFieldFocused())
                {
                    _spiritContracts.TryRevertFocusedNickname();
                    return;
                }

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
            var canvasGo = new GameObject("RacialAbilitiesCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _panelRoot = new GameObject("RacialAbilitiesPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panelRoot.transform.SetParent(canvasGo.transform, false);
            Image panelImage = _panelRoot.GetComponent<Image>();
            panelImage.sprite = RacialUiTheme.PlaceholderSprite;
            panelImage.color = RacialUiTheme.PanelBackground;

            RectTransform panelRt = _panelRoot.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var outerLayout = _panelRoot.AddComponent<VerticalLayoutGroup>();
            outerLayout.padding = new RectOffset(12, 12, 12, 12);
            outerLayout.spacing = 6f;
            outerLayout.childControlWidth = true;
            outerLayout.childControlHeight = true;
            outerLayout.childForceExpandWidth = true;
            outerLayout.childForceExpandHeight = false;

            _titleText = CreateLayoutText("Title", "RACIAL ABILITIES", RacialUiTheme.TitleFontSize, FontStyles.Bold, 36f);
            _titleText.color = RacialUiTheme.TitleText;
            _titleText.alignment = TextAlignmentOptions.MidlineLeft;

            _bannerText = CreateLayoutText("Banner", string.Empty, RacialUiTheme.BannerFontSize, FontStyles.Italic, 26f);
            _bannerText.color = RacialUiTheme.BannerText;
            _bannerText.alignment = TextAlignmentOptions.MidlineLeft;

            _partyStrip = RacialAbilitiesPartyStripView.Create(_panelRoot.transform, SetFocusedPartyIndex);

            _barbarianBodyRoot = CreateBodyRoot(_panelRoot.transform, "BarbarianBody");
            _sectionLabel = CreateLayoutTextOn(_barbarianBodyRoot.transform, "SectionLabel", "SPIRIT IMPRINT PATH", RacialUiTheme.SectionFontSize, FontStyles.Bold, 24f);
            _sectionLabel.color = RacialUiTheme.SectionLabel;
            _sectionLabel.alignment = TextAlignmentOptions.MidlineLeft;

            var scrollHost = new GameObject("TimelineScroll", typeof(RectTransform));
            scrollHost.transform.SetParent(_barbarianBodyRoot.transform, false);
            var scrollLe = scrollHost.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 200f;

            ScrollRect scroll = scrollHost.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollHost.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            viewport.GetComponent<Image>().color = new Color(0.12f, 0.125f, 0.135f, 0.92f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            _scrollContent = content.GetComponent<RectTransform>();
            _scrollContent.anchorMin = new Vector2(0f, 1f);
            _scrollContent.anchorMax = new Vector2(1f, 1f);
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            _scrollContent.sizeDelta = new Vector2(0f, 0f);

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = _scrollContent;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _timeline = SpiritImprintTimelineView.Create(_scrollContent);

            _elfBodyRoot = CreateBodyRoot(_panelRoot.transform, "ElfBody");
            CreateLayoutTextOn(_elfBodyRoot.transform, "SectionLabel", "ELEMENTAL SPIRIT CONTRACTS", RacialUiTheme.SectionFontSize, FontStyles.Bold, 24f).color =
                RacialUiTheme.SectionLabel;

            var elfScrollHost = new GameObject("ContractsScroll", typeof(RectTransform));
            elfScrollHost.transform.SetParent(_elfBodyRoot.transform, false);
            var elfScrollLe = elfScrollHost.AddComponent<LayoutElement>();
            elfScrollLe.flexibleHeight = 1f;
            elfScrollLe.minHeight = 200f;

            ScrollRect elfScroll = elfScrollHost.AddComponent<ScrollRect>();
            elfScroll.horizontal = false;
            elfScroll.vertical = true;
            elfScroll.movementType = ScrollRect.MovementType.Clamped;

            var elfViewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            elfViewport.transform.SetParent(elfScrollHost.transform, false);
            RacialUiTheme.Stretch(elfViewport.GetComponent<RectTransform>());
            elfViewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            elfViewport.GetComponent<Image>().color = new Color(0.12f, 0.125f, 0.135f, 0.92f);
            elfViewport.GetComponent<Mask>().showMaskGraphic = false;

            var elfContent = new GameObject("Content", typeof(RectTransform));
            elfContent.transform.SetParent(elfViewport.transform, false);
            _elfScrollContent = elfContent.GetComponent<RectTransform>();
            _elfScrollContent.anchorMin = new Vector2(0f, 1f);
            _elfScrollContent.anchorMax = new Vector2(1f, 1f);
            _elfScrollContent.pivot = new Vector2(0.5f, 1f);
            _elfScrollContent.sizeDelta = new Vector2(0f, 0f);

            elfScroll.viewport = elfViewport.GetComponent<RectTransform>();
            elfScroll.content = _elfScrollContent;

            var elfContentLayout = elfContent.AddComponent<VerticalLayoutGroup>();
            elfContentLayout.childControlWidth = true;
            elfContentLayout.childForceExpandWidth = true;
            elfContentLayout.childControlHeight = true;
            elfContentLayout.childForceExpandHeight = false;
            elfContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _spiritContracts = ElementalSpiritContractsView.Create(_elfScrollContent);

            var elfBodyFlex = _elfBodyRoot.AddComponent<LayoutElement>();
            elfBodyFlex.flexibleHeight = 1f;
            elfBodyFlex.minHeight = 240f;
            _elfBodyRoot.SetActive(false);

            _tieflingBodyRoot = CreateBodyRoot(_panelRoot.transform, "TieflingBody");
            _tieflingImplants = TieflingImplantBodyView.Create(_tieflingBodyRoot.transform);
            var tieflingBodyFlex = _tieflingBodyRoot.AddComponent<LayoutElement>();
            tieflingBodyFlex.flexibleHeight = 1f;
            tieflingBodyFlex.minHeight = 240f;
            _tieflingBodyRoot.SetActive(false);

            _beastmanBodyRoot = CreateBodyRoot(_panelRoot.transform, "BeastmanBody");
            _beastmanSoulBeast = BeastmanSoulBeastBodyView.Create(_beastmanBodyRoot.transform);
            var beastmanBodyFlex = _beastmanBodyRoot.AddComponent<LayoutElement>();
            beastmanBodyFlex.flexibleHeight = 1f;
            beastmanBodyFlex.minHeight = 240f;
            _beastmanBodyRoot.SetActive(false);

            _dragonianBodyRoot = CreateBodyRoot(_panelRoot.transform, "DragonianBody");
            _dragonianSpells = DragonianSpellBodyView.Create(_dragonianBodyRoot.transform);
            var dragonianBodyFlex = _dragonianBodyRoot.AddComponent<LayoutElement>();
            dragonianBodyFlex.flexibleHeight = 1f;
            dragonianBodyFlex.minHeight = 240f;
            _dragonianBodyRoot.SetActive(false);

            _humanMageBodyRoot = CreateBodyRoot(_panelRoot.transform, "HumanMageBody");
            _humanMageSpells = HumanMageSpellBodyView.Create(_humanMageBodyRoot.transform);
            var humanMageBodyFlex = _humanMageBodyRoot.AddComponent<LayoutElement>();
            humanMageBodyFlex.flexibleHeight = 1f;
            humanMageBodyFlex.minHeight = 240f;
            _humanMageBodyRoot.SetActive(false);

            _defaultBodyRoot = CreateBodyRoot(_panelRoot.transform, "DefaultBody");
            var defaultPanel = new GameObject("PlaceholderPanel", typeof(RectTransform), typeof(Image));
            defaultPanel.transform.SetParent(_defaultBodyRoot.transform, false);
            var defaultPanelLe = defaultPanel.AddComponent<LayoutElement>();
            defaultPanelLe.flexibleHeight = 1f;
            defaultPanelLe.minHeight = 200f;
            Image defaultPanelBg = defaultPanel.GetComponent<Image>();
            defaultPanelBg.sprite = RacialUiTheme.PlaceholderSprite;
            defaultPanelBg.color = new Color(0.12f, 0.125f, 0.135f, 0.92f);

            _placeholderBody = RacialUiTheme.CreateText(
                defaultPanel.transform, "Placeholder", string.Empty, RacialUiTheme.MessageFontSize, TextAlignmentOptions.Center);
            RacialUiTheme.Stretch(_placeholderBody.rectTransform);
            _placeholderBody.color = RacialUiTheme.MutedText;

            _hintText = CreateLayoutText("Hint", "K — racial abilities · Esc — close · F1–F5 — focus member", RacialUiTheme.FooterFontSize, FontStyles.Normal, 26f);
            _hintText.color = RacialUiTheme.FooterText;
            _hintText.alignment = TextAlignmentOptions.MidlineLeft;

            var bodyFlex = _barbarianBodyRoot.AddComponent<LayoutElement>();
            bodyFlex.flexibleHeight = 1f;
            bodyFlex.minHeight = 240f;

            var defaultFlex = _defaultBodyRoot.AddComponent<LayoutElement>();
            defaultFlex.flexibleHeight = 1f;
            defaultFlex.minHeight = 240f;

            _defaultBodyRoot.SetActive(false);
            _panelRoot.SetActive(false);
        }

        static GameObject CreateBodyRoot(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return root;
        }

        TextMeshProUGUI CreateLayoutText(string name, string value, float size, FontStyles style, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_panelRoot.transform, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            TextMeshProUGUI tmp = RacialUiTheme.CreateText(go.transform, "Label", value, size, TextAlignmentOptions.MidlineLeft, style);
            RacialUiTheme.Stretch(tmp.rectTransform);
            return tmp;
        }

        static TextMeshProUGUI CreateLayoutTextOn(
            Transform parent, string name, string value, float size, FontStyles style, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            TextMeshProUGUI tmp = RacialUiTheme.CreateText(go.transform, "Label", value, size, TextAlignmentOptions.MidlineLeft, style);
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
            JRogue.UI.Character.CharacterEquipmentUI.ForceCloseIfOpen();

            RefreshPartyActors();
            if (_partyActors.Count == 0)
            {
                ShowEmptyPartyState();
            }
            else
            {
                _focusedPartyIndex = ResolveDefaultFocusIndex();
                RefreshPartyStrip();
                RefreshBodyForFocusedMember();
            }

            _panelRoot.SetActive(true);
        }

        void ShowEmptyPartyState()
        {
            _bannerText.text = string.Empty;
            _partyStrip.Rebuild(_partyActors, 0);
            _barbarianBodyRoot.SetActive(true);
            _elfBodyRoot.SetActive(false);
            _tieflingBodyRoot.SetActive(false);
            _beastmanBodyRoot.SetActive(false);
            _dragonianBodyRoot.SetActive(false);
            _humanMageBodyRoot.SetActive(false);
            _defaultBodyRoot.SetActive(false);
            _timeline.SetPlainMessage("No party members available.");
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

        void RefreshPartyStrip() => _partyStrip.Rebuild(_partyActors, _focusedPartyIndex);

        void SetFocusedPartyIndex(int index)
        {
            if (index < 0 || index >= _partyActors.Count)
                return;

            _focusedPartyIndex = index;
            RefreshPartyStrip();
            RefreshBodyForFocusedMember();
        }

        void RefreshBodyForFocusedMember()
        {
            BaseActor actor = _partyActors[_focusedPartyIndex];
            if (actor == null)
            {
                _bannerText.text = string.Empty;
                _barbarianBodyRoot.SetActive(true);
                _elfBodyRoot.SetActive(false);
                _tieflingBodyRoot.SetActive(false);
                _beastmanBodyRoot.SetActive(false);
                _dragonianBodyRoot.SetActive(false);
                _humanMageBodyRoot.SetActive(false);
                _defaultBodyRoot.SetActive(false);
                _timeline.SetPlainMessage("Invalid party member.");
                return;
            }

            Race race = actor.stats != null ? actor.stats.race : Race.Human;

            if (race == Race.Barbarian)
                ShowBarbarianBody(actor);
            else if (race == Race.Elf)
                ShowElfBody(actor);
            else if (race == Race.Tiefling)
                ShowTieflingBody(actor);
            else if (race == Race.Beastman)
                ShowBeastmanBody(actor);
            else if (race == Race.Dragonian)
                ShowDragonianBody(actor);
            else if (race == Race.Human)
                ShowHumanBody(actor);
            else
                ShowDefaultBody(actor, race);
        }

        void ShowBarbarianBody(BaseActor actor)
        {
            _defaultBodyRoot.SetActive(false);
            _elfBodyRoot.SetActive(false);
            _tieflingBodyRoot.SetActive(false);
            _beastmanBodyRoot.SetActive(false);
            _dragonianBodyRoot.SetActive(false);
            _humanMageBodyRoot.SetActive(false);
            _barbarianBodyRoot.SetActive(true);

            var runtime = actor.GetComponent<SpiritImprintRuntime>();
            if (runtime == null || runtime.Graph == null)
            {
                _bannerText.text = string.Empty;
                _timeline.SetPlainMessage(BarbarianSpiritImprintViewModel.NotAwakenedMessage);
                return;
            }

            BarbarianSpiritImprintViewModel vm = BarbarianSpiritImprintViewModel.Build(actor);
            _bannerText.text = vm.BannerText;
            _timeline.Rebuild(vm.Cards);
        }

        void ShowElfBody(BaseActor actor)
        {
            _defaultBodyRoot.SetActive(false);
            _barbarianBodyRoot.SetActive(false);
            _tieflingBodyRoot.SetActive(false);
            _beastmanBodyRoot.SetActive(false);
            _dragonianBodyRoot.SetActive(false);
            _humanMageBodyRoot.SetActive(false);
            _elfBodyRoot.SetActive(true);

            if (actor.stats == null || actor.stats.racialSubsystem != RacialSubsystemKind.ElfElementalContracts)
            {
                _bannerText.text = string.Empty;
                _spiritContracts.SetPlainMessage("This character cannot form elemental spirit contracts.");
                return;
            }

            var runtime = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (runtime == null)
            {
                _bannerText.text = string.Empty;
                _spiritContracts.SetPlainMessage("This character cannot form elemental spirit contracts.");
                return;
            }

            _bannerText.text = ElfElementalSpiritViewModel.BannerText;
            _spiritContracts.Rebuild(actor, ElfElementalSpiritViewModel.Build(actor));
        }

        void ShowTieflingBody(BaseActor actor)
        {
            if (actor.stats == null ||
                actor.stats.racialSubsystem != RacialSubsystemKind.TieflingImplants ||
                actor.GetComponent<TieflingImplantsRuntime>() == null)
            {
                ShowDefaultBody(actor, Race.Tiefling);
                return;
            }

            _defaultBodyRoot.SetActive(false);
            _barbarianBodyRoot.SetActive(false);
            _elfBodyRoot.SetActive(false);
            _beastmanBodyRoot.SetActive(false);
            _dragonianBodyRoot.SetActive(false);
            _humanMageBodyRoot.SetActive(false);
            _tieflingBodyRoot.SetActive(true);

            _bannerText.text = TieflingImplantBodyViewModel.BannerText;
            _tieflingImplants.Rebuild(actor);
        }

        void ShowBeastmanBody(BaseActor actor)
        {
            if (actor.stats == null ||
                actor.stats.racialSubsystem != RacialSubsystemKind.BeastmanSoulBeast ||
                actor.GetComponent<BeastmanSoulBeastRuntime>() == null)
            {
                ShowDefaultBody(actor, Race.Beastman);
                return;
            }

            _defaultBodyRoot.SetActive(false);
            _barbarianBodyRoot.SetActive(false);
            _elfBodyRoot.SetActive(false);
            _tieflingBodyRoot.SetActive(false);
            _dragonianBodyRoot.SetActive(false);
            _humanMageBodyRoot.SetActive(false);
            _beastmanBodyRoot.SetActive(true);

            _bannerText.text = BeastmanSoulBeastBodyViewModel.BannerText;
            _beastmanSoulBeast.Rebuild(actor);
        }

        void ShowDragonianBody(BaseActor actor)
        {
            if (actor.stats == null ||
                actor.stats.racialSubsystem != RacialSubsystemKind.DragonianSpells ||
                actor.GetComponent<DragonianSpellsRuntime>() == null)
            {
                ShowDefaultBody(actor, Race.Dragonian);
                return;
            }

            _defaultBodyRoot.SetActive(false);
            _barbarianBodyRoot.SetActive(false);
            _elfBodyRoot.SetActive(false);
            _tieflingBodyRoot.SetActive(false);
            _beastmanBodyRoot.SetActive(false);
            _humanMageBodyRoot.SetActive(false);
            _dragonianBodyRoot.SetActive(true);

            DragonianSpellBodyViewModel vm = DragonianSpellBodyViewModel.Build(actor);
            _bannerText.text = vm.BannerText;
            _dragonianSpells.Rebuild(actor, vm.SelectedSpellId);
        }

        void ShowHumanBody(BaseActor actor)
        {
            if (actor.stats == null ||
                actor.stats.humanClass != HumanClass.Mage ||
                actor.stats.racialSubsystem != RacialSubsystemKind.HumanSpecialization)
            {
                ShowHumanDefaultPlaceholder(actor);
                return;
            }

            var runtime = actor.GetComponent<HumanMageSpellsRuntime>();
            if (runtime == null)
            {
                ShowHumanDefaultPlaceholder(actor, HumanMageSpellBodyViewModel.MissingRuntimeMessage);
                return;
            }

            _defaultBodyRoot.SetActive(false);
            _barbarianBodyRoot.SetActive(false);
            _elfBodyRoot.SetActive(false);
            _tieflingBodyRoot.SetActive(false);
            _beastmanBodyRoot.SetActive(false);
            _dragonianBodyRoot.SetActive(false);
            _humanMageBodyRoot.SetActive(true);

            HumanMageSpellBodyViewModel vm = HumanMageSpellBodyViewModel.Build(actor);
            _bannerText.text = vm.BannerText;
            _humanMageSpells.Rebuild(actor, vm.SelectedSpellId);
        }

        void ShowHumanDefaultPlaceholder(BaseActor actor, string overrideMessage = null)
        {
            _barbarianBodyRoot.SetActive(false);
            _elfBodyRoot.SetActive(false);
            _tieflingBodyRoot.SetActive(false);
            _beastmanBodyRoot.SetActive(false);
            _dragonianBodyRoot.SetActive(false);
            _humanMageBodyRoot.SetActive(false);
            _defaultBodyRoot.SetActive(true);

            _bannerText.text = string.Empty;
            string message = overrideMessage;
            if (string.IsNullOrWhiteSpace(message))
            {
                message = HumanMageSpellBodyViewModel.NotMageMessage;
                HumanClass humanClass = actor.stats?.humanClass ?? HumanClass.None;
                if (humanClass is HumanClass.Knight or HumanClass.Priest)
                    message += "\n\n" + HumanMageSpellBodyViewModel.PermanentClassMessage;
            }

            _placeholderBody.text = $"<b>{actor.DisplayName}</b>\nHuman\n\n{message}";
        }

        void ShowDefaultBody(BaseActor actor, Race race)
        {
            _barbarianBodyRoot.SetActive(false);
            _elfBodyRoot.SetActive(false);
            _tieflingBodyRoot.SetActive(false);
            _beastmanBodyRoot.SetActive(false);
            _dragonianBodyRoot.SetActive(false);
            _humanMageBodyRoot.SetActive(false);
            _defaultBodyRoot.SetActive(true);

            _bannerText.text = string.Empty;
            _placeholderBody.text =
                $"<b>{actor.DisplayName}</b>\n{race}\n\n" +
                RacialAbilitiesDefaultCopy.PlaceholderSubtitle(race) +
                "\n\nRacial ability details for this race will appear here in a future update.";
        }
    }
}
