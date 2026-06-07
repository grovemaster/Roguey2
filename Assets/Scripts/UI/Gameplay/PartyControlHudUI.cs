using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Input;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.UI.Hotbar;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class PartyControlHudUI : MonoBehaviour
    {
        const float ChipWidth = 108f;
        const float ChipHeight = 100f;
        const float PortraitSize = 62f;
        const float NameRowHeight = 24f;
        const float PartyLabelWidth = 84f;

        static readonly Color ActiveBorderColor = new Color(0.91f, 0.77f, 0.28f, 1f);
        static readonly Color InactiveBorderColor = new Color(0.14f, 0.16f, 0.2f, 0.95f);

        static PartyControlHudUI _instance;

        readonly List<PortraitChipWidget> _chips = new List<PortraitChipWidget>();

        GameObject _stripRoot;
        Transform _chipRow;
        InputHandler _inputHandler;
        PartyManager _partyManager;
        TurnManager _turnManager;
        int _lastPartyCount = -1;
        GameState _lastGameState = (GameState)(-1);

        public static PartyControlHudUI Instance => _instance;

        public static PartyControlHudUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(PartyControlHudUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PartyControlHudUI>();
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
            PartyMemberMapHighlight.EnsureInstance();
        }

        void OnEnable()
        {
            SubscribeManagers();
            RefreshAll();
        }

        void OnDisable() => UnsubscribeManagers();

        void OnDestroy()
        {
            UnsubscribeManagers();
            if (_instance == this)
                _instance = null;
        }

        void Update()
        {
            if (_stripRoot == null)
                return;

            SubscribeManagers();

            if (_inputHandler == null)
                _inputHandler = Object.FindAnyObjectByType<InputHandler>();

            PartyManager party = PartyManager.Instance;
            int partyCount = party?.partyMembers?.Count ?? 0;
            if (partyCount != _lastPartyCount)
            {
                _lastPartyCount = partyCount;
                RebuildChips();
            }

            TurnManager turn = TurnManager.Instance;
            GameState state = turn != null ? turn.currentState : GameState.BUSY;
            if (state != _lastGameState)
            {
                _lastGameState = state;
                RefreshAll();
            }
        }

        public void RefreshAll()
        {
            SubscribeManagers();

            bool showStrip = ShouldShowStrip();
            if (_stripRoot != null)
                _stripRoot.SetActive(showStrip);

            if (!showStrip)
            {
                PartyMemberMapHighlight.Instance?.Clear();
                return;
            }

            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return;

            int livingIndex = 0;
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (!IsLiving(member))
                    continue;

                if (livingIndex >= _chips.Count)
                    break;

                _chips[livingIndex].Bind(this, member, i, i == party.ActiveMemberIndex);
                livingIndex++;
            }

            for (int i = livingIndex; i < _chips.Count; i++)
                _chips[i].Bind(this, null, -1, false);

            UpdateMapHighlight();
        }

        void RebuildChips()
        {
            ClearChips();

            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
            {
                RefreshAll();
                return;
            }

            int livingCount = 0;
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                if (IsLiving(party.partyMembers[i]))
                    livingCount++;
            }

            for (int i = 0; i < livingCount; i++)
            {
                PortraitChipWidget chip = CreateChipWidget(_chipRow);
                _chips.Add(chip);
            }

            RefreshAll();
        }

        void UpdateMapHighlight()
        {
            PartyMemberMapHighlight highlight = PartyMemberMapHighlight.Instance;
            if (highlight == null)
                return;

            if (!ShouldShowMapHighlight())
            {
                highlight.Clear();
                return;
            }

            highlight.AttachTo(PartyManager.Instance?.GetActiveMember());
        }

        bool ShouldShowStrip()
        {
            TurnManager turn = TurnManager.Instance;
            if (turn != null && turn.currentState == GameState.GAME_OVER)
                return false;

            if (GameOverModalUI.BlocksGameplay)
                return false;

            return PartyManager.Instance != null;
        }

        bool ShouldShowMapHighlight()
        {
            TurnManager turn = TurnManager.Instance;
            if (turn == null || turn.currentState != GameState.PLAYER_TURN)
                return false;

            return ShouldShowStrip();
        }

        void TrySelectMember(int listIndex)
        {
            if (listIndex < 0)
                return;

            if (GameplayModalGate.BlocksFloorGameplay)
                return;

            TurnManager turn = TurnManager.Instance;
            if (turn == null || turn.currentState != GameState.PLAYER_TURN)
                return;

            PartyManager party = PartyManager.Instance;
            if (party == null || listIndex >= party.partyMembers.Count)
                return;

            BaseActor member = party.partyMembers[listIndex];
            if (!IsLiving(member))
                return;

            if (_inputHandler == null)
                _inputHandler = Object.FindAnyObjectByType<InputHandler>();

            if (_inputHandler?.CommandProcessor != null)
                _inputHandler.CommandProcessor.TryApply(PlayerCommand.SwapPartyMember(listIndex));
            else
                party.SwapActiveMember(listIndex);

            AbilityHotbarUI.Instance?.RefreshAll();
            RefreshAll();
        }

        void SubscribeManagers()
        {
            PartyManager party = PartyManager.Instance;
            if (party != null && party != _partyManager)
            {
                if (_partyManager != null)
                    _partyManager.ActiveMemberChanged -= OnActiveMemberChanged;

                _partyManager = party;
                _partyManager.ActiveMemberChanged += OnActiveMemberChanged;
            }

            TurnManager turn = TurnManager.Instance;
            if (turn != null && turn != _turnManager)
            {
                if (_turnManager != null)
                    _turnManager.PlayerActedStateChanged -= OnPlayerActedStateChanged;

                _turnManager = turn;
                _turnManager.PlayerActedStateChanged += OnPlayerActedStateChanged;
            }
        }

        void UnsubscribeManagers()
        {
            if (_partyManager != null)
                _partyManager.ActiveMemberChanged -= OnActiveMemberChanged;

            if (_turnManager != null)
                _turnManager.PlayerActedStateChanged -= OnPlayerActedStateChanged;

            _partyManager = null;
            _turnManager = null;
        }

        void OnActiveMemberChanged()
        {
            AbilityHotbarUI.Instance?.RefreshAll();
            RefreshAll();
        }

        void OnPlayerActedStateChanged() => RefreshAll();

        static bool IsLiving(BaseActor member) =>
            member != null && member.stats != null && member.stats.currentHP > 0;

        static bool HasActed(BaseActor member)
        {
            TurnManager turn = TurnManager.Instance;
            return turn != null && member != null && turn.HasActedThisTurn(member.gameObject);
        }

        void BuildUi()
        {
            var canvasGo = new GameObject(
                "PartyControlCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 46;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _stripRoot = new GameObject("PartyStrip", typeof(RectTransform), typeof(Image));
            _stripRoot.transform.SetParent(canvasGo.transform, false);
            RectTransform stripRt = (RectTransform)_stripRoot.transform;
            stripRt.anchorMin = new Vector2(0.5f, 1f);
            stripRt.anchorMax = new Vector2(0.5f, 1f);
            stripRt.pivot = new Vector2(0.5f, 1f);
            stripRt.anchoredPosition = new Vector2(0f, -8f);
            stripRt.sizeDelta = new Vector2(
                PlayfieldLayout.PartyStripWidthPixels,
                PlayfieldLayout.PartyStripHeightPixels - 8f);
            _stripRoot.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.82f);

            var bodyRow = new GameObject("Body", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bodyRow.transform.SetParent(_stripRoot.transform, false);
            StretchFull((RectTransform)bodyRow.transform);
            HorizontalLayoutGroup bodyLayout = bodyRow.GetComponent<HorizontalLayoutGroup>();
            bodyLayout.padding = new RectOffset(12, 12, 8, 8);
            bodyLayout.spacing = 8f;
            bodyLayout.childAlignment = TextAnchor.MiddleLeft;
            bodyLayout.childControlWidth = bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = bodyLayout.childForceExpandHeight = false;

            var labelColumn = new GameObject("PartyLabel", typeof(RectTransform));
            labelColumn.transform.SetParent(bodyRow.transform, false);
            LayoutElement labelLe = labelColumn.AddComponent<LayoutElement>();
            labelLe.minWidth = PartyLabelWidth;
            labelLe.preferredWidth = PartyLabelWidth;
            labelLe.flexibleHeight = 1f;

            TextMeshProUGUI header = CreateText(labelColumn.transform, "Header", "PARTY", 20f, FontStyles.Bold);
            StretchFull((RectTransform)header.transform);
            header.alignment = TextAlignmentOptions.MidlineLeft;

            var rowGo = new GameObject("Chips", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(bodyRow.transform, false);
            LayoutElement rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.flexibleWidth = 1f;
            rowLe.flexibleHeight = 1f;
            HorizontalLayoutGroup rowLayout = rowGo.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;
            _chipRow = rowGo.transform;
        }

        static Sprite _crownSprite;

        static Sprite CrownSprite
        {
            get
            {
                if (_crownSprite == null)
                    _crownSprite = CreateCrownSprite();

                return _crownSprite;
            }
        }

        PortraitChipWidget CreateChipWidget(Transform parent)
        {
            var go = new GameObject("PortraitChip", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = ChipWidth;
            le.preferredWidth = ChipWidth;
            le.minHeight = ChipHeight;
            le.preferredHeight = ChipHeight;

            Image frame = go.GetComponent<Image>();
            frame.color = InactiveBorderColor;

            TextMeshProUGUI nameLabel = CreateText(go.transform, "Name", string.Empty, 16f, FontStyles.Normal);
            RectTransform nameRt = (RectTransform)nameLabel.transform;
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.anchoredPosition = Vector2.zero;
            nameRt.sizeDelta = new Vector2(-8f, NameRowHeight);
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.overflowMode = TextOverflowModes.Ellipsis;

            var portraitFrame = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image));
            portraitFrame.transform.SetParent(go.transform, false);
            RectTransform portraitRt = (RectTransform)portraitFrame.transform;
            portraitRt.anchorMin = new Vector2(0.5f, 0f);
            portraitRt.anchorMax = new Vector2(0.5f, 0f);
            portraitRt.pivot = new Vector2(0.5f, 0f);
            portraitRt.anchoredPosition = new Vector2(0f, NameRowHeight + 4f);
            portraitRt.sizeDelta = new Vector2(PortraitSize, PortraitSize);
            portraitFrame.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 1f);

            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(portraitFrame.transform, false);
            StretchFull((RectTransform)portraitGo.transform);
            Image portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            var crownGo = new GameObject("Crown", typeof(RectTransform), typeof(Image));
            crownGo.transform.SetParent(portraitFrame.transform, false);
            RectTransform crownRt = (RectTransform)crownGo.transform;
            crownRt.anchorMin = new Vector2(1f, 1f);
            crownRt.anchorMax = new Vector2(1f, 1f);
            crownRt.pivot = new Vector2(1f, 1f);
            crownRt.anchoredPosition = new Vector2(-5f, -5f);
            crownRt.sizeDelta = new Vector2(18f, 14f);
            Image crownImage = crownGo.GetComponent<Image>();
            crownImage.sprite = CrownSprite;
            crownImage.color = new Color(0.95f, 0.82f, 0.35f, 1f);
            crownImage.preserveAspect = true;
            crownImage.raycastTarget = false;

            TextMeshProUGUI keyLabel = CreateKeyLabel(portraitFrame.transform);
            keyLabel.transform.SetAsLastSibling();

            PortraitChipWidget widget = go.AddComponent<PortraitChipWidget>();
            widget.Initialize(frame, portraitImage, keyLabel, crownImage, nameLabel, go.GetComponent<CanvasGroup>());
            return widget;
        }

        static Sprite CreateCrownSprite()
        {
            const int width = 16;
            const int height = 12;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color gold = Color.white;

            bool IsFilled(int x, int y)
            {
                if (y <= 1)
                    return x >= 1 && x <= 14;

                if (y == 2)
                    return x == 2 || x == 7 || x == 8 || x == 13;

                if (y == 3)
                    return x == 2 || x == 7 || x == 8 || x == 13;

                if (y == 4)
                    return x >= 2 && x <= 13;

                if (y >= 5 && y <= 8)
                    return x >= 4 && x <= 11;

                return false;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    texture.SetPixel(x, y, IsFilled(x, y) ? gold : clear);
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                height);
        }

        void ClearChips()
        {
            foreach (PortraitChipWidget chip in _chips)
            {
                if (chip != null)
                    Destroy(chip.gameObject);
            }

            _chips.Clear();
        }

        static TextMeshProUGUI CreateKeyLabel(Transform parent)
        {
            var badgeGo = new GameObject("KeyBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(parent, false);
            RectTransform badgeRt = (RectTransform)badgeGo.transform;
            badgeRt.anchorMin = new Vector2(0f, 1f);
            badgeRt.anchorMax = new Vector2(0f, 1f);
            badgeRt.pivot = new Vector2(0f, 1f);
            badgeRt.anchoredPosition = new Vector2(-4f, 4f);
            badgeRt.sizeDelta = new Vector2(36f, 22f);
            badgeGo.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.92f);

            TextMeshProUGUI text = CreateText(badgeGo.transform, "Key", string.Empty, 14f, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            StretchFull((RectTransform)text.transform);
            text.color = Color.white;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
            return text;
        }

        static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            float size,
            FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.text = value;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        sealed class PortraitChipWidget :
            MonoBehaviour,
            IPointerClickHandler
        {
            Image _frame;
            Image _portrait;
            TextMeshProUGUI _keyLabel;
            Image _crownBadge;
            TextMeshProUGUI _nameLabel;
            CanvasGroup _canvasGroup;
            PartyControlHudUI _host;
            int _listIndex = -1;

            public void Initialize(
                Image frame,
                Image portrait,
                TextMeshProUGUI keyLabel,
                Image crownBadge,
                TextMeshProUGUI nameLabel,
                CanvasGroup canvasGroup)
            {
                _frame = frame;
                _portrait = portrait;
                _keyLabel = keyLabel;
                _crownBadge = crownBadge;
                _nameLabel = nameLabel;
                _canvasGroup = canvasGroup;
            }

            public void Bind(PartyControlHudUI host, BaseActor member, int listIndex, bool isActive)
            {
                _host = host;
                _listIndex = listIndex;
                gameObject.SetActive(member != null);

                if (member == null)
                    return;

                _frame.color = isActive ? ActiveBorderColor : InactiveBorderColor;

                bool showKey = listIndex >= 0 && listIndex < 5;
                _keyLabel.transform.parent.gameObject.SetActive(showKey);
                _keyLabel.text = showKey ? $"F{listIndex + 1}" : string.Empty;

                bool isMain = PartyManager.Instance != null && PartyManager.Instance.IsMainCharacter(member);
                _crownBadge.gameObject.SetActive(isMain);

                _nameLabel.text = member.DisplayName;

                PortraitDefinition portraitDef = PortraitResolver.ResolveSpeaker(member, null);
                _portrait.sprite = portraitDef?.portrait;
                _portrait.color = _portrait.sprite != null ? Color.white : new Color(0.35f, 0.38f, 0.42f, 0.8f);

                bool acted = HasActed(member);
                bool playerTurn = TurnManager.Instance != null
                    && TurnManager.Instance.currentState == GameState.PLAYER_TURN;

                float alpha = playerTurn ? 1f : 0.55f;
                if (acted && !isActive)
                {
                    _portrait.color = new Color(0.65f, 0.68f, 0.72f, alpha);
                    _nameLabel.color = new Color(0.55f, 0.58f, 0.62f, alpha);
                }
                else
                {
                    _nameLabel.color = new Color(0.88f, 0.9f, 0.94f, alpha);
                    if (_portrait.sprite != null)
                        _portrait.color = new Color(1f, 1f, 1f, alpha);
                }

                _canvasGroup.alpha = alpha;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData.button != PointerEventData.InputButton.Left || _host == null)
                    return;

                _host.TrySelectMember(_listIndex);
            }
        }
    }
}
