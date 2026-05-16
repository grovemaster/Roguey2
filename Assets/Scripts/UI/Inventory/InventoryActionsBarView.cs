using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    public sealed class InventoryActionsBarView : MonoBehaviour
    {
        Button _equipBtn;
        Button _useBtn;
        Button _dropBtn;
        Button _giveBtn;

        Action _onEquip;
        Action _onUse;
        Action _onDrop;
        Action _onGive;

        public static InventoryActionsBarView Create(
            Transform parent,
            Action onEquip,
            Action onUse,
            Action onDrop,
            Action onGive)
        {
            Transform existing = parent.Find("ActionsBar");
            InventoryActionsBarView view;
            if (existing != null)
            {
                view = existing.GetComponent<InventoryActionsBarView>() ??
                       existing.gameObject.AddComponent<InventoryActionsBarView>();
            }
            else
            {
                var root = new GameObject("ActionsBar", typeof(RectTransform));
                root.transform.SetParent(parent, false);

                var le = root.AddComponent<LayoutElement>();
                le.minHeight = 40f;
                le.preferredHeight = 44f;
                le.flexibleWidth = 1f;

                var h = root.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 10;
                h.padding = new RectOffset(8, 8, 6, 6);
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = true;
                h.childForceExpandWidth = false;

                view = root.AddComponent<InventoryActionsBarView>();
                view._equipBtn = view.AddAction(root.transform, "Equip");
                view._useBtn = view.AddAction(root.transform, "Use");
                view._dropBtn = view.AddAction(root.transform, "Drop");
                view._giveBtn = view.AddAction(root.transform, "Give");
            }

            view._onEquip = onEquip;
            view._onUse = onUse;
            view._onDrop = onDrop;
            view._onGive = onGive;
            view.WireClicks();
            return view;
        }

        void WireClicks()
        {
            _equipBtn.onClick.RemoveAllListeners();
            _useBtn.onClick.RemoveAllListeners();
            _dropBtn.onClick.RemoveAllListeners();
            _giveBtn.onClick.RemoveAllListeners();
            _equipBtn.onClick.AddListener(() => _onEquip?.Invoke());
            _useBtn.onClick.AddListener(() => _onUse?.Invoke());
            _dropBtn.onClick.AddListener(() => _onDrop?.Invoke());
            _giveBtn.onClick.AddListener(() => _onGive?.Invoke());
        }

        Button AddAction(Transform parent, string label)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.19f, 0.21f, 0.98f);

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 72f;
            le.preferredWidth = 88f;
            le.minHeight = 32f;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var rt = labelGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 13f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.92f, 0.95f);

            return go.GetComponent<Button>();
        }

        public void SetState(bool canEquip, bool canUnequip, bool canUse, bool canDrop, bool canGive, float fontScale)
        {
            SetButton(_equipBtn, canEquip ? "Equip" : canUnequip ? "Unequip" : "Equip", canEquip || canUnequip, fontScale);
            SetButton(_useBtn, "Use", canUse, fontScale);
            SetButton(_dropBtn, "Drop", canDrop, fontScale);
            SetButton(_giveBtn, "Give", canGive, fontScale);
        }

        static void SetButton(Button btn, string label, bool enabled, float fontScale)
        {
            if (btn == null)
                return;

            btn.interactable = enabled;
            if (btn.transform.Find("Label")?.GetComponent<TextMeshProUGUI>() is { } tmp)
            {
                tmp.text = label;
                tmp.fontSize = 13f * fontScale;
                tmp.color = enabled
                    ? new Color(0.9f, 0.92f, 0.95f)
                    : new Color(0.45f, 0.48f, 0.52f);
            }

            if (btn.TryGetComponent(out Image img))
                img.color = enabled
                    ? new Color(0.18f, 0.19f, 0.21f, 0.98f)
                    : new Color(0.12f, 0.125f, 0.13f, 0.85f);
        }
    }
}
