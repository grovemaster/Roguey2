using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    /// <summary>
    /// Shown when dungeon time expires; OK continues to town load.
    /// </summary>
    public sealed class DungeonEndedDialogUI : MonoBehaviour
    {
        const string Title = "The Dungeon Has Ended";
        const string DefaultButtonLabel = "Continue";

        static DungeonEndedDialogUI _instance;

        GameObject _modalRoot;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _bodyText;
        TextMeshProUGUI _buttonLabelText;
        Action _onOk;
        bool _blocking;

        public static bool BlocksGameplay =>
            _instance != null && _instance._blocking;

        public static DungeonEndedDialogUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(DungeonEndedDialogUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DungeonEndedDialogUI>();
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
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame
                || kb.escapeKey.wasPressedThisFrame)
                CommitOk();
        }

        public void Show(string bodyMessage, Action onOk) =>
            Show(Title, bodyMessage, DefaultButtonLabel, onOk);

        public void Show(string title, string bodyMessage, string buttonLabel, Action onOk)
        {
            EnsureModalBuilt();
            _onOk = onOk;
            _blocking = true;

            if (_titleText != null)
                _titleText.text = title;

            if (_bodyText != null)
                _bodyText.text = bodyMessage;

            if (_buttonLabelText != null)
                _buttonLabelText.text = buttonLabel;

            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }

        void CommitOk()
        {
            Action act = _onOk;
            Close();
            act?.Invoke();
        }

        public static void ForceClose()
        {
            if (_instance != null)
                _instance.Close();
        }

        void Close()
        {
            _blocking = false;
            _onOk = null;
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
        }

        void EnsureModalBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject("DungeonEndedDialogCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            bubble.transform.SetParent(_modalRoot.transform, false);
            RectTransform bubbleRt = (RectTransform)bubble.transform;
            bubbleRt.anchorMin = bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRt.sizeDelta = new Vector2(520f, 300f);

            Image bubbleBg = bubble.GetComponent<Image>();
            bubbleBg.color = new Color(0.12f, 0.14f, 0.2f, 0.98f);

            VerticalLayoutGroup vlg = bubble.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            CreateTmp(bubble.transform, Title, 22, FontStyles.Bold, out _titleText);

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            bodyGo.transform.SetParent(bubble.transform, false);
            _bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
            _bodyText.fontSize = 16f;
            _bodyText.color = Color.white;
            _bodyText.alignment = TextAlignmentOptions.Center;
            _bodyText.textWrappingMode = TextWrappingModes.Normal;

            LayoutElement bodyLe = bodyGo.GetComponent<LayoutElement>();
            bodyLe.minHeight = 100f;
            bodyLe.flexibleHeight = 1f;

            CreateOkButton(bubble.transform);

            _modalRoot.SetActive(false);
        }

        void CreateOkButton(Transform parent)
        {
            var go = new GameObject("OkButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minHeight = 40f;
            le.preferredHeight = 40f;

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.38f, 0.58f, 1f);

            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(CommitOk);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = DefaultButtonLabel;
            _buttonLabelText = label;
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
        }

        static void CreateTmp(
            Transform parent,
            string text,
            float size,
            FontStyles style,
            out TextMeshProUGUI tmp)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }
}
