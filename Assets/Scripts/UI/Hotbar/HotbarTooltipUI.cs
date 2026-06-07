using System.Collections;
using JRogue.Ability;
using JRogue.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Hotbar
{
    public sealed class HotbarTooltipUI : MonoBehaviour
    {
        const float ShowDelaySeconds = 0.3f;

        static HotbarTooltipUI _instance;

        GameObject _root;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _bodyText;
        TextMeshProUGUI _footerText;
        Coroutine _showRoutine;

        public static HotbarTooltipUI Instance => _instance;

        public static HotbarTooltipUI EnsureInstance(Transform parent)
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(HotbarTooltipUI));
            go.transform.SetParent(parent, false);
            _instance = go.AddComponent<HotbarTooltipUI>();
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
            Build();
            _root.SetActive(false);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public void ShowDelayed(
            RectTransform anchor,
            string title,
            string description,
            string footer,
            Canvas rootCanvas)
        {
            HideImmediate();
            _showRoutine = StartCoroutine(ShowAfterDelay(anchor, title, description, footer, rootCanvas));
        }

        public void HideImmediate()
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            if (_root != null)
                _root.SetActive(false);
        }

        IEnumerator ShowAfterDelay(
            RectTransform anchor,
            string title,
            string description,
            string footer,
            Canvas rootCanvas)
        {
            yield return new WaitForSeconds(ShowDelaySeconds);
            Present(anchor, title, description, footer, rootCanvas);
            _showRoutine = null;
        }

        void Present(
            RectTransform anchor,
            string title,
            string description,
            string footer,
            Canvas rootCanvas)
        {
            if (_root == null || anchor == null || rootCanvas == null)
                return;

            _titleText.text = string.IsNullOrEmpty(title) ? "Ability" : title;
            _bodyText.text = string.IsNullOrEmpty(description) ? "No description." : description;
            _footerText.text = footer ?? string.Empty;

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();

            RectTransform tooltipRt = (RectTransform)_root.transform;
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[1]);

            RectTransform canvasRt = rootCanvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt,
                    screenPoint + new Vector2(0f, 12f),
                    rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
                    out Vector2 localPoint))
            {
                tooltipRt.anchoredPosition = localPoint;
            }
        }

        public static string BuildFooter(HotbarResolvedAction resolved, string keyHint)
        {
            AbilityAction ability = resolved.Ability;
            if (ability == null)
                return keyHint ?? string.Empty;

            var parts = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(keyHint))
                parts.Append(keyHint);

            if (ability.soulPowerCost > 0)
                AppendPart(parts, $"Soul Power: {ability.soulPowerCost}");
            if (ability.magicPowerCost > 0)
                AppendPart(parts, $"Magic Power: {ability.magicPowerCost}");
            if (resolved.ItemInstance != null && resolved.ItemInstance.Quantity > 1)
                AppendPart(parts, $"Qty: {resolved.ItemInstance.Quantity}");

            return parts.ToString();
        }

        static void AppendPart(System.Text.StringBuilder builder, string part)
        {
            if (builder.Length > 0)
                builder.Append("   ·   ");
            builder.Append(part);
        }

        void Build()
        {
            _root = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(transform, false);
            RectTransform rt = (RectTransform)_root.transform;
            rt.sizeDelta = new Vector2(320f, 0f);

            Image bg = _root.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);

            var layout = _root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            var fitter = _root.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _titleText = CreateText("Title", 16f, FontStyles.Bold);
            _bodyText = CreateText("Body", 13f, FontStyles.Normal);
            _footerText = CreateText("Footer", 11f, FontStyles.Italic);
            _footerText.color = new Color(0.7f, 0.75f, 0.82f, 1f);
        }

        static TextMeshProUGUI CreateText(string name, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }
    }
}
