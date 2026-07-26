using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.UI.Inventory;
using JRogue.World.Altar;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class AltarOfferingModalUI : MonoBehaviour
    {
        enum FocusPane
        {
            PlaceList = 0,
            OnAltar = 1,
        }

        static AltarOfferingModalUI _instance;

        GameObject _modalRoot;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _bodyText;
        TextMeshProUGUI _hintText;

        BaseActor _actor;
        AltarInstance _altar;
        System.Action<bool> _onClosed;

        readonly List<AltarPlaceableStack> _placeStacks = new List<AltarPlaceableStack>();
        bool _removeMode;
        FocusPane _focusPane = FocusPane.PlaceList;
        int _placeFocus;
        int _altarFocus;
        bool _blocking;

        public static bool BlocksGameplay => _instance != null && _instance._blocking;

        public static AltarOfferingModalUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(AltarOfferingModalUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AltarOfferingModalUI>();
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
                Close(consumedTurn: false);
                return;
            }

            if (kb.rKey.wasPressedThisFrame)
            {
                EnterRemoveMode();
                return;
            }

            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                MoveFocus(-1);
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                MoveFocus(1);
            else if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                ConfirmFocused();
        }

        public void Show(BaseActor actor, AltarInstance altar, System.Action<bool> onClosed)
        {
            EnsureModalBuilt();
            _actor = actor;
            _altar = altar;
            _onClosed = onClosed;
            _removeMode = false;
            _focusPane = FocusPane.PlaceList;
            _placeFocus = 0;
            _altarFocus = 0;
            _blocking = true;

            if (_titleText != null)
            {
                string title = altar?.Definition != null
                    ? altar.Definition.displayName.ToUpperInvariant()
                    : "ALTAR";
                _titleText.text = title;
            }

            RefreshBody();
            UpdateHint();

            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }

        void EnterRemoveMode()
        {
            if (_altar == null || !HasAnyOfferingOnAltar())
                return;

            _removeMode = true;
            _focusPane = FocusPane.OnAltar;
            _altarFocus = FindFirstFilledAltarIndex();
            RefreshBody();
            UpdateHint();
        }

        void MoveFocus(int delta)
        {
            if (_removeMode || _focusPane == FocusPane.OnAltar)
            {
                int filledCount = CountFilledAltarSlots();
                if (filledCount == 0)
                    return;

                _altarFocus = (_altarFocus + delta + _altar.Slots.Count) % _altar.Slots.Count;
                if (_altar.Slots[_altarFocus].IsEmpty)
                    MoveFocus(delta);
                else
                    RefreshBody();
            }
            else if (_placeStacks.Count > 0)
            {
                _placeFocus = (_placeFocus + delta + _placeStacks.Count) % _placeStacks.Count;
                RefreshBody();
            }
        }

        void ConfirmFocused()
        {
            if (_removeMode || _focusPane == FocusPane.OnAltar)
            {
                if (_altar == null)
                    return;

                int slotIndex = _altarFocus;
                if (slotIndex < 0 || slotIndex >= _altar.Slots.Count || _altar.Slots[slotIndex].IsEmpty)
                    return;

                if (AltarOfferingService.TryRemoveFromSlot(_altar, slotIndex) == AltarOfferingResult.Removed)
                    Close(consumedTurn: true);

                return;
            }

            if (_placeStacks.Count == 0)
                return;

            AltarPlaceableStack stack = _placeStacks[_placeFocus];
            if (AltarOfferingService.TryPlaceManaStone(_altar, stack.Tier, stack.SourceSpeciesId)
                == AltarOfferingResult.Placed)
            {
                Close(consumedTurn: true);
            }
        }

        void Close(bool consumedTurn)
        {
            _blocking = false;
            System.Action<bool> cb = _onClosed;
            _onClosed = null;
            _actor = null;
            _altar = null;
            _placeStacks.Clear();
            _removeMode = false;

            if (_modalRoot != null)
                _modalRoot.SetActive(false);

            cb?.Invoke(consumedTurn);
        }

        void RefreshBody()
        {
            if (_bodyText == null || _altar == null)
                return;

            PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
            AltarPlaceListBuilder.BuildPlaceableStacks(_altar, ledger, _placeStacks);

            var sb = new StringBuilder();
            string description = _altar.Definition != null
                ? _altar.Definition.descriptionTemplate
                : string.Empty;
            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendLine(description);
                sb.AppendLine();
            }

            sb.AppendLine("ON ALTAR");
            AppendOnAltarSection(sb);
            sb.AppendLine();

            string header = AltarPlaceListBuilder.BuildPlaceListHeader(_altar);
            sb.AppendLine(header);
            AppendPlaceListSection(sb);

            _bodyText.text = sb.ToString();
        }

        void AppendOnAltarSection(StringBuilder sb)
        {
            if (_altar?.Definition?.slots == null)
            {
                sb.AppendLine("  (empty)");
                return;
            }

            for (int i = 0; i < _altar.Definition.slots.Length; i++)
            {
                AltarSlotDefinition slotDef = _altar.Definition.slots[i];
                string label = slotDef != null ? slotDef.label : $"Slot {i}";
                bool filled = i < _altar.Slots.Count && !_altar.Slots[i].IsEmpty;
                bool highlight = _removeMode && _focusPane == FocusPane.OnAltar && _altarFocus == i && filled;
                string prefix = highlight ? "▶ " : "  ";

                if (!filled)
                {
                    sb.Append(prefix);
                    sb.Append("(empty");
                    if (slotDef != null && AltarSlotFilters.TryGetManaStoneTier(slotDef.acceptFilter, out int tier))
                        sb.Append($" — tier {tier} slot");
                    sb.AppendLine(")");
                    continue;
                }

                AltarManaStoneOffering offering = _altar.Slots[i].Offering;
                string species = InventoryCurrencyDisplay.FormatSpeciesDisplayName(offering.SourceSpeciesId);
                sb.Append(prefix);
                sb.Append(label);
                sb.Append(" · ");
                sb.Append(species);
                sb.AppendLine();
            }
        }

        void AppendPlaceListSection(StringBuilder sb)
        {
            if (_placeStacks.Count == 0)
            {
                if (!AltarPlaceListBuilder.HasOpenSlot(_altar))
                {
                    sb.AppendLine("  (altar full)");
                    return;
                }

                var needed = new HashSet<int>();
                AltarPlaceListBuilder.CollectNeededTiers(_altar, needed);
                if (needed.Count == 1)
                {
                    foreach (int tier in needed)
                        sb.AppendLine($"  You have no tier {tier} mana stones to place.");
                }
                else if (needed.Count > 1)
                {
                    sb.AppendLine("  You have no tier 9 or tier 8 mana stones to place.");
                }
                else
                {
                    sb.AppendLine("  You have no matching mana stones to place.");
                }

                return;
            }

            for (int i = 0; i < _placeStacks.Count; i++)
            {
                AltarPlaceableStack stack = _placeStacks[i];
                bool highlight = !_removeMode && _focusPane == FocusPane.PlaceList && _placeFocus == i;
                string prefix = highlight ? "▶ " : "  ";
                string species = InventoryCurrencyDisplay.FormatSpeciesDisplayName(stack.SourceSpeciesId);
                sb.Append(prefix);
                sb.Append("Tier ");
                sb.Append(stack.Tier);
                sb.Append(" · ");
                sb.Append(species);
                sb.Append(" × ");
                sb.AppendLine(stack.Count.ToString());
            }
        }

        void UpdateHint()
        {
            if (_hintText == null)
                return;

            bool canRemove = HasAnyOfferingOnAltar();
            if (_removeMode)
            {
                _hintText.text =
                    "<size=14><color=#9bbdff>Enter</color> remove from altar   ·   "
                    + "<color=#ffb28a>Esc</color> cancel</size>";
                return;
            }

            string removeHint = canRemove
                ? "<color=#e8c070>R</color> remove   ·   "
                : string.Empty;

            _hintText.text =
                "<size=14><color=#9bbdff>Enter</color> place selected stone   ·   "
                + removeHint
                + "<color=#ffb28a>Esc</color> cancel   ·   ↑↓ move</size>";
        }

        bool HasAnyOfferingOnAltar()
        {
            if (_altar == null)
                return false;

            for (int i = 0; i < _altar.Slots.Count; i++)
            {
                if (!_altar.Slots[i].IsEmpty)
                    return true;
            }

            return false;
        }

        int CountFilledAltarSlots()
        {
            int count = 0;
            if (_altar == null)
                return count;

            for (int i = 0; i < _altar.Slots.Count; i++)
            {
                if (!_altar.Slots[i].IsEmpty)
                    count++;
            }

            return count;
        }

        int FindFirstFilledAltarIndex()
        {
            if (_altar == null)
                return 0;

            for (int i = 0; i < _altar.Slots.Count; i++)
            {
                if (!_altar.Slots[i].IsEmpty)
                    return i;
            }

            return 0;
        }

        void EnsureModalBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject(
                "AltarOfferingCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 504;

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

            Image dim = _modalRoot.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.65f);
            dim.raycastTarget = true;

            var bubble = new GameObject(
                "Bubble",
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup));
            bubble.transform.SetParent(_modalRoot.transform, false);
            RectTransform bubbleRt = (RectTransform)bubble.transform;
            bubbleRt.anchorMin = bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRt.sizeDelta = new Vector2(640f, 420f);

            Image bubbleBg = bubble.GetComponent<Image>();
            bubbleBg.color = new Color(0.12f, 0.14f, 0.2f, 0.98f);

            VerticalLayoutGroup vlg = bubble.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            _titleText = CreateText(bubble.transform, "Title", 20, FontStyles.Bold);
            _bodyText = CreateText(bubble.transform, "Body", 15, FontStyles.Normal);
            _hintText = CreateText(bubble.transform, "Hint", 14, FontStyles.Normal);

            LayoutElement bodyLayout = _bodyText.gameObject.AddComponent<LayoutElement>();
            bodyLayout.minHeight = 260f;
            bodyLayout.flexibleHeight = 1f;

            _modalRoot.SetActive(false);
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
    }
}
