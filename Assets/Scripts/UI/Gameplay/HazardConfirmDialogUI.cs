using System;
using JRogue.Actors;
using JRogue.Hazards;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class HazardConfirmDialogUI : MonoBehaviour
    {
        static HazardConfirmDialogUI _instance;

        GameObject _modalRoot;
        TextMeshProUGUI _bodyText;
        Action _onYes;
        bool _blocking;

        public static bool BlocksGameplay =>
            (_instance != null && _instance._blocking)
            || AutoPickupConfirmDialogUI.BlocksGameplay;

        public static HazardConfirmDialogUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(HazardConfirmDialogUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<HazardConfirmDialogUI>();
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
                CommitYes();
            else if (kb.nKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
                Cancel();
        }

        public void Show(BaseActor mover, EnvironmentalHazardDefinition hazard, Action onYes)
        {
            EnsureModalBuilt();
            _onYes = onYes;
            _blocking = true;

            if (_bodyText != null)
            {
                string actorName = mover != null ? mover.DisplayName : "Party member";
                string hazardName = hazard != null ? hazard.displayName : "hazard";
                _bodyText.text =
                    $"{actorName} is about to enter {hazardName}. Entering may harm you each turn you remain inside. Continue?\n\n" +
                    "<size=14><color=#9bbdff>Y</color> confirm   ·   <color=#ffb28a>N</color> cancel</size>";
            }

            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }

        void CommitYes()
        {
            Action act = _onYes;
            Close();
            act?.Invoke();
        }

        void Cancel() => Close();

        void Close()
        {
            _blocking = false;
            _onYes = null;
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
        }

        void EnsureModalBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject("HazardConfirmCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 501;

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
            bubbleRt.sizeDelta = new Vector2(520f, 280f);

            Image bubbleBg = bubble.GetComponent<Image>();
            bubbleBg.color = new Color(0.12f, 0.14f, 0.2f, 0.98f);

            VerticalLayoutGroup vlg = bubble.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            bodyGo.transform.SetParent(bubble.transform, false);
            _bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
            _bodyText.fontSize = 16f;
            _bodyText.color = Color.white;
            _bodyText.alignment = TextAlignmentOptions.TopLeft;
            _bodyText.textWrappingMode = TextWrappingModes.Normal;

            LayoutElement bodyLe = bodyGo.GetComponent<LayoutElement>();
            bodyLe.minHeight = 140f;
            bodyLe.flexibleHeight = 1f;

            _modalRoot.SetActive(false);
        }
    }
}
