using JRogue.World.Altar;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class AltarUsedModalUI : MonoBehaviour
    {
        static AltarUsedModalUI _instance;

        GameObject _modalRoot;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _bodyText;
        TextMeshProUGUI _hintText;
        bool _blocking;

        public static bool BlocksGameplay => _instance != null && _instance._blocking;

        public static AltarUsedModalUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(AltarUsedModalUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AltarUsedModalUI>();
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

            if (Keyboard.current.escapeKey.wasPressedThisFrame
                || Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Show(AltarInstance altar)
        {
            EnsureModalBuilt();
            _blocking = true;

            string title = altar?.Definition != null
                ? altar.Definition.displayName.ToUpperInvariant()
                : "ALTAR";

            string body = altar?.Definition != null
                ? altar.Definition.usedDescriptionTemplate
                : "This altar has been used.";

            if (_titleText != null)
                _titleText.text = title;

            if (_bodyText != null)
                _bodyText.text = body;

            if (_hintText != null)
            {
                _hintText.text =
                    "<size=14><color=#ffb28a>Esc</color> close</size>";
            }

            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }

        void Close()
        {
            _blocking = false;
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
        }

        void EnsureModalBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject(
                "AltarUsedCanvas",
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
            bubbleRt.sizeDelta = new Vector2(520f, 240f);

            Image bubbleBg = bubble.GetComponent<Image>();
            bubbleBg.color = new Color(0.12f, 0.14f, 0.2f, 0.98f);

            VerticalLayoutGroup vlg = bubble.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            _titleText = CreateText(bubble.transform, "Title", 20, FontStyles.Bold);
            _bodyText = CreateText(bubble.transform, "Body", 16, FontStyles.Normal);
            _hintText = CreateText(bubble.transform, "Hint", 14, FontStyles.Normal);

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
