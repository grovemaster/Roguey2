using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public sealed class RacialAbilitiesPartyStripView : MonoBehaviour
    {
        const float ChipWidth = 96f;
        const float ChipHeight = 108f;
        const float PortraitSize = 56f;

        Transform _chipsRoot;
        Action<int> _onMemberSelected;
        HorizontalLayoutGroup _rowLayout;

        public static RacialAbilitiesPartyStripView Create(Transform parent, Action<int> onMemberSelected)
        {
            Transform existing = parent.Find("PartyStrip");
            RacialAbilitiesPartyStripView view;
            if (existing != null)
            {
                view = existing.GetComponent<RacialAbilitiesPartyStripView>() ??
                       existing.gameObject.AddComponent<RacialAbilitiesPartyStripView>();
                view.WireExisting();
            }
            else
            {
                var root = new GameObject("PartyStrip", typeof(RectTransform));
                root.transform.SetParent(parent, false);

                var le = root.AddComponent<LayoutElement>();
                le.minHeight = ChipHeight + 4f;
                le.preferredHeight = ChipHeight + 4f;
                le.flexibleWidth = 1f;

                view = root.AddComponent<RacialAbilitiesPartyStripView>();
                view._chipsRoot = root.transform;
                view._rowLayout = root.AddComponent<HorizontalLayoutGroup>();
                ConfigureRowLayout(view._rowLayout);
            }

            view._onMemberSelected = onMemberSelected;
            return view;
        }

        void WireExisting()
        {
            _chipsRoot = transform;
            _rowLayout = GetComponent<HorizontalLayoutGroup>();
            if (_rowLayout == null)
            {
                _rowLayout = gameObject.AddComponent<HorizontalLayoutGroup>();
                ConfigureRowLayout(_rowLayout);
            }

            Transform legacyMembers = transform.Find("Members");
            if (legacyMembers != null)
            {
                for (int i = legacyMembers.childCount - 1; i >= 0; i--)
                    legacyMembers.GetChild(i).SetParent(_chipsRoot, false);

                Destroy(legacyMembers.gameObject);
            }
        }

        static void ConfigureRowLayout(HorizontalLayoutGroup row)
        {
            row.spacing = 10f;
            row.padding = new RectOffset(0, 0, 0, 0);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
        }

        public void Rebuild(IReadOnlyList<BaseActor> party, int focusedIndex)
        {
            if (_chipsRoot == null)
                _chipsRoot = transform;

            for (int i = _chipsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _chipsRoot.GetChild(i);
                if (child.name.StartsWith("Member_", StringComparison.Ordinal))
                    Destroy(child.gameObject);
            }

            for (int i = 0; i < party.Count; i++)
                CreateMemberChip(i, party[i], i == focusedIndex);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_chipsRoot as RectTransform);
        }

        void CreateMemberChip(int index, BaseActor actor, bool focused)
        {
            var chip = new GameObject($"Member_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
            chip.transform.SetParent(_chipsRoot, false);

            var chipLe = chip.AddComponent<LayoutElement>();
            chipLe.minWidth = ChipWidth;
            chipLe.preferredWidth = ChipWidth;
            chipLe.minHeight = ChipHeight;
            chipLe.preferredHeight = ChipHeight;

            Image frame = chip.GetComponent<Image>();
            frame.sprite = RacialUiTheme.PlaceholderSprite;
            frame.color = focused ? RacialUiTheme.FocusBorder : RacialUiTheme.InactiveBorder;

            var chipLayout = chip.AddComponent<VerticalLayoutGroup>();
            chipLayout.padding = new RectOffset(6, 6, 6, 4);
            chipLayout.spacing = 4f;
            chipLayout.childAlignment = TextAnchor.UpperCenter;
            chipLayout.childControlWidth = true;
            chipLayout.childControlHeight = true;
            chipLayout.childForceExpandWidth = true;
            chipLayout.childForceExpandHeight = false;

            var portraitFrame = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            portraitFrame.transform.SetParent(chip.transform, false);
            LayoutElement portraitLe = portraitFrame.GetComponent<LayoutElement>();
            portraitLe.minWidth = PortraitSize;
            portraitLe.preferredWidth = PortraitSize;
            portraitLe.minHeight = PortraitSize;
            portraitLe.preferredHeight = PortraitSize;
            Image portraitFrameImage = portraitFrame.GetComponent<Image>();
            portraitFrameImage.sprite = RacialUiTheme.PlaceholderSprite;
            portraitFrameImage.color = new Color(0.08f, 0.09f, 0.12f, 1f);

            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(portraitFrame.transform, false);
            RacialUiTheme.Stretch(portraitGo.GetComponent<RectTransform>());
            Image portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            ApplyPortrait(portraitImage, actor);

            TextMeshProUGUI keyLabel = CreateKeyLabel(portraitFrame.transform);
            keyLabel.text = $"F{index + 1}";

            TextMeshProUGUI nameLabel = RacialUiTheme.CreateText(
                chip.transform,
                "Name",
                actor != null ? actor.DisplayName : "?",
                RacialUiTheme.PartyNameFontSize,
                TextAlignmentOptions.Center);
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            var nameLe = nameLabel.gameObject.AddComponent<LayoutElement>();
            nameLe.minHeight = 22f;
            nameLe.preferredHeight = 22f;

            int captured = index;
            chip.GetComponent<Button>().onClick.AddListener(() => _onMemberSelected?.Invoke(captured));
        }

        static void ApplyPortrait(Image portraitImage, BaseActor actor)
        {
            PortraitDefinition def = PortraitResolver.ResolveSpeaker(actor, null);
            if (def != null && def.portrait != null)
            {
                portraitImage.sprite = def.portrait;
                portraitImage.color = Color.white;
                return;
            }

            portraitImage.sprite = RacialUiTheme.PlaceholderSprite;
            portraitImage.color = new Color(0.35f, 0.38f, 0.42f, 0.8f);
        }

        static TextMeshProUGUI CreateKeyLabel(Transform parent)
        {
            var badgeGo = new GameObject("KeyBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(parent, false);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0f, 1f);
            badgeRt.anchorMax = new Vector2(0f, 1f);
            badgeRt.pivot = new Vector2(0f, 1f);
            badgeRt.anchoredPosition = new Vector2(-4f, 4f);
            badgeRt.sizeDelta = new Vector2(32f, 20f);
            badgeGo.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            badgeGo.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.92f);

            TextMeshProUGUI text = RacialUiTheme.CreateText(
                badgeGo.transform,
                "Key",
                string.Empty,
                RacialUiTheme.PartyKeyFontSize,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            RacialUiTheme.Stretch(text.rectTransform);
            text.color = Color.white;
            return text;
        }
    }
}
