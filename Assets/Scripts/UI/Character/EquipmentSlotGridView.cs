using System;
using System.Collections.Generic;
using JRogue.Item;
using JRogue.UI.Racial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Character
{
    public sealed class EquipmentSlotGridView : MonoBehaviour
    {
        readonly Dictionary<EquipmentSlot, SlotCellChrome> _cells = new Dictionary<EquipmentSlot, SlotCellChrome>();

        sealed class SlotCellChrome
        {
            public Image Frame;
            public Image Icon;
            public TextMeshProUGUI Label;
            public Button Button;
        }

        public static EquipmentSlotGridView Create(Transform parent, Action<EquipmentSlot> onSelect)
        {
            Transform existing = parent.Find("EquipmentGrid");
            EquipmentSlotGridView view;
            if (existing != null)
            {
                view = existing.GetComponent<EquipmentSlotGridView>() ??
                       existing.gameObject.AddComponent<EquipmentSlotGridView>();
            }
            else
            {
                var root = new GameObject("EquipmentGrid", typeof(RectTransform));
                root.transform.SetParent(parent, false);
                var le = root.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.flexibleHeight = 1f;

                view = root.AddComponent<EquipmentSlotGridView>();
                view.BuildGrid(root.transform, onSelect);
            }

            view._onSelect = onSelect;
            return view;
        }

        Action<EquipmentSlot> _onSelect;

        void BuildGrid(Transform root, Action<EquipmentSlot> onSelect)
        {
            _onSelect = onSelect;

            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI heading = RacialUiTheme.CreateText(
                root, "Heading", "EQUIPMENT", RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            heading.color = RacialUiTheme.SectionLabel;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            AddRow(root, null, EquipmentSlot.Head, EquipmentSlot.Accessory_Head);
            AddRow(root, null, EquipmentSlot.Torso, null);
            AddRow(root, EquipmentSlot.MainHand, null, EquipmentSlot.OffHand);
            AddRow(root, EquipmentSlot.Accessory_MainHand, EquipmentSlot.Legs, EquipmentSlot.Accessory_OffHand);
            AddRow(root, null, EquipmentSlot.Feet, null);
        }

        void AddRow(Transform parent, EquipmentSlot? left, EquipmentSlot? center, EquipmentSlot? right)
        {
            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = 88f;
            rowLe.preferredHeight = 88f;

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = false;
            h.childForceExpandWidth = false;

            AddCell(row.transform, left);
            AddCell(row.transform, center);
            AddCell(row.transform, right);
        }

        void AddCell(Transform row, EquipmentSlot? slot)
        {
            if (!slot.HasValue)
            {
                var spacer = new GameObject("Spacer", typeof(RectTransform));
                spacer.transform.SetParent(row, false);
                var le = spacer.AddComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = 88f;
                le.minHeight = le.preferredHeight = 84f;
                return;
            }

            EquipmentSlot value = slot.Value;
            var go = new GameObject(value.ToString(), typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(row, false);

            var cellLe = go.AddComponent<LayoutElement>();
            cellLe.minWidth = cellLe.preferredWidth = 88f;
            cellLe.minHeight = cellLe.preferredHeight = 84f;

            Image frame = go.GetComponent<Image>();
            frame.sprite = RacialUiTheme.PlaceholderSprite;
            frame.color = RacialUiTheme.InactiveBorder;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.55f);
            iconRt.anchorMax = new Vector2(0.5f, 0.55f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(52f, 52f);
            Image icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI label = RacialUiTheme.CreateText(
                go.transform, "Label", EquipmentSlotLabels.GetLabel(value), 11f,
                TextAlignmentOptions.Center, FontStyles.Normal);
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.sizeDelta = new Vector2(-4f, 22f);
            labelRt.anchoredPosition = Vector2.zero;
            label.color = RacialUiTheme.MutedText;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            Button btn = go.GetComponent<Button>();
            EquipmentSlot captured = value;
            btn.onClick.AddListener(() => _onSelect?.Invoke(captured));

            _cells[value] = new SlotCellChrome
            {
                Frame = frame,
                Icon = icon,
                Label = label,
                Button = btn
            };
        }

        public void Rebuild(IReadOnlyList<EquipmentSlotCellModel> slots, EquipmentSlot selectedSlot)
        {
            foreach (EquipmentSlotCellModel model in slots)
            {
                if (!_cells.TryGetValue(model.Slot, out SlotCellChrome cell))
                    continue;

                bool selected = model.Slot == selectedSlot;
                cell.Frame.color = selected ? RacialUiTheme.FocusBorder : RacialUiTheme.InactiveBorder;

                if (model.Occupied)
                {
                    ItemData def = model.Instance.Definition;
                    cell.Icon.sprite = def.icon != null ? def.icon : RacialUiTheme.PlaceholderSprite;
                    cell.Icon.color = Color.white;
                    string qty = model.Instance.Quantity > 1 ? $" ×{model.Instance.Quantity}" : string.Empty;
                    cell.Label.text = def.itemName + qty;
                    cell.Label.color = RacialUiTheme.BodyText;
                }
                else
                {
                    cell.Icon.sprite = RacialUiTheme.PlaceholderSprite;
                    cell.Icon.color = new Color(1f, 1f, 1f, 0.12f);
                    cell.Label.text = model.Label;
                    cell.Label.color = RacialUiTheme.MutedText;
                }
            }
        }
    }
}
