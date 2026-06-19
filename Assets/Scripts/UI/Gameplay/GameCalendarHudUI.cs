using JRogue.World.Generation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class GameCalendarHudUI : MonoBehaviour
    {
        static GameCalendarHudUI _instance;

        GameObject _panelRoot;
        TextMeshProUGUI _dateText;
        GameCalendarService _calendar;

        public static GameCalendarHudUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(GameCalendarHudUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GameCalendarHudUI>();
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
        }

        void OnEnable()
        {
            SubscribeCalendar();
            RefreshDate();
        }

        void OnDisable() => UnsubscribeCalendar();

        void OnDestroy()
        {
            UnsubscribeCalendar();
            if (_instance == this)
                _instance = null;
        }

        void Update()
        {
            if (_calendar == null)
                SubscribeCalendar();

            bool show = _calendar != null && _calendar.IsEnabled;
            if (_panelRoot != null && _panelRoot.activeSelf != show)
                _panelRoot.SetActive(show);
        }

        void SubscribeCalendar()
        {
            if (_calendar != null)
                return;

            _calendar = GameCalendarService.Instance;
            if (_calendar == null)
                return;

            _calendar.DateChanged += OnDateChanged;
        }

        void UnsubscribeCalendar()
        {
            if (_calendar == null)
                return;

            _calendar.DateChanged -= OnDateChanged;
            _calendar = null;
        }

        void OnDateChanged(GameCalendarDate _) => RefreshDate();

        void RefreshDate()
        {
            if (_dateText == null)
                return;

            GameCalendarService calendar = GameCalendarService.Instance;
            _dateText.text = calendar != null && calendar.IsEnabled
                ? calendar.FormatCurrentDate()
                : string.Empty;
        }

        void BuildUi()
        {
            var canvasGo = new GameObject(
                "GameCalendarCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 47;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _panelRoot = new GameObject("CalendarDatePanel", typeof(RectTransform), typeof(Image));
            _panelRoot.transform.SetParent(canvasGo.transform, false);
            RectTransform panelRt = (RectTransform)_panelRoot.transform;
            panelRt.anchorMin = new Vector2(1f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.anchoredPosition = new Vector2(-16f, -12f);
            panelRt.sizeDelta = new Vector2(360f, 40f);
            _panelRoot.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.82f);

            _dateText = CreateText(_panelRoot.transform, "DateText", string.Empty, 20f);
            RectTransform textRt = (RectTransform)_dateText.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 4f);
            textRt.offsetMax = new Vector2(-12f, -4f);
            _dateText.alignment = TextAlignmentOptions.MidlineRight;
            _dateText.color = new Color(0.88f, 0.9f, 0.94f, 1f);
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
