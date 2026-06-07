using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class MessageHistoryUI : MonoBehaviour
    {
        static MessageHistoryUI _instance;

        GameObject _modalRoot;
        ScrollRect _scrollRect;
        TextMeshProUGUI _bodyText;
        TextMeshProUGUI _footerText;
        bool _open;

        public static bool IsOpen => _instance != null && _instance._open;

        public static bool BlocksGameplay => IsOpen;

        public static MessageHistoryUI EnsureInstance()
        {
            GameLogService.EnsureInstance();
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(MessageHistoryUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MessageHistoryUI>();
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
            EnsureBuilt();
            _modalRoot.SetActive(false);
            GameLogService.EnsureInstance().Session.SessionChanged += OnSessionChanged;
        }

        void OnDestroy()
        {
            if (GameLogService.Instance != null)
                GameLogService.Instance.Session.SessionChanged -= OnSessionChanged;

            if (_instance == this)
                _instance = null;
        }

        void Update()
        {
            if (!_open)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Hide();
                return;
            }

            if (keyboard.homeKey.wasPressedThisFrame)
                _scrollRect.verticalNormalizedPosition = 1f;
            else if (keyboard.endKey.wasPressedThisFrame)
                _scrollRect.verticalNormalizedPosition = 0f;
            else if (keyboard.pageUpKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                NudgeScroll(0.15f);
            else if (keyboard.pageDownKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                NudgeScroll(-0.15f);

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 scrollDelta = mouse.scroll.ReadValue();
                if (Mathf.Abs(scrollDelta.y) > 0.01f)
                    NudgeScroll(scrollDelta.y / 120f * 0.15f);
            }
        }

        public void Show()
        {
            EnsureBuilt();
            _open = true;
            RefreshContent(scrollToLatest: true);
            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            _open = false;
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
        }

        void OnSessionChanged()
        {
            if (!_open)
                return;

            RefreshContent(scrollToLatest: IsNearLatest());
        }

        bool IsNearLatest() => _scrollRect != null && _scrollRect.verticalNormalizedPosition >= 0.95f;

        void NudgeScroll(float delta) =>
            _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + delta);

        void RefreshContent(bool scrollToLatest)
        {
            GameLogSession session = GameLogService.Instance?.Session;
            if (session == null || _bodyText == null)
                return;

            var builder = new StringBuilder();
            for (int i = session.Count - 1; i >= 0; i--)
            {
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(session.Lines[i]);
            }

            _bodyText.text = builder.ToString();
            if (_footerText != null)
                _footerText.text = $"{session.Count} messages · Esc close · Home/End/PgUp/PgDn scroll";

            RectTransform contentRt = _bodyText.rectTransform;
            _bodyText.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            if (scrollToLatest)
                _scrollRect.verticalNormalizedPosition = 1f;
        }

        void EnsureBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject(
                "MessageHistoryCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 510;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _modalRoot = new GameObject("Modal", typeof(RectTransform), typeof(Image));
            _modalRoot.transform.SetParent(canvasGo.transform, false);
            StretchFull((RectTransform)_modalRoot.transform);
            _modalRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_modalRoot.transform, false);
            RectTransform panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(1200f, 780f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            var header = CreateText(panel.transform, "Header", 22f, FontStyles.Bold);
            RectTransform headerRt = (RectTransform)header.transform;
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(-32f, 40f);
            headerRt.anchoredPosition = new Vector2(0f, -16f);
            header.text = "MESSAGE HISTORY";
            header.alignment = TextAlignmentOptions.MidlineLeft;

            var scrollGo = new GameObject(
                "Scroll",
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(Image));
            scrollGo.transform.SetParent(panel.transform, false);
            RectTransform scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(16f, 48f);
            scrollRt.offsetMax = new Vector2(-16f, -56f);
            scrollGo.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            RectTransform viewportRt = (RectTransform)viewport.transform;
            StretchFull(viewportRt);

            _scrollRect = scrollGo.GetComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(LayoutElement));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            LayoutElement contentLe = content.GetComponent<LayoutElement>();
            contentLe.flexibleWidth = 1f;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _bodyText = content.AddComponent<TextMeshProUGUI>();
            _bodyText.fontSize = 15f;
            _bodyText.alignment = TextAlignmentOptions.TopLeft;
            _bodyText.textWrappingMode = TextWrappingModes.Normal;
            _bodyText.color = new Color(0.9f, 0.92f, 0.95f, 1f);
            _bodyText.margin = new Vector4(12f, 12f, 12f, 12f);
            _bodyText.raycastTarget = false;

            _scrollRect.content = contentRt;
            _scrollRect.viewport = viewportRt;

            _footerText = CreateText(panel.transform, "Footer", 13f, FontStyles.Normal);
            RectTransform footerRt = (RectTransform)_footerText.transform;
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.sizeDelta = new Vector2(-32f, 28f);
            footerRt.anchoredPosition = new Vector2(0f, 12f);
            _footerText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.raycastTarget = false;
            return text;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
