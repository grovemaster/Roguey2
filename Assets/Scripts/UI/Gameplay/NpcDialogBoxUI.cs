using System;
using System.Collections.Generic;
using JRogue.Dialog;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class NpcDialogBoxUI : MonoBehaviour
    {
        const float PanelHeightFraction = 0.27f;
        const float PortraitWidth = 250f;
        const float PortraitHeight = 250f;
        const float PortraitTopOverlap = 52f;
        const float NameFontSize = 40f;
        const float BodyFontSize = 30f;
        const float NameLeft = 40f;
        const float NameTop = -20f;
        const float NameRowHeight = 52f;
        const float BodyLeft = 40f;
        const float BodyRight = 24f;
        const float BodyBottom = 36f;
        const float BodyTopGap = 8f;
        const float BodyTopInset = NameTop - NameRowHeight - BodyTopGap;
        const float ChoiceBodyBottom = 152f;
        static readonly Color OuterBorderColor = new Color(0.16f, 0.09f, 0.06f, 1f);
        static readonly Color InnerBorderColor = new Color(0.78f, 0.63f, 0.38f, 1f);
        static readonly Color PanelFillColor = new Color(0.08f, 0.1f, 0.14f, 0.94f);
        static readonly Color PortraitFrameFillColor = new Color(0.05f, 0.07f, 0.1f, 0.98f);
        static readonly Color SpeakerNameColor = new Color(0.45f, 0.88f, 0.92f, 1f);

        static NpcDialogBoxUI _instance;

        GameObject _panelRoot;
        Image _portraitImage;
        TextMeshProUGUI _nameText;
        TextMeshProUGUI _bodyText;
        TextMeshProUGUI _hintText;
        GameObject _choiceContainer;
        readonly List<Button> _choiceButtons = new List<Button>();
        bool _blocking;
        bool _suppressConfirmUntilReleased;
        Action _onAdvance;
        Action<DialogChoiceOptionData> _onChoice;
        Action _onDismiss;

        public static bool BlocksGameplay =>
            _instance != null && _instance._blocking;

        public static NpcDialogBoxUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(NpcDialogBoxUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<NpcDialogBoxUI>();
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
            EnsurePanelBuilt();
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
            if (_suppressConfirmUntilReleased)
            {
                if (!kb.enterKey.isPressed && !kb.spaceKey.isPressed)
                    _suppressConfirmUntilReleased = false;
                else
                    return;
            }

            if (_onChoice != null)
            {
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                    MoveChoiceSelection(-1);
                else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                    MoveChoiceSelection(1);
                else if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                    CommitSelectedChoice();
                else if (kb.escapeKey.wasPressedThisFrame)
                    Close();
                return;
            }

            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                Advance();
        }

        public void ShowLine(DialogLineStep step, Action onAdvance)
        {
            if (step == null)
                return;

            EnsurePanelBuilt();
            _onChoice = null;
            _onAdvance = onAdvance;
            _blocking = true;

            if (_nameText != null)
                _nameText.text = step.SpeakerName ?? string.Empty;
            if (_bodyText != null)
                _bodyText.text = step.ResolvedText ?? string.Empty;
            if (_hintText != null)
                _hintText.text = "Enter — Continue";

            ApplyPortrait(step.Portrait);
            SetChoiceMode(false);
            _suppressConfirmUntilReleased = true;
            _panelRoot.SetActive(true);
            _panelRoot.transform.SetAsLastSibling();
        }

        public void ShowChoice(DialogChoiceStep step, Action<DialogChoiceOptionData> onChoice, Action onDismissed = null)
        {
            if (step == null)
                return;

            EnsurePanelBuilt();
            _onAdvance = null;
            _onChoice = onChoice;
            _onDismiss = onDismissed;
            _blocking = true;

            if (_nameText != null)
                _nameText.text = step.SpeakerName ?? string.Empty;
            if (_bodyText != null)
                _bodyText.text = step.PromptText ?? string.Empty;
            if (_hintText != null)
                _hintText.text = "↑↓ — Choose    Enter — Confirm";

            ApplyPortrait(step.Portrait);
            BuildChoiceButtons(step.Options);
            SetChoiceMode(true);
            _suppressConfirmUntilReleased = true;
            _panelRoot.SetActive(true);
            _panelRoot.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (_onChoice != null)
                _onDismiss?.Invoke();

            _blocking = false;
            _suppressConfirmUntilReleased = false;
            _onAdvance = null;
            _onChoice = null;
            _onDismiss = null;
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        void Advance()
        {
            Action act = _onAdvance;
            _onAdvance = null;
            act?.Invoke();
        }

        void ApplyPortrait(PortraitDefinition portrait)
        {
            if (_portraitImage == null)
                return;

            if (portrait != null && portrait.portrait != null)
            {
                _portraitImage.sprite = portrait.portrait;
                _portraitImage.color = Color.white;
                _portraitImage.preserveAspect = true;
            }
            else
            {
                _portraitImage.sprite = null;
                _portraitImage.color = new Color(0.2f, 0.22f, 0.28f, 1f);
            }
        }

        void SetChoiceMode(bool visible)
        {
            if (_choiceContainer != null)
                _choiceContainer.SetActive(visible);
            if (_hintText != null)
                _hintText.gameObject.SetActive(true);

            if (_bodyText != null)
            {
                RectTransform bodyRt = (RectTransform)_bodyText.transform;
                bodyRt.offsetMin = new Vector2(BodyLeft, visible ? ChoiceBodyBottom : BodyBottom);
                bodyRt.offsetMax = new Vector2(-BodyRight, BodyTopInset);
            }
        }

        void BuildChoiceButtons(IReadOnlyList<DialogChoiceOptionData> options)
        {
            ClearChoiceButtons();
            if (_choiceContainer == null || options == null)
                return;

            int selectedIndex = 0;
            for (int i = 0; i < options.Count; i++)
            {
                DialogChoiceOptionData option = options[i];
                Button button = CreateChoiceButton(_choiceContainer.transform, option.label, i);
                int captured = i;
                button.onClick.AddListener(() => SelectChoice(options[captured]));
                _choiceButtons.Add(button);
            }

            HighlightChoice(selectedIndex);
        }

        void ClearChoiceButtons()
        {
            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                if (_choiceButtons[i] != null)
                    Destroy(_choiceButtons[i].gameObject);
            }

            _choiceButtons.Clear();
        }

        void MoveChoiceSelection(int delta)
        {
            if (_choiceButtons.Count == 0)
                return;

            int current = GetSelectedChoiceIndex();
            int next = (current + delta + _choiceButtons.Count) % _choiceButtons.Count;
            HighlightChoice(next);
        }

        void CommitSelectedChoice()
        {
            if (_choiceButtons.Count == 0)
                return;

            _choiceButtons[GetSelectedChoiceIndex()].onClick.Invoke();
        }

        int GetSelectedChoiceIndex()
        {
            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                Image bg = _choiceButtons[i].GetComponent<Image>();
                if (bg != null && bg.color.g > 0.45f)
                    return i;
            }

            return 0;
        }

        void HighlightChoice(int index)
        {
            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                Image bg = _choiceButtons[i].GetComponent<Image>();
                if (bg == null)
                    continue;

                bg.color = i == index
                    ? new Color(0.35f, 0.55f, 0.75f, 0.95f)
                    : new Color(0.15f, 0.18f, 0.24f, 0.95f);
            }
        }

        void SelectChoice(DialogChoiceOptionData option)
        {
            Action<DialogChoiceOptionData> act = _onChoice;
            _onChoice = null;
            _onDismiss = null;
            SetChoiceMode(false);
            act?.Invoke(option);
        }

        Button CreateChoiceButton(Transform parent, string label, int index)
        {
            var go = new GameObject($"Choice_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(0f, 36f);

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.18f, 0.24f, 0.95f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 2f);
            textRt.offsetMax = new Vector2(-12f, -2f);

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            return button;
        }

        void EnsurePanelBuilt()
        {
            if (_panelRoot != null)
                return;

            var canvasGo = new GameObject("NpcDialogCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 510;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _panelRoot = CreateBorderedPanel(canvasGo.transform, "DialogPanel", PanelFillColor, OuterBorderColor, InnerBorderColor);
            RectTransform panelRt = (RectTransform)_panelRoot.transform;
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(1f, PanelHeightFraction);
            panelRt.offsetMin = new Vector2(12f, 12f);
            panelRt.offsetMax = new Vector2(-12f, 0f);

            Transform contentRoot = _panelRoot.transform.Find("Inner/Fill");
            if (contentRoot == null)
                contentRoot = _panelRoot.transform;

            _nameText = CreateTmp(contentRoot, "Speaker", NameFontSize, FontStyles.Bold);
            _nameText.color = SpeakerNameColor;
            RectTransform nameRt = (RectTransform)_nameText.transform;
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0f, 1f);
            nameRt.offsetMin = new Vector2(NameLeft, NameTop - NameRowHeight);
            nameRt.offsetMax = new Vector2(-BodyRight, NameTop);

            _bodyText = CreateTmp(contentRoot, string.Empty, BodyFontSize, FontStyles.Normal);
            RectTransform bodyRt = (RectTransform)_bodyText.transform;
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(BodyLeft, BodyBottom);
            bodyRt.offsetMax = new Vector2(-BodyRight, BodyTopInset);
            _bodyText.alignment = TextAlignmentOptions.TopLeft;
            _bodyText.textWrappingMode = TextWrappingModes.Normal;
            _bodyText.lineSpacing = 4f;

            _choiceContainer = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup));
            _choiceContainer.transform.SetParent(contentRoot, false);
            RectTransform choiceRt = (RectTransform)_choiceContainer.transform;
            choiceRt.anchorMin = new Vector2(0f, 0f);
            choiceRt.anchorMax = new Vector2(1f, 0f);
            choiceRt.pivot = new Vector2(0.5f, 0f);
            choiceRt.offsetMin = new Vector2(24f, 36f);
            choiceRt.offsetMax = new Vector2(-24f, 148f);
            VerticalLayoutGroup choiceVlg = _choiceContainer.GetComponent<VerticalLayoutGroup>();
            choiceVlg.spacing = 6f;
            choiceVlg.childControlWidth = true;
            choiceVlg.childForceExpandWidth = true;

            _hintText = CreateTmp(contentRoot, "Enter — Continue", 15, FontStyles.Italic);
            _hintText.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            _hintText.alignment = TextAlignmentOptions.BottomRight;
            RectTransform hintRt = (RectTransform)_hintText.transform;
            hintRt.anchorMin = new Vector2(1f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(1f, 0f);
            hintRt.sizeDelta = new Vector2(260f, 24f);
            hintRt.anchoredPosition = new Vector2(-20f, 10f);

            GameObject portraitFrame = CreateBorderedPanel(
                _panelRoot.transform,
                "PortraitFrame",
                PortraitFrameFillColor,
                OuterBorderColor,
                InnerBorderColor);
            RectTransform portraitFrameRt = (RectTransform)portraitFrame.transform;
            portraitFrameRt.anchorMin = new Vector2(0f, 1f);
            portraitFrameRt.anchorMax = new Vector2(0f, 1f);
            portraitFrameRt.pivot = new Vector2(0f, 0f);
            portraitFrameRt.sizeDelta = new Vector2(PortraitWidth, PortraitHeight);
            portraitFrameRt.anchoredPosition = new Vector2(20f, PortraitTopOverlap);

            var portraitImageGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitImageGo.transform.SetParent(portraitFrame.transform, false);
            RectTransform portraitRt = (RectTransform)portraitImageGo.transform;
            Stretch(portraitRt, 8f);
            _portraitImage = portraitImageGo.GetComponent<Image>();

            portraitFrame.transform.SetAsLastSibling();

            _panelRoot.SetActive(false);
        }

        static TextMeshProUGUI CreateTmp(Transform parent, string text, float size, FontStyles style)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            return tmp;
        }

        static GameObject CreateBorderedPanel(
            Transform parent,
            string name,
            Color fill,
            Color outer,
            Color inner)
        {
            var outerGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            outerGo.transform.SetParent(parent, false);
            Image outerImage = outerGo.GetComponent<Image>();
            outerImage.color = outer;

            var innerGo = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            innerGo.transform.SetParent(outerGo.transform, false);
            Image innerImage = innerGo.GetComponent<Image>();
            innerImage.color = inner;
            Stretch((RectTransform)innerGo.transform, 3f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(innerGo.transform, false);
            Image fillImage = fillGo.GetComponent<Image>();
            fillImage.color = fill;
            Stretch((RectTransform)fillGo.transform, 2f);

            return outerGo;
        }

        static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
