using JRogue.Actors;
using JRogue.Input;
using JRogue.Manager.Floor;
using JRogue.Manager.Party;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    /// <summary>Gameplay HUD control for manual floor pickup (`,`).</summary>
    public sealed class FloorPickupHudButton : MonoBehaviour
    {
        static FloorPickupHudButton _instance;

        Button _button;
        TextMeshProUGUI _label;

        public static FloorPickupHudButton EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(FloorPickupHudButton));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FloorPickupHudButton>();
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
            BuildHud();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Update() => RefreshInteractable();

        void BuildHud()
        {
            var canvasGo = new GameObject("FloorPickupHudCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var btnGo = new GameObject("PickUpButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(canvasGo.transform, false);

            RectTransform rt = (RectTransform)btnGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(24f, 24f);
            rt.sizeDelta = new Vector2(120f, 36f);

            btnGo.GetComponent<Image>().color = new Color(0.16f, 0.2f, 0.28f, 0.95f);
            _button = btnGo.GetComponent<Button>();
            _button.onClick.AddListener(OnPickUpClicked);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            RectTransform textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            _label = textGo.GetComponent<TextMeshProUGUI>();
            _label.text = "Pick up ,";
            _label.fontSize = 14f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Color.white;
        }

        void OnPickUpClicked()
        {
            InputHandler handler = Object.FindAnyObjectByType<InputHandler>();
            if (handler != null && handler.TryApplyRecordedCommand(PlayerCommand.PickupFloorItems()))
                return;

            PartyManager party = PartyManager.Instance;
            BaseActor active = party?.GetActiveMember();
            if (active != null)
                FloorPickupCoordinator.TryBeginManualPickup(active);
        }

        void RefreshInteractable()
        {
            if (_button == null)
                return;

            _button.interactable = FloorPickupCoordinator.CanManualPickupNow();
        }
    }
}
