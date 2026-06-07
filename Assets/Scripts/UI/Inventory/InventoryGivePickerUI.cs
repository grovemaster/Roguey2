using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Party;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    public sealed class InventoryGivePickerUI : MonoBehaviour
    {
        static InventoryGivePickerUI _instance;

        GameObject _modalRoot;
        Transform _buttonRow;
        Action<BaseActor> _onPicked;
        bool _open;

        public static bool IsOpen => _instance != null && _instance._open;

        public static InventoryGivePickerUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(InventoryGivePickerUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<InventoryGivePickerUI>();
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
            EnsureBuilt();
            _modalRoot.SetActive(false);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Update()
        {
            if (!_open || Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Hide();
        }

        public void Show(BaseActor giver, Action<BaseActor> onPicked)
        {
            EnsureBuilt();
            _onPicked = onPicked;
            _open = true;
            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
            RebuildButtons(giver);
        }

        public void Hide()
        {
            _open = false;
            _onPicked = null;
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
        }

        void RebuildButtons(BaseActor giver)
        {
            for (int i = _buttonRow.childCount - 1; i >= 0; i--)
                Destroy(_buttonRow.GetChild(i).gameObject);

            List<BaseActor> members = PartyManager.Instance?.partyMembers;
            if (members == null)
                return;

            foreach (BaseActor member in members)
            {
                if (member == null || member == giver)
                    continue;

                BaseActor target = member;
                Button button = CreateButton(target.DisplayName, () =>
                {
                    _onPicked?.Invoke(target);
                    Hide();
                });
                button.transform.SetParent(_buttonRow, false);
            }
        }

        Button CreateButton(string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Pick", typeof(RectTransform), typeof(Image), typeof(Button));
            go.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.2f, 1f);
            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform rt = (RectTransform)textGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12f, 6f);
            rt.offsetMax = new Vector2(-12f, -6f);
            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16f;
            tmp.alignment = TextAlignmentOptions.Center;

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = 36f;
            return button;
        }

        void EnsureBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject(
                "GivePickerCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 520;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _modalRoot = new GameObject("Modal", typeof(RectTransform), typeof(Image));
            _modalRoot.transform.SetParent(canvasGo.transform, false);
            StretchFull((RectTransform)_modalRoot.transform);
            _modalRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(_modalRoot.transform, false);
            RectTransform panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(420f, 260f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(panel.transform, false);
            TextMeshProUGUI title = titleGo.GetComponent<TextMeshProUGUI>();
            title.text = "Give to whom?";
            title.fontSize = 20f;
            title.fontStyle = FontStyles.Bold;

            var rowGo = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rowGo.transform.SetParent(panel.transform, false);
            VerticalLayoutGroup rowLayout = rowGo.GetComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            _buttonRow = rowGo.transform;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
