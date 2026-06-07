using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class MessageConsoleUI : MonoBehaviour
    {
        public const int DefaultVisibleLines = 5;

        static MessageConsoleUI _instance;

        readonly List<string> _windowScratch = new List<string>();
        GameObject _root;
        TextMeshProUGUI _bodyText;
        TextMeshProUGUI _hintText;
        int _scrollbackOffset;
        int _visibleLines = DefaultVisibleLines;

        public static MessageConsoleUI Instance => _instance;

        public static MessageConsoleUI EnsureInstance()
        {
            GameLogService.EnsureInstance();
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(MessageConsoleUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MessageConsoleUI>();
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
            BuildConsole();
            GameLogService.EnsureInstance().Session.SessionChanged += OnSessionChanged;
            RefreshDisplay();
        }

        void OnDestroy()
        {
            if (GameLogService.Instance != null)
                GameLogService.Instance.Session.SessionChanged -= OnSessionChanged;

            if (_instance == this)
                _instance = null;
        }

        public void ResetScrollback() => _scrollbackOffset = 0;

        public void HandleGameplayInput()
        {
            if (MessageHistoryUI.IsOpen || GameplayModalGate.BlocksFloorGameplay)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.pKey.wasPressedThisFrame)
            {
                MessageHistoryUI.EnsureInstance().Show();
                return;
            }

            if (keyboard.minusKey.wasPressedThisFrame || keyboard.leftBracketKey.wasPressedThisFrame)
                ScrollBack(1);
            else if (keyboard.equalsKey.wasPressedThisFrame || keyboard.rightBracketKey.wasPressedThisFrame)
                ScrollForward(1);
            else if (keyboard.pageUpKey.wasPressedThisFrame)
                ScrollBack(_visibleLines);
            else if (keyboard.pageDownKey.wasPressedThisFrame)
                ScrollForward(_visibleLines);
            else if (keyboard.endKey.wasPressedThisFrame)
                ResetScrollback();
        }

        void OnSessionChanged()
        {
            if (_scrollbackOffset == 0)
                RefreshDisplay();
        }

        void ScrollBack(int amount)
        {
            GameLogSession session = GameLogService.ActiveSession;
            if (session == null || amount <= 0)
                return;

            int maxOffset = session.GetMaxScrollbackOffset(_visibleLines);
            _scrollbackOffset = Mathf.Min(_scrollbackOffset + amount, maxOffset);
            RefreshDisplay();
        }

        void ScrollForward(int amount)
        {
            if (amount <= 0)
                return;

            _scrollbackOffset = Mathf.Max(0, _scrollbackOffset - amount);
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            if (_bodyText == null)
                return;

            GameLogSession session = GameLogService.ActiveSession;
            if (session == null || session.Count == 0)
            {
                _bodyText.text = string.Empty;
                return;
            }

            session.CopyWindow(_scrollbackOffset, _visibleLines, _windowScratch);
            var builder = new StringBuilder();
            for (int i = _windowScratch.Count - 1; i >= 0; i--)
            {
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(_windowScratch[i]);
            }

            _bodyText.text = builder.ToString();
        }

        void BuildConsole()
        {
            var canvasGo = new GameObject(
                "MessageConsoleCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _root = new GameObject("ConsolePanel", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            _root.transform.SetParent(canvasGo.transform, false);

            RectTransform panelRt = (RectTransform)_root.transform;
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(0f, PlayfieldLayout.ConsoleHeightPixels);

            Image panelBg = _root.GetComponent<Image>();
            panelBg.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);

            _bodyText = CreateText(_root.transform, "Body", 14f, new Vector2(12f, 28f), new Vector2(-12f, -8f));
            _bodyText.alignment = TextAlignmentOptions.TopLeft;
            _bodyText.textWrappingMode = TextWrappingModes.Normal;
            _bodyText.overflowMode = TextOverflowModes.Overflow;

            _hintText = CreateText(_root.transform, "Hint", 11f, new Vector2(12f, 4f), new Vector2(-12f, 22f));
            _hintText.alignment = TextAlignmentOptions.BottomRight;
            _hintText.color = new Color(0.65f, 0.7f, 0.78f, 0.9f);
            _hintText.text = "P history   -/+ scroll";
        }

        static TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            float fontSize,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = new Color(0.88f, 0.9f, 0.94f, 1f);
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }
    }
}
