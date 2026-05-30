using System;
using JRogue.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    [DisallowMultipleComponent]
    public class InventoryItemRowView : MonoBehaviour
    {
        TextMeshProUGUI _letterText;
        Image _iconImage;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _subtitleText;
        TextMeshProUGUI _qtyText;
        TextMeshProUGUI _weightText;
        TextMeshProUGUI _valueText;
        Button _button;
        Image _backing;
        bool _layoutReady;

        public Button Button => _button;

        public void EnsureLayoutBuilt()
        {
            if (_layoutReady)
                return;
            _layoutReady = true;

            _button = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            _backing = _button.targetGraphic as Image;
            if (_backing == null)
            {
                _backing = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
                _button.targetGraphic = _backing;
            }

            _button.transition = Selectable.Transition.None;

            var h = GetComponent<HorizontalLayoutGroup>() ?? gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(6, 8, 4, 4);
            h.spacing = 6;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlHeight = true;
            h.childControlWidth = true;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;

            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            _letterText = CreateLetterColumn();
            _iconImage = CreateIconColumn();
            CreateNameColumn(out _titleText, out _subtitleText);
            _qtyText = CreateNumericColumn("Qty", 44f, TextAlignmentOptions.Right);
            _weightText = CreateNumericColumn("Wt", 52f, TextAlignmentOptions.Right);
            _valueText = CreateNumericColumn("Value", 56f, TextAlignmentOptions.Right);

            var rowLe = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            rowLe.minHeight = 52f;
            rowLe.preferredHeight = 56f;
            rowLe.flexibleWidth = 1f;
        }

        TextMeshProUGUI CreateLetterColumn()
        {
            var go = new GameObject("Letter", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 28f;
            le.preferredWidth = 28f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 17f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.color = new Color(0.92f, 0.93f, 0.94f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        Image CreateIconColumn()
        {
            var go = new GameObject("Icon", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 44f;
            le.preferredHeight = 44f;
            le.minWidth = 44f;
            var img = go.AddComponent<Image>();
            img.preserveAspect = true;
            img.color = Color.white;
            return img;
        }

        void CreateNameColumn(out TextMeshProUGUI title, out TextMeshProUGUI subtitle)
        {
            var col = new GameObject("NameColumn", typeof(RectTransform));
            col.transform.SetParent(transform, false);
            var le = col.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minWidth = 100f;

            var v = col.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(col.transform, false);
            title = titleGo.AddComponent<TextMeshProUGUI>();
            title.fontSize = 14f;
            title.alignment = TextAlignmentOptions.Left;
            title.richText = true;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.textWrappingMode = TextWrappingModes.NoWrap;

            var subGo = new GameObject("Subtitle", typeof(RectTransform));
            subGo.transform.SetParent(col.transform, false);
            var subLe = subGo.AddComponent<LayoutElement>();
            subLe.minHeight = 16f;
            subtitle = subGo.AddComponent<TextMeshProUGUI>();
            subtitle.fontSize = 11f;
            subtitle.alignment = TextAlignmentOptions.Left;
            subtitle.richText = true;
            subtitle.color = new Color(0.62f, 0.67f, 0.72f);
            subtitle.overflowMode = TextOverflowModes.Ellipsis;
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
        }

        TextMeshProUGUI CreateNumericColumn(string name, float width, TextAlignmentOptions align, Transform parent = null)
        {
            parent ??= transform;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 13f;
            tmp.alignment = align;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.color = new Color(0.88f, 0.91f, 0.94f);
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        public void Bind(
            InventoryViewModel.Row row,
            Color nameColor,
            bool showOwnerInSubtitle,
            Action onClicked,
            Sprite itemIcon,
            Sprite placeholderIcon,
            float detailsFontScale = 1f)
        {
            _letterText.text = row.Letter.ToString();
            _letterText.fontSize = 17f * detailsFontScale;

            string chargeCol = EvocableChargeRules.FormatChargeColumn(row.Instance, row.Item);
            if (chargeCol != null)
            {
                _qtyText.text = chargeCol;
            }
            else
            {
                int qty = row.Instance != null ? row.Instance.Quantity : 1;
                _qtyText.text = qty > 1 ? $"×{qty}" : "×1";
            }

            _qtyText.fontSize = 13f * detailsFontScale;

            _weightText.text = $"{row.StackedWeight:0.#}";
            _weightText.fontSize = 13f * detailsFontScale;

            string valueStr = InventoryValueDisplay.FormatListColumn(row.Instance, row.Item);
            _valueText.text = valueStr;
            _valueText.fontSize = 13f * detailsFontScale;
            _valueText.color = valueStr == InventoryValueDisplay.Unknown
                ? new Color(0.55f, 0.58f, 0.62f)
                : new Color(0.88f, 0.91f, 0.94f);

            string markPrefix = FormatUserMarksPrefix(row.Instance);
            string nameHex = ColorUtility.ToHtmlStringRGB(nameColor);
            string baseName = row.Item != null ? row.Item.itemName : "(?)";
            string idShort = row.Instance != null && row.Instance.Id != null && row.Instance.Id.Length >= 6
                ? row.Instance.Id.Substring(0, 6)
                : string.Empty;

            string nameWithId = string.IsNullOrEmpty(idShort)
                ? baseName
                : $"{baseName} <color=#5a6a72><size=11>#{idShort}</size></color>";

            _titleText.fontSize = 14f * detailsFontScale;
            _titleText.text = $"{markPrefix}<color=#{nameHex}>{nameWithId}</color>";

            var subParts = new System.Collections.Generic.List<string>();
            string slot = row.Item != null ? row.Item.slotType.ToString() : "?";
            subParts.Add(slot);
            if (showOwnerInSubtitle && !string.IsNullOrEmpty(row.OwnerDisplayName))
                subParts.Add(row.OwnerDisplayName);
            if (row.IsEquipped && row.EquippedSlot.HasValue)
                subParts.Add($"[E {row.EquippedSlot}]");

            if (row.Item is EvocableItemData evocableDef)
            {
                string recharge = EvocableChargeRules.FormatRechargeSubtitle(row.Instance, evocableDef);
                if (!string.IsNullOrEmpty(recharge))
                    subParts.Add(recharge);
            }

            string sub = string.Join(" · ", subParts);
            if (valueStr == InventoryValueDisplay.Unknown)
                sub += " · Unappraised";

            _subtitleText.fontSize = 11f * detailsFontScale;
            _subtitleText.text = sub;

            Sprite use = itemIcon != null ? itemIcon : placeholderIcon;
            _iconImage.sprite = use;
            _iconImage.color = itemIcon != null ? Color.white : new Color(0.45f, 0.45f, 0.48f, 0.9f);

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClicked?.Invoke());
        }

        public void SetSelected(bool selected, Color selectedTint, Color normalTint)
        {
            if (_backing != null)
                _backing.color = selected ? selectedTint : normalTint;
        }

        public void SetInteractable(bool interactable) => _button.interactable = interactable;

        static string FormatUserMarksPrefix(ItemInstance inst)
        {
            if (inst == null)
                return string.Empty;

            ItemUserMark m = inst.UserMarks;
            if (m == ItemUserMark.None)
                return string.Empty;

            var parts = new System.Collections.Generic.List<string>();
            if ((m & ItemUserMark.Favorite) != 0)
                parts.Add("<color=#e8c56c>[F]</color>");
            if ((m & ItemUserMark.Protected) != 0)
                parts.Add("<color=#7ec8ff>[P]</color>");
            if ((m & ItemUserMark.Junk) != 0)
                parts.Add("<color=#9aa7b0>[J]</color>");

            return string.Join(string.Empty, parts) + " ";
        }
    }
}
