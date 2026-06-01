using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class EnterDungeonDialogUI : MonoBehaviour
    {
        const string Title = "Enter the dungeon?";

        static EnterDungeonDialogUI _instance;

        GameObject _modalRoot;
        TextMeshProUGUI _bodyText;
        Action _onEnter;
        bool _blocking;

        public static bool BlocksGameplay =>
            _instance != null && _instance._blocking;

        public static EnterDungeonDialogUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(EnterDungeonDialogUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<EnterDungeonDialogUI>();
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
            if (kb.yKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                CommitEnter();
            else if (kb.nKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
                Cancel();
        }

        public void Show(string bodyMessage, Action onEnter)
        {
            EnsureModalBuilt();
            _onEnter = onEnter;
            _blocking = true;

            if (_bodyText != null)
                _bodyText.text = bodyMessage;

            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }

        void CommitEnter()
        {
            Action act = _onEnter;
            Close();
            act?.Invoke();
        }

        void Cancel() => Close();

        public static void ForceClose()
        {
            if (_instance != null)
                _instance.Close();
        }

        void Close()
        {
            _blocking = false;
            _onEnter = null;
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
        }

        void EnsureModalBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject("EnterDungeonDialogCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 505;

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
            bubbleRt.sizeDelta = new Vector2(520f, 320f);

            Image bubbleBg = bubble.GetComponent<Image>();
            bubbleBg.color = new Color(0.12f, 0.14f, 0.2f, 0.98f);

            VerticalLayoutGroup vlg = bubble.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            CreateTmp(bubble.transform, Title, 22, FontStyles.Bold, out _);

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            bodyGo.transform.SetParent(bubble.transform, false);
            _bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
            _bodyText.fontSize = 16f;
            _bodyText.color = Color.white;
            _bodyText.alignment = TextAlignmentOptions.Center;
            _bodyText.textWrappingMode = TextWrappingModes.Normal;

            LayoutElement bodyLe = bodyGo.GetComponent<LayoutElement>();
            bodyLe.minHeight = 110f;
            bodyLe.flexibleHeight = 1f;

            CreateButtonRow(bubble.transform);

            _modalRoot.SetActive(false);
        }

        void CreateButtonRow(Transform parent)
        {
            var row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            LayoutElement rowLe = row.GetComponent<LayoutElement>();
            rowLe.minHeight = 44f;
            rowLe.preferredHeight = 44f;

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;

            CreateButton(row.transform, "Enter", new Color(0.22f, 0.38f, 0.58f, 1f), CommitEnter);
            CreateButton(row.transform, "Stay", new Color(0.35f, 0.32f, 0.32f, 1f), Cancel);
        }

        void CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Image img = go.GetComponent<Image>();
            img.color = color;

            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
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
