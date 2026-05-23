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
        TextMeshProUGUI _manaBadge;
        TextMeshProUGUI _goldBadge;
        Transform _resourcesRoot;

        public static InventoryPartyStripView Create(Transform parent, Action<int> onMember, Action onModeToggle)
        {
            Transform existing = parent.Find("PartyStrip");
            InventoryPartyStripView view;
            if (existing != null)
            {
                view = existing.GetComponent<InventoryPartyStripView>() ??
                       existing.gameObject.AddComponent<InventoryPartyStripView>();
                view.WireExisting();
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
                membersLe.minWidth = 80f;
                var membersH = members.AddComponent<HorizontalLayoutGroup>();
                membersH.spacing = 6;
                membersH.childAlignment = TextAnchor.MiddleLeft;
                membersH.childControlWidth = true;
                membersH.childForceExpandWidth = false;

                var resources = new GameObject("Resources", typeof(RectTransform));
                resources.transform.SetParent(root.transform, false);
                var resourcesLe = resources.AddComponent<LayoutElement>();
                resourcesLe.flexibleWidth = 0f;
                resourcesLe.minWidth = 80f;
                var resourcesH = resources.AddComponent<HorizontalLayoutGroup>();
                resourcesH.spacing = 6;
                resourcesH.childAlignment = TextAnchor.MiddleLeft;
                resourcesH.childControlWidth = true;
                resourcesH.childForceExpandWidth = false;

                view = root.AddComponent<InventoryPartyStripView>();
                view._membersRoot = members.transform;
                view._resourcesRoot = resources.transform;
                view._manaBadge = view.CreateResourceBadge(resources.transform, "ManaBadge");
                view._goldBadge = view.CreateResourceBadge(resources.transform, "GoldBadge");

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

                view._modeLabel = modeTmp;
                view._modeButton = modeGo.GetComponent<Button>();
            }

            view._onMemberSelected = onMember;
            view._onModeToggle = onModeToggle;
            if (view._modeButton != null)
            {
                view._modeButton.onClick.RemoveAllListeners();
                view._modeButton.onClick.AddListener(() => view._onModeToggle?.Invoke());
            }

            return view;
        }

        Transform _membersRoot;

        void WireExisting()
        {
            _membersRoot = transform.Find("Members") ?? transform;
            _resourcesRoot = transform.Find("Resources");
            if (_resourcesRoot == null)
            {
                var resources = new GameObject("Resources", typeof(RectTransform));
                resources.transform.SetParent(transform, false);
                resources.transform.SetSiblingIndex(_membersRoot.GetSiblingIndex() + 1);
                var resourcesH = resources.AddComponent<HorizontalLayoutGroup>();
                resourcesH.spacing = 6;
                resourcesH.childAlignment = TextAnchor.MiddleLeft;
                resourcesH.childControlWidth = true;
                resourcesH.childForceExpandWidth = false;
                _resourcesRoot = resources.transform;
            }

            _manaBadge = FindOrCreateBadge(_resourcesRoot, "ManaBadge");
            _goldBadge = FindOrCreateBadge(_resourcesRoot, "GoldBadge");
            _modeButton = transform.Find("ModeToggle")?.GetComponent<Button>();
            _modeLabel = transform.Find("ModeToggle/Label")?.GetComponent<TextMeshProUGUI>();
        }

        TextMeshProUGUI FindOrCreateBadge(Transform root, string name)
        {
            Transform t = root.Find(name);
            if (t != null)
                return t.GetComponent<TextMeshProUGUI>();
            return CreateResourceBadge(root, name);
        }

        TextMeshProUGUI CreateResourceBadge(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.14f, 0.15f, 0.165f, 0.95f);

            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 28f;
            le.preferredHeight = 30f;
            le.minWidth = 56f;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            SetStretch(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 12f;
            tmp.richText = true;
            tmp.color = new Color(0.88f, 0.91f, 0.94f);
            return tmp;
        }

        public void Rebuild(
            IReadOnlyList<BaseActor> party,
            int selectedIndex,
            InventoryUI.BrowseMode mode,
            int manaTotal,
            int goldTotal,
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

            RefreshResourceBadge(
                _manaBadge,
                manaTotal > 0,
                $"<color=#7a8fa8>Mana Stones</color> <b>{manaTotal}</b>",
                fontScale);
            RefreshResourceBadge(
                _goldBadge,
                goldTotal > 0,
                $"<color=#7a8fa8>Gold</color> <b>{goldTotal}</b>",
                fontScale);

            if (_resourcesRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_resourcesRoot as RectTransform);
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }

            if (_modeLabel != null)
            {
                _modeLabel.fontSize = 12f * fontScale;
                _modeLabel.text = mode == InventoryUI.BrowseMode.FocusedMember
                    ? "Mode: Member ▾"
                    : "Mode: Party ▾";
            }
        }

        static void RefreshResourceBadge(TextMeshProUGUI label, bool visible, string text, float fontScale)
        {
            if (label == null)
                return;

            Transform badgeRoot = label.transform.parent;
            if (badgeRoot == null)
                return;

            badgeRoot.gameObject.SetActive(visible);
            if (!visible)
                return;

            label.fontSize = 12f * fontScale;
            label.richText = true;
            label.text = text;

            var le = badgeRoot.GetComponent<LayoutElement>() ?? badgeRoot.gameObject.AddComponent<LayoutElement>();
            le.minWidth = Mathf.Max(88f, text.Length * 7f + 24f);
            le.preferredWidth = le.minWidth;
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
