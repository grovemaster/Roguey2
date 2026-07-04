using System;
using JRogue.Actors;
using JRogue.Item;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    public sealed class InventoryGiveQuantityDialogUI : MonoBehaviour
    {
        static InventoryGiveQuantityDialogUI _instance;

        GameObject _modalRoot;
        TextMeshProUGUI _summaryText;
        TextMeshProUGUI _recipientText;
        TextMeshProUGUI _rangeText;
        TextMeshProUGUI _hintText;
        TMP_InputField _quantityInput;
        Button _minusButton;
        Button _plusButton;
        Button _giveButton;
        Button _cancelButton;
        Image _iconImage;

        int _maxQuantity;
        Action<int> _onConfirmed;
        bool _open;

        public static bool IsOpen => _instance != null && _instance._open;

        public static InventoryGiveQuantityDialogUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(InventoryGiveQuantityDialogUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<InventoryGiveQuantityDialogUI>();
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
            {
                Hide();
                return;
            }

            if (Keyboard.current.enterKey.wasPressedThisFrame
                && _giveButton != null
                && _giveButton.interactable
                && TryGetValidQuantity(out int quantity))
            {
                Confirm(quantity);
            }
        }

        public void Show(
            ItemInstance source,
            BaseActor recipient,
            int maxQuantity,
            Action<int> onConfirmed)
        {
            if (source == null || recipient == null || onConfirmed == null || maxQuantity < 2)
                return;

            EnsureBuilt();
            _maxQuantity = maxQuantity;
            _onConfirmed = onConfirmed;
            _open = true;
            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();

            string itemName = source.Definition?.itemName ?? "Item";
            _summaryText.text = $"{itemName}  ·  you have {maxQuantity}";
            _recipientText.text = $"To: {recipient.DisplayName}";
            _rangeText.text = $"1 – {maxQuantity}";
            _hintText.text = $"Enter a number from 1 to {maxQuantity}.";

            if (_iconImage != null)
            {
                Sprite icon = source.Definition?.icon;
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
                _iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            }

            _quantityInput.SetTextWithoutNotify("1");
            RefreshControls();
            _quantityInput.ActivateInputField();
            _quantityInput.Select();
        }

        public void Hide()
        {
            _open = false;
            _onConfirmed = null;
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
        }

        void Confirm(int quantity)
        {
            Action<int> callback = _onConfirmed;
            Hide();
            callback?.Invoke(quantity);
        }

        void OnQuantityTextChanged(string _)
        {
            RefreshControls();
        }

        void AdjustQuantity(int delta)
        {
            int current = 1;
            if (TryGetValidQuantity(out int parsed))
                current = parsed;
            else if (int.TryParse(_quantityInput.text, out int raw))
                current = raw;

            int next = Mathf.Clamp(current + delta, 1, _maxQuantity);
            _quantityInput.SetTextWithoutNotify(next.ToString());
            RefreshControls();
        }

        void RefreshControls()
        {
            bool valid = TryGetValidQuantity(out int quantity);
            _giveButton.interactable = valid;
            _hintText.gameObject.SetActive(!valid);

            _minusButton.interactable = !valid || quantity > 1;
            _plusButton.interactable = !valid || quantity < _maxQuantity;
        }

        bool TryGetValidQuantity(out int quantity)
        {
            quantity = 0;
            string text = _quantityInput != null ? _quantityInput.text?.Trim() : null;
            if (string.IsNullOrEmpty(text))
                return false;

            if (!int.TryParse(text, out quantity))
                return false;

            return quantity >= 1 && quantity <= _maxQuantity;
        }

        void EnsureBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject(
                "GiveQuantityCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 521;

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
            panelRt.sizeDelta = new Vector2(480f, 320f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(panel.transform, false);
            TextMeshProUGUI title = titleGo.GetComponent<TextMeshProUGUI>();
            title.text = "How many?";
            title.fontSize = 20f;
            title.fontStyle = FontStyles.Bold;

            var summaryRow = new GameObject("SummaryRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            summaryRow.transform.SetParent(panel.transform, false);
            HorizontalLayoutGroup summaryLayout = summaryRow.GetComponent<HorizontalLayoutGroup>();
            summaryLayout.spacing = 12f;
            summaryLayout.childAlignment = TextAnchor.MiddleLeft;
            summaryLayout.childControlWidth = false;
            summaryLayout.childForceExpandWidth = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(summaryRow.transform, false);
            LayoutElement iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.minWidth = iconLe.preferredWidth = 40f;
            iconLe.minHeight = iconLe.preferredHeight = 40f;
            _iconImage = iconGo.GetComponent<Image>();
            _iconImage.preserveAspect = true;

            var summaryTextGo = new GameObject("Summary", typeof(RectTransform), typeof(TextMeshProUGUI));
            summaryTextGo.transform.SetParent(summaryRow.transform, false);
            LayoutElement summaryLe = summaryTextGo.AddComponent<LayoutElement>();
            summaryLe.flexibleWidth = 1f;
            _summaryText = summaryTextGo.GetComponent<TextMeshProUGUI>();
            _summaryText.fontSize = 16f;
            _summaryText.alignment = TextAlignmentOptions.MidlineLeft;

            var recipientGo = new GameObject("Recipient", typeof(RectTransform), typeof(TextMeshProUGUI));
            recipientGo.transform.SetParent(panel.transform, false);
            _recipientText = recipientGo.GetComponent<TextMeshProUGUI>();
            _recipientText.fontSize = 15f;
            _recipientText.color = new Color(0.75f, 0.8f, 0.86f);

            var qtyLabelGo = new GameObject("QtyLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            qtyLabelGo.transform.SetParent(panel.transform, false);
            TextMeshProUGUI qtyLabel = qtyLabelGo.GetComponent<TextMeshProUGUI>();
            qtyLabel.text = "Give quantity";
            qtyLabel.fontSize = 14f;

            var qtyRow = new GameObject("QtyRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            qtyRow.transform.SetParent(panel.transform, false);
            HorizontalLayoutGroup qtyLayout = qtyRow.GetComponent<HorizontalLayoutGroup>();
            qtyLayout.spacing = 10f;
            qtyLayout.childAlignment = TextAnchor.MiddleCenter;
            qtyLayout.childControlWidth = false;
            qtyLayout.childForceExpandWidth = false;

            _minusButton = CreateIconButton(qtyRow.transform, "−", () => AdjustQuantity(-1), 44f);
            _quantityInput = CreateQuantityInput(qtyRow.transform);
            _quantityInput.onValueChanged.AddListener(OnQuantityTextChanged);
            _plusButton = CreateIconButton(qtyRow.transform, "+", () => AdjustQuantity(1), 44f);

            var rangeGo = new GameObject("Range", typeof(RectTransform), typeof(TextMeshProUGUI));
            rangeGo.transform.SetParent(panel.transform, false);
            _rangeText = rangeGo.GetComponent<TextMeshProUGUI>();
            _rangeText.fontSize = 14f;
            _rangeText.alignment = TextAlignmentOptions.Center;
            _rangeText.color = new Color(0.7f, 0.76f, 0.82f);

            var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintGo.transform.SetParent(panel.transform, false);
            _hintText = hintGo.GetComponent<TextMeshProUGUI>();
            _hintText.fontSize = 13f;
            _hintText.alignment = TextAlignmentOptions.Center;
            _hintText.color = new Color(0.9f, 0.55f, 0.45f);
            _hintText.gameObject.SetActive(false);

            var actionsRow = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionsRow.transform.SetParent(panel.transform, false);
            HorizontalLayoutGroup actionsLayout = actionsRow.GetComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 12f;
            actionsLayout.childAlignment = TextAnchor.MiddleCenter;
            actionsLayout.childControlWidth = true;
            actionsLayout.childForceExpandWidth = true;

            _giveButton = CreateActionButton(actionsRow.transform, "Give", () =>
            {
                if (TryGetValidQuantity(out int quantity))
                    Confirm(quantity);
            });
            _cancelButton = CreateActionButton(actionsRow.transform, "Cancel", Hide);
        }

        static TMP_InputField CreateQuantityInput(Transform parent)
        {
            var go = new GameObject("Quantity", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 88f;
            le.minHeight = le.preferredHeight = 40f;

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            var viewportGo = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(go.transform, false);
            StretchFull((RectTransform)viewportGo.transform);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(viewportGo.transform, false);
            RectTransform textRt = (RectTransform)textGo.transform;
            StretchFull(textRt);
            textRt.offsetMin = new Vector2(8f, 4f);
            textRt.offsetMax = new Vector2(-8f, -4f);
            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            text.fontSize = 18f;
            text.alignment = TextAlignmentOptions.Center;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGo.transform.SetParent(viewportGo.transform, false);
            RectTransform placeholderRt = (RectTransform)placeholderGo.transform;
            StretchFull(placeholderRt);
            TextMeshProUGUI placeholder = placeholderGo.GetComponent<TextMeshProUGUI>();
            placeholder.fontSize = 18f;
            placeholder.alignment = TextAlignmentOptions.Center;
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            placeholder.text = "1";

            TMP_InputField input = go.GetComponent<TMP_InputField>();
            input.textViewport = (RectTransform)viewportGo.transform;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onFocusSelectAll = true;
            return input;
        }

        static Button CreateIconButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, float size)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = size;
            le.minHeight = le.preferredHeight = size;
            go.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.2f, 1f);
            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            StretchFull((RectTransform)textGo.transform);
            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            return button;
        }

        static Button CreateActionButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = 36f;
            go.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.2f, 1f);
            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            StretchFull((RectTransform)textGo.transform);
            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16f;
            tmp.alignment = TextAlignmentOptions.Center;
            return button;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
