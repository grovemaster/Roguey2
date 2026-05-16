using System;
using System.Collections.Generic;
using JRogue.Actors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    public sealed class InventoryPartyStripView : MonoBehaviour
    {
        readonly List<Button> _memberButtons = new List<Button>();
        Action<int> _onMemberSelected;
        Action _onModeToggle;
        TextMeshProUGUI _modeLabel;
        Button _modeButton;

        public static InventoryPartyStripView Create(Transform parent, Action<int> onMember, Action onModeToggle)
        {
            Transform existing = parent.Find("PartyStrip");
            InventoryPartyStripView view;
            if (existing != null)
            {
                view = existing.GetComponent<InventoryPartyStripView>() ??
                       existing.gameObject.AddComponent<InventoryPartyStripView>();
            }
            else
            {
                var root = new GameObject("PartyStrip", typeof(RectTransform));
                root.transform.SetParent(parent, false);

                var le = root.AddComponent<LayoutElement>();
                le.minHeight = 36f;
                le.preferredHeight = 40f;
                le.flexibleWidth = 1f;

                var h = root.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 8;
                h.padding = new RectOffset(4, 8, 4, 4);
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = true;
                h.childForceExpandWidth = false;

                var members = new GameObject("Members", typeof(RectTransform));
                members.transform.SetParent(root.transform, false);
                var membersLe = members.AddComponent<LayoutElement>();
                membersLe.flexibleWidth = 1f;
                var membersH = members.AddComponent<HorizontalLayoutGroup>();
                membersH.spacing = 6;
                membersH.childAlignment = TextAnchor.MiddleLeft;
                membersH.childControlWidth = true;
                membersH.childForceExpandWidth = false;

                var modeGo = new GameObject("ModeToggle", typeof(RectTransform), typeof(Image), typeof(Button));
                modeGo.transform.SetParent(root.transform, false);
                var modeLe = modeGo.AddComponent<LayoutElement>();
                modeLe.minWidth = 120f;
                modeLe.preferredWidth = 140f;

                var modeImg = modeGo.GetComponent<Image>();
                modeImg.color = new Color(0.14f, 0.15f, 0.165f, 0.95f);

                var modeLabelGo = new GameObject("Label", typeof(RectTransform));
                modeLabelGo.transform.SetParent(modeGo.transform, false);
                var modeRt = modeLabelGo.GetComponent<RectTransform>();
                SetStretch(modeRt);
                var modeTmp = modeLabelGo.AddComponent<TextMeshProUGUI>();
                modeTmp.alignment = TextAlignmentOptions.Center;
                modeTmp.fontSize = 12f;
                modeTmp.color = new Color(0.85f, 0.88f, 0.92f);

                view = root.AddComponent<InventoryPartyStripView>();
                view._membersRoot = members.transform;
                view._modeLabel = modeTmp;
                view._modeButton = modeGo.GetComponent<Button>();
            }

            view._onMemberSelected = onMember;
            view._onModeToggle = onModeToggle;
            view._modeButton.onClick.RemoveAllListeners();
            view._modeButton.onClick.AddListener(() => view._onModeToggle?.Invoke());
            return view;
        }

        Transform _membersRoot;

        public void Rebuild(
            IReadOnlyList<BaseActor> party,
            int selectedIndex,
            InventoryUI.BrowseMode mode,
            float fontScale)
        {
            if (_membersRoot == null)
                _membersRoot = transform.Find("Members") ?? transform;

            for (int i = _membersRoot.childCount - 1; i >= 0; i--)
                Destroy(_membersRoot.GetChild(i).gameObject);

            _memberButtons.Clear();

            for (int i = 0; i < party.Count; i++)
            {
                BaseActor actor = party[i];
                string name = actor != null ? actor.DisplayName : "?";
                bool selected = mode == InventoryUI.BrowseMode.FocusedMember && i == selectedIndex;
                AddMemberButton(i, selected ? $"● {name}" : $"○ {name}", selected, fontScale);
            }

            if (_modeLabel != null)
            {
                _modeLabel.fontSize = 12f * fontScale;
                _modeLabel.text = mode == InventoryUI.BrowseMode.FocusedMember
                    ? "Mode: Member ▾"
                    : "Mode: Party ▾";
            }
        }

        void AddMemberButton(int index, string label, bool selected, float fontScale)
        {
            var go = new GameObject($"Member_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_membersRoot, false);

            var img = go.GetComponent<Image>();
            img.color = selected
                ? new Color(0.22f, 0.285f, 0.34f, 0.98f)
                : new Color(0.14f, 0.15f, 0.165f, 0.95f);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 28f;
            le.preferredHeight = 30f;
            le.minWidth = 72f;
            le.preferredWidth = Mathf.Max(72f, label.Length * 7f + 20f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            SetStretch(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 12f * fontScale;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(10, 0, 8, 0);
            tmp.color = new Color(0.88f, 0.91f, 0.94f);

            var btn = go.GetComponent<Button>();
            int captured = index;
            btn.onClick.AddListener(() => _onMemberSelected?.Invoke(captured));
            _memberButtons.Add(btn);
        }

        static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
