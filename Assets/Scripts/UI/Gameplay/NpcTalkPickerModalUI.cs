using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class NpcTalkPickerModalUI : MonoBehaviour
    {
        static NpcTalkPickerModalUI _instance;

        GameObject _modalRoot;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _listText;
        TextMeshProUGUI _hintText;

        readonly List<INpcTalkTarget> _targets = new List<INpcTalkTarget>();
        BaseActor _actor;
        Action<INpcTalkTarget> _onSelected;
        int _focusIndex;
        bool _blocking;

        public static bool BlocksGameplay => _instance != null && _instance._blocking;

        public static NpcTalkPickerModalUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(NpcTalkPickerModalUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<NpcTalkPickerModalUI>();
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
                Cancel();
                return;
            }

            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                MoveFocus(-1);
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                MoveFocus(1);
            else if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                ConfirmFocused();
            else
                TryDigitSelect(kb);
        }

        void TryDigitSelect(Keyboard kb)
        {
            for (int digit = 1; digit <= 9; digit++)
            {
                if (!WasDigitPressed(kb, digit))
                    continue;

                int index = digit - 1;
                if (index < 0 || index >= _targets.Count)
                    return;

                _focusIndex = index;
                ConfirmFocused();
                return;
            }
        }

        static bool WasDigitPressed(Keyboard kb, int digit) =>
            digit switch
            {
                1 => kb.digit1Key.wasPressedThisFrame,
                2 => kb.digit2Key.wasPressedThisFrame,
                3 => kb.digit3Key.wasPressedThisFrame,
                4 => kb.digit4Key.wasPressedThisFrame,
                5 => kb.digit5Key.wasPressedThisFrame,
                6 => kb.digit6Key.wasPressedThisFrame,
                7 => kb.digit7Key.wasPressedThisFrame,
                8 => kb.digit8Key.wasPressedThisFrame,
                9 => kb.digit9Key.wasPressedThisFrame,
                _ => false,
            };

        public void Show(BaseActor actor, IReadOnlyList<INpcTalkTarget> targets, Action<INpcTalkTarget> onSelected)
        {
            EnsureModalBuilt();
            _actor = actor;
            _onSelected = onSelected;
            _targets.Clear();

            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                    _targets.Add(targets[i]);
            }

            _focusIndex = 0;
            _blocking = true;
            RefreshListText();

            if (_titleText != null)
                _titleText.text = "TALK";

            if (_hintText != null)
            {
                _hintText.text =
                    "<size=14><color=#9bbdff>Enter</color> talk   ·   "
                    + "<color=#ffb28a>Esc</color> cancel   ·   ↑↓ move</size>";
            }

            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }

        void MoveFocus(int delta)
        {
            if (_targets.Count == 0)
                return;

            _focusIndex = (_focusIndex + delta + _targets.Count) % _targets.Count;
            RefreshListText();
        }

        void ConfirmFocused()
        {
            if (_targets.Count == 0)
            {
                Cancel();
                return;
            }

            INpcTalkTarget selected = _targets[_focusIndex];
            Action<INpcTalkTarget> cb = _onSelected;
            Close();
            cb?.Invoke(selected);
        }

        void Cancel() => Close();

        void Close()
        {
            _blocking = false;
            _onSelected = null;
            _actor = null;
            _targets.Clear();
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
        }

        void RefreshListText()
        {
            if (_listText == null)
                return;

            if (_targets.Count == 0)
            {
                _listText.text = "Choose who to talk to:\n\n  (none)";
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Choose who to talk to:");
            sb.AppendLine();

            for (int i = 0; i < _targets.Count; i++)
            {
                string prefix = i == _focusIndex ? "▶ " : "  ";
                sb.Append(prefix);
                string name = _targets[i].Actor != null ? _targets[i].Actor.DisplayName : "NPC";
                sb.AppendLine(name);
            }

            _listText.text = sb.ToString();
        }

        void EnsureModalBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject(
                "NpcTalkPickerCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 503;

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
            bubbleRt.sizeDelta = new Vector2(480f, 320f);

            Image bubbleBg = bubble.GetComponent<Image>();
            bubbleBg.color = new Color(0.12f, 0.14f, 0.2f, 0.98f);

            VerticalLayoutGroup vlg = bubble.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            _titleText = CreateText(bubble.transform, "Title", 20, FontStyles.Bold);
            _listText = CreateText(bubble.transform, "List", 16, FontStyles.Normal);
            _hintText = CreateText(bubble.transform, "Hint", 14, FontStyles.Normal);

            LayoutElement listLayout = _listText.gameObject.AddComponent<LayoutElement>();
            listLayout.minHeight = 160f;
            listLayout.flexibleHeight = 1f;

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
