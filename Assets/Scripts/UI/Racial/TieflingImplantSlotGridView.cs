using System;
using System.Collections.Generic;
using JRogue.Racial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public sealed class TieflingImplantSlotGridView : MonoBehaviour
    {
        readonly Dictionary<ImplantSlot, SlotCellChrome> _cells = new Dictionary<ImplantSlot, SlotCellChrome>();

        sealed class SlotCellChrome
        {
            public Image Frame;
            public Image Icon;
            public TextMeshProUGUI Label;
            public Button Button;
        }

        Action<ImplantSlot> _onSelect;

        public static TieflingImplantSlotGridView Create(Transform parent, Action<ImplantSlot> onSelect)
        {
            Transform existing = parent.Find("ImplantGrid");
            TieflingImplantSlotGridView view;
            if (existing != null)
            {
                view = existing.GetComponent<TieflingImplantSlotGridView>() ??
                       existing.gameObject.AddComponent<TieflingImplantSlotGridView>();
            }
            else
            {
                var root = new GameObject("ImplantGrid", typeof(RectTransform));
                root.transform.SetParent(parent, false);
                var le = root.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.flexibleHeight = 1f;

                view = root.AddComponent<TieflingImplantSlotGridView>();
                view.BuildGrid(root.transform, onSelect);
            }

            view._onSelect = onSelect;
            return view;
        }

        void BuildGrid(Transform root, Action<ImplantSlot> onSelect)
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
                root, "Heading", "CYBORG IMPLANTS", RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            heading.color = new Color(0.88f, 0.55f, 0.38f, 1f);
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            AddRow(root, null, ImplantSlot.Head, null);
            AddRow(root, ImplantSlot.LeftArm, ImplantSlot.Torso, ImplantSlot.RightArm);
            AddRow(root, ImplantSlot.Heart, null, null);
            AddRow(root, ImplantSlot.LeftLeg, null, ImplantSlot.RightLeg);
        }

        void AddRow(Transform parent, ImplantSlot? left, ImplantSlot? center, ImplantSlot? right)
        {
            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = 92f;
            rowLe.preferredHeight = 92f;

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = false;
            h.childForceExpandWidth = false;

            AddCell(row.transform, left);
            AddCell(row.transform, center);
            AddCell(row.transform, right);
        }

        void AddCell(Transform row, ImplantSlot? slot)
        {
            if (!slot.HasValue)
            {
                var spacer = new GameObject("Spacer", typeof(RectTransform));
                spacer.transform.SetParent(row, false);
                var le = spacer.AddComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = 88f;
                le.minHeight = le.preferredHeight = 88f;
                return;
            }

            ImplantSlot value = slot.Value;
            var go = new GameObject(value.ToString(), typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(row, false);

            var cellLe = go.AddComponent<LayoutElement>();
            cellLe.minWidth = cellLe.preferredWidth = 88f;
            cellLe.minHeight = cellLe.preferredHeight = 88f;

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
                go.transform, "Label", ImplantSlotLabels.GetLabel(value), 11f,
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
            ImplantSlot captured = value;
            btn.onClick.AddListener(() => _onSelect?.Invoke(captured));

            _cells[value] = new SlotCellChrome
            {
                Frame = frame,
                Icon = icon,
                Label = label,
                Button = btn
            };
        }

        public void Rebuild(IReadOnlyList<TieflingImplantSlotCellModel> cells, ImplantSlot selectedSlot)
        {
            foreach (TieflingImplantSlotCellModel model in cells)
            {
                if (!_cells.TryGetValue(model.Slot, out SlotCellChrome cell))
                    continue;

                bool selected = model.Slot == selectedSlot;
                if (model.Occupied)
                {
                    cell.Frame.color = selected ? RacialUiTheme.FocusBorder : RacialUiTheme.InactiveBorder;
                    cell.Icon.sprite = RacialUiTheme.ImprintEmblemSprite;
                    cell.Icon.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.92f);
                    cell.Label.text = model.Subtitle;
                    cell.Label.color = RacialUiTheme.BodyText;
                }
                else
                {
                    cell.Frame.color = selected
                        ? RacialUiTheme.FocusBorder
                        : new Color(0.14f, 0.16f, 0.2f, 0.45f);
                    cell.Icon.sprite = RacialUiTheme.PlaceholderSprite;
                    cell.Icon.color = new Color(1f, 1f, 1f, 0.12f);
                    cell.Label.text = model.Label;
                    cell.Label.color = RacialUiTheme.MutedText;
                }
            }
        }
    }
}
