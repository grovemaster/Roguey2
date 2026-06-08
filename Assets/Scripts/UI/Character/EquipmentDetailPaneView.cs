using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.UI.Inventory;
using JRogue.UI.Racial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Character
{
    public static class CharacterEquipmentDetailBuilder
    {
        public static void PopulateDetailPane(
            Image heroIcon,
            TextMeshProUGUI heroText,
            TextMeshProUGUI bodyText,
            CharacterEquipmentSheetModel sheet,
            CharacterEquipmentSelection selection)
        {
            if (heroIcon == null || heroText == null || bodyText == null)
                return;

            if (sheet == null || selection.Kind == CharacterEquipmentSelectionKind.None)
            {
                heroIcon.sprite = RacialUiTheme.PlaceholderSprite;
                heroIcon.color = new Color(1f, 1f, 1f, 0.25f);
                heroText.text = "DETAILS";
                bodyText.text = "Select a slot above to view details.";
                return;
            }

            if (selection.Kind == CharacterEquipmentSelectionKind.Equipment)
                PopulateEquipmentDetail(heroIcon, heroText, bodyText, sheet, selection.EquipmentSlot);
            else
                PopulateEssenceDetail(heroIcon, heroText, bodyText, sheet, selection.EssenceSlotIndex);
        }

        static void PopulateEquipmentDetail(
            Image heroIcon,
            TextMeshProUGUI heroText,
            TextMeshProUGUI bodyText,
            CharacterEquipmentSheetModel sheet,
            EquipmentSlot slot)
        {
            EquipmentSlotCellModel cell = sheet.EquipmentSlots.Find(c => c.Slot == slot);
            string slotLabel = cell?.Label ?? EquipmentSlotLabels.GetLabel(slot);

            if (cell == null || !cell.Occupied)
            {
                heroIcon.sprite = RacialUiTheme.PlaceholderSprite;
                heroIcon.color = new Color(1f, 1f, 1f, 0.2f);
                heroText.text = $"<size=22><b>{slotLabel}</b></size>";
                bodyText.text =
                    $"Nothing equipped in {slotLabel}.\n\nUse Inventory to equip items.";
                return;
            }

            ItemData item = cell.Instance.Definition;
            var row = new InventoryViewModel.Row(
                ' ',
                cell.Instance,
                sheet.Actor,
                sheet.Actor != null ? sheet.Actor.DisplayName : string.Empty,
                true,
                slot,
                -1,
                cell.Instance.TotalWeight);

            heroIcon.sprite = item.icon != null ? item.icon : RacialUiTheme.PlaceholderSprite;
            heroIcon.color = Color.white;
            heroText.text =
                InventoryDetailFormatter.FormatHeroTitle(item, cell.Instance) + "\n" +
                $"<color=#8a97a3>{InventoryDetailFormatter.FormatHeroSubtitle(item, row)}</color>\n" +
                $"<color=#8a97a3>Equipped on {slotLabel}</color>";

            bodyText.text = InventoryDetailFormatter.FormatInspectBody(item, row);
        }

        static void PopulateEssenceDetail(
            Image heroIcon,
            TextMeshProUGUI heroText,
            TextMeshProUGUI bodyText,
            CharacterEquipmentSheetModel sheet,
            int slotIndex)
        {
            if (!sheet.CanGainEssences)
            {
                heroIcon.sprite = RacialUiTheme.PlaceholderSprite;
                heroIcon.color = new Color(1f, 1f, 1f, 0.2f);
                heroText.text = "<size=22><b>Essences</b></size>";
                bodyText.text = "This class cannot equip essences.";
                return;
            }

            EssenceSlotCellModel cell = sheet.EssenceSlots.Find(c => c.SlotIndex == slotIndex);
            if (cell == null || !cell.Occupied)
            {
                heroIcon.sprite = RacialUiTheme.ImprintEmblemSprite;
                heroIcon.color = new Color(1f, 1f, 1f, 0.35f);
                heroText.text = $"<size=22><b>Essence slot {slotIndex + 1}</b></size>";
                bodyText.text = EssenceDetailFormatter.FormatEmptySlot(slotIndex);
                return;
            }

            EssenceData essence = cell.Essence;
            heroIcon.sprite = essence.mapIcon != null ? essence.mapIcon : RacialUiTheme.ImprintEmblemSprite;
            heroIcon.color = Color.white;
            heroText.text =
                $"<size=22><b>{EssenceDetailFormatter.FormatTitle(essence)}</b></size>\n" +
                $"<color=#8a97a3>{EssenceDetailFormatter.FormatSubtitle(essence, slotIndex)}</color>";
            bodyText.text = EssenceDetailFormatter.FormatBody(essence);
        }
    }

    public sealed class EquipmentDetailPaneView : MonoBehaviour
    {
        Image _heroIcon;
        TextMeshProUGUI _heroText;
        TextMeshProUGUI _bodyText;

        public static EquipmentDetailPaneView Create(Transform parent)
        {
            Transform existing = parent.Find("DetailPane");
            if (existing != null)
            {
                return existing.GetComponent<EquipmentDetailPaneView>() ??
                       existing.gameObject.AddComponent<EquipmentDetailPaneView>();
            }

            var root = new GameObject("DetailPane", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Image bg = root.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = new Color(0.12f, 0.125f, 0.135f, 0.92f);

            var le = root.AddComponent<LayoutElement>();
            le.minHeight = 220f;
            le.preferredHeight = 260f;
            le.flexibleHeight = 0f;

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI section = RacialUiTheme.CreateText(
                root.transform, "Section", "DETAILS", RacialUiTheme.SectionFontSize,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            section.color = RacialUiTheme.SectionLabel;
            section.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            var hero = new GameObject("Hero", typeof(RectTransform));
            hero.transform.SetParent(root.transform, false);
            var heroLe = hero.AddComponent<LayoutElement>();
            heroLe.minHeight = 96f;
            heroLe.preferredHeight = 104f;
            var heroH = hero.AddComponent<HorizontalLayoutGroup>();
            heroH.spacing = 12f;
            heroH.childAlignment = TextAnchor.UpperLeft;
            heroH.childControlWidth = heroH.childControlHeight = true;
            heroH.childForceExpandWidth = true;
            heroH.childForceExpandHeight = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(hero.transform, false);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = iconLe.minWidth = 80f;
            iconLe.preferredHeight = iconLe.minHeight = 80f;
            Image icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.sprite = RacialUiTheme.PlaceholderSprite;

            var heroTextGo = new GameObject("HeroText", typeof(RectTransform));
            heroTextGo.transform.SetParent(hero.transform, false);
            var heroTextLe = heroTextGo.AddComponent<LayoutElement>();
            heroTextLe.flexibleWidth = 1f;
            TextMeshProUGUI heroText = RacialUiTheme.CreateText(
                heroTextGo.transform, "Text", string.Empty, RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.TopLeft);

            var scrollGo = new GameObject("BodyScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(root.transform, false);
            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 100f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);

            TextMeshProUGUI bodyText = RacialUiTheme.CreateText(
                content.transform, "Body", string.Empty, RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.TopLeft);
            bodyText.richText = true;
            var bodyFitter = content.AddComponent<ContentSizeFitter>();
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bodyLayout = content.AddComponent<LayoutElement>();
            bodyLayout.flexibleWidth = 1f;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;

            var view = root.AddComponent<EquipmentDetailPaneView>();
            view._heroIcon = icon;
            view._heroText = heroText;
            view._bodyText = bodyText;
            return view;
        }

        public void Refresh(CharacterEquipmentSheetModel sheet, CharacterEquipmentSelection selection) =>
            CharacterEquipmentDetailBuilder.PopulateDetailPane(_heroIcon, _heroText, _bodyText, sheet, selection);
    }
}
