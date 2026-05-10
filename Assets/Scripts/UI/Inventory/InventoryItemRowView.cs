using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    [DisallowMultipleComponent]
    public class InventoryItemRowView : MonoBehaviour
    {
        TextMeshProUGUI _letterText;
        TextMeshProUGUI _detailsText;
        Image _iconImage;
        Button _button;
        Image _backing;
        bool _layoutReady;

        public Button Button => _button;

        public void EnsureLayoutBuilt()
        {
            if (_layoutReady) return;
            _layoutReady = true;

            _button = GetComponent<Button>();
            if (_button == null)
                _button = gameObject.AddComponent<Button>();

            _backing = _button.targetGraphic as Image;
            if (_backing == null)
            {
                _backing = GetComponent<Image>();
                if (_backing == null)
                    _backing = gameObject.AddComponent<Image>();

                _button.targetGraphic = _backing;
            }

            var h = gameObject.GetComponent<HorizontalLayoutGroup>() ?? gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(6, 8, 2, 2);
            h.spacing = 6;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlHeight = true;
            h.childControlWidth = true;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;

            // Keep transition off so Graphic.color we set isn't overridden every frame.
            _button.transition = Selectable.Transition.None;

            if (transform.childCount == 0)
            {
                _letterText = CreateLetterChild();
                _iconImage = CreateIconChild();
                _detailsText = CreateDetailsChild();
            }
            else
            {
                Transform textChild = transform.GetChild(0);
                _detailsText = textChild.GetComponent<TextMeshProUGUI>();
                if (_detailsText == null)
                    _detailsText = textChild.gameObject.AddComponent<TextMeshProUGUI>();

                var letterGo = new GameObject("Letter", typeof(RectTransform));
                letterGo.transform.SetParent(transform, false);
                letterGo.transform.SetAsFirstSibling();
                _letterText = letterGo.AddComponent<TextMeshProUGUI>();
                SetupLetter(_letterText);

                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(transform, false);
                iconGo.transform.SetSiblingIndex(1);
                _iconImage = iconGo.AddComponent<Image>();
                SetupIcon(_iconImage);

                var leL = letterGo.GetComponent<LayoutElement>() ?? letterGo.AddComponent<LayoutElement>();
                leL.minWidth = 28;
                leL.preferredWidth = 28;

                var leI = iconGo.GetComponent<LayoutElement>() ?? iconGo.AddComponent<LayoutElement>();
                leI.preferredHeight = 40;
                leI.preferredWidth = 40;
                leI.flexibleHeight = 0;
                leI.flexibleWidth = 0;

                LayoutElement flex = textChild.GetComponent<LayoutElement>() ?? textChild.gameObject.AddComponent<LayoutElement>();
                flex.flexibleWidth = 1;
                flex.flexibleHeight = 1;
                flex.minHeight = 40f;

                ConfigureDetailsTMP(_detailsText);
                AttachDetailsSizeFitter(textChild.gameObject);
            }
        }

        TextMeshProUGUI CreateLetterChild()
        {
            var go = new GameObject("Letter", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            SetupLetter(tmp);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 28;
            le.preferredWidth = 28;
            return tmp;
        }

        Image CreateIconChild()
        {
            var go = new GameObject("Icon", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>();
            SetupIcon(img);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.preferredWidth = 40;
            return img;
        }

        TextMeshProUGUI CreateDetailsChild()
        {
            var go = new GameObject("Details", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            ConfigureDetailsTMP(tmp);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            le.minHeight = 40f;
            AttachDetailsSizeFitter(go);
            return tmp;
        }

        static void AttachDetailsSizeFitter(GameObject detailsGo)
        {
            var csf = detailsGo.GetComponent<ContentSizeFitter>() ?? detailsGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void SetupLetter(TextMeshProUGUI tmp)
        {
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.color = new Color(0.92f, 0.93f, 0.94f);
            tmp.text = "?";
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
        }

        static void SetupIcon(Image img)
        {
            img.color = Color.white;
            img.preserveAspect = true;
        }

        static void ConfigureDetailsTMP(TextMeshProUGUI tmp)
        {
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.verticalAlignment = VerticalAlignmentOptions.Top;
            tmp.color = new Color(0.92f, 0.93f, 0.94f);
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.textWrappingMode = TextWrappingModes.Normal;
        }

        public void Bind(
            InventoryViewModel.Row row,
            Color nameColor,
            Action onClicked,
            Sprite itemIcon,
            Sprite placeholderIcon)
        {
            _letterText.text = row.Letter.ToString();

            int qty = row.Instance != null ? row.Instance.Quantity : 1;
            string qtyLabel = qty > 1 ? $" ×{qty}" : string.Empty;
            string slot = row.Item != null ? row.Item.slotType.ToString() : "?";
            string weight = $"{row.StackedWeight:0.#} kg";
            string equipped = string.Empty;
            if (row.IsEquipped && row.EquippedSlot.HasValue)
                equipped = $"  <color=#7dd3fc>[E {row.EquippedSlot} · {row.OwnerDisplayName}]</color>";

            string nameHex = ColorUtility.ToHtmlStringRGB(nameColor);
            string baseName = row.Item != null ? row.Item.itemName : "(?)";
            string idShort = row.Instance != null && row.Instance.Id != null && row.Instance.Id.Length >= 6
                ? row.Instance.Id.Substring(0, 6)
                : "";

            string nameWithId = string.IsNullOrEmpty(idShort)
                ? baseName
                : $"{baseName} <color=#5a6a72><size=11>#{idShort}</size></color>";

            _detailsText.text =
                $"<color=#{nameHex}>{nameWithId}</color>{qtyLabel}  ·  {row.OwnerDisplayName}  ·  [{slot}]  ·  {weight}{equipped}";

            Sprite use = itemIcon != null ? itemIcon : placeholderIcon;
            _iconImage.sprite = use;
            _iconImage.color = itemIcon != null ? Color.white : new Color(0.45f, 0.45f, 0.48f, 0.9f);

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClicked?.Invoke());
        }

        public void SetSelected(bool selected, Color selectedTint, Color normalTint)
        {
            if (_backing == null) return;
            _backing.color = selected ? selectedTint : normalTint;
        }

        public void SetInteractable(bool interactable) => _button.interactable = interactable;
    }
}
