using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    /// <summary>Static column labels above the item scroll list.</summary>
    public sealed class InventoryListColumnHeaderView : MonoBehaviour
    {
        public static InventoryListColumnHeaderView Create(Transform parent, float fontScale)
        {
            Transform existing = parent.Find("ColumnHeaders");
            if (existing != null)
            {
                var v = existing.GetComponent<InventoryListColumnHeaderView>();
                v.ApplyScale(fontScale);
                return v;
            }

            var root = new GameObject("ColumnHeaders", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            root.transform.SetAsFirstSibling();

            var bg = root.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.105f, 0.12f, 0.95f);

            var le = root.AddComponent<LayoutElement>();
            le.minHeight = 26f;
            le.preferredHeight = 28f;
            le.flexibleWidth = 1f;

            var h = root.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(6, 8, 4, 4);
            h.spacing = 6;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            var view = root.AddComponent<InventoryListColumnHeaderView>();
            view._letterSpacer = AddSpacer(root.transform, 28f);
            view._iconSpacer = AddSpacer(root.transform, 44f);
            view._nameLabel = AddLabel(root.transform, "Name", TextAlignmentOptions.Left, flex: 1f);
            view._qtyLabel = AddLabel(root.transform, "Qty", TextAlignmentOptions.Right, 44f);
            view._wtLabel = AddLabel(root.transform, "Wt", TextAlignmentOptions.Right, 52f);
            view._valueLabel = AddLabel(root.transform, "Value", TextAlignmentOptions.Right, 56f);
            view.ApplyScale(fontScale);
            return view;
        }

        TextMeshProUGUI _nameLabel;
        TextMeshProUGUI _qtyLabel;
        TextMeshProUGUI _wtLabel;
        TextMeshProUGUI _valueLabel;
        GameObject _letterSpacer;
        GameObject _iconSpacer;

        void ApplyScale(float scale)
        {
            float fs = 11f * scale;
            SetSize(_nameLabel, fs);
            SetSize(_qtyLabel, fs);
            SetSize(_wtLabel, fs);
            SetSize(_valueLabel, fs);
        }

        static void SetSize(TextMeshProUGUI tmp, float size)
        {
            if (tmp != null)
                tmp.fontSize = size;
        }

        static GameObject AddSpacer(Transform parent, float width)
        {
            var go = new GameObject("Spacer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;
            return go;
        }

        static TextMeshProUGUI AddLabel(
            Transform parent,
            string text,
            TextAlignmentOptions align,
            float width = 0f,
            float flex = 0f)
        {
            var go = new GameObject(text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 11f;
            tmp.color = new Color(0.62f, 0.67f, 0.72f);
            tmp.alignment = align;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            var le = go.AddComponent<LayoutElement>();
            if (flex > 0f)
            {
                le.flexibleWidth = flex;
                le.minWidth = 80f;
            }
            else
            {
                le.preferredWidth = width;
                le.minWidth = width;
            }

            return tmp;
        }
    }
}
