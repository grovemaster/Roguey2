using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    /// <summary>Terminal game-over overlay — no dismiss control.</summary>
    public sealed class GameOverModalUI : MonoBehaviour
    {
        const string Title = "Game Over";

        static GameOverModalUI _instance;

        GameObject _modalRoot;
        bool _visible;

        public static bool BlocksGameplay =>
            _instance != null && _instance._visible;

        public static bool IsVisible =>
            _instance != null && _instance._visible;

        public static GameOverModalUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(GameOverModalUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GameOverModalUI>();
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

        public void ShowTerminal(string mainCharacterDisplayName)
        {
            EnsureModalBuilt();
            _visible = true;

            string name = string.IsNullOrWhiteSpace(mainCharacterDisplayName)
                ? "Your hero"
                : mainCharacterDisplayName.Trim();

            Transform body = _modalRoot.transform.Find("Bubble/Body");
            if (body != null && body.TryGetComponent(out TextMeshProUGUI bodyText))
            {
                bodyText.text =
                    $"<b>{name}</b> has fallen.\n\n<size=16>Your journey ends here.</size>";
            }

            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }

        void EnsureModalBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject("GameOverCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;

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
            dim.color = new Color(0f, 0f, 0f, 0.75f);
            dim.raycastTarget = true;

            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            bubble.transform.SetParent(_modalRoot.transform, false);
            RectTransform bubbleRt = (RectTransform)bubble.transform;
            bubbleRt.anchorMin = bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRt.sizeDelta = new Vector2(480f, 240f);

            Image bubbleBg = bubble.GetComponent<Image>();
            bubbleBg.color = new Color(0.1f, 0.08f, 0.12f, 0.98f);

            VerticalLayoutGroup vlg = bubble.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(28, 28, 24, 24);
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            CreateTmp(bubble.transform, Title, 28, FontStyles.Bold, new Color(0.95f, 0.35f, 0.3f));

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            bodyGo.transform.SetParent(bubble.transform, false);
            TextMeshProUGUI bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
            bodyText.fontSize = 18f;
            bodyText.color = Color.white;
            bodyText.alignment = TextAlignmentOptions.Center;
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            LayoutElement bodyLe = bodyGo.GetComponent<LayoutElement>();
            bodyLe.minHeight = 100f;

            _modalRoot.SetActive(false);
        }

        static void CreateTmp(Transform parent, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }
}
