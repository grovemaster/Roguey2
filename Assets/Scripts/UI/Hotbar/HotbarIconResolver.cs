using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;
using JRogue.UI.Inventory;
using UnityEngine;

namespace JRogue.UI.Hotbar
{
    public static class HotbarIconResolver
    {
        static readonly Color PlaceholderColor = new Color(0.35f, 0.38f, 0.45f, 1f);
        static Sprite _placeholderSprite;

        public static Sprite GetIcon(HotbarResolvedAction resolved, BaseActor actor = null)
        {
            if (!resolved.IsValid && resolved.Ability == null && resolved.ItemInstance == null)
                return GetPlaceholderSprite();

            EssenceData essence = null;
            if (actor != null
                && resolved.Kind == HotbarEntryKind.EssenceActive
                && actor.TryGetComponent(out EssenceSlotManager essenceManager))
            {
                essence = essenceManager.GetEssenceInSlot(resolved.SlotIndex);
            }

            ItemData item = resolved.ItemInstance?.Definition;
            return GetIcon(resolved.Ability, item, essence);
        }

        public static Sprite GetIcon(AbilityAction ability, ItemData item = null, EssenceData essence = null)
        {
            if (ability?.hotbarIcon != null)
                return ability.hotbarIcon;

            if (item?.icon != null)
                return item.icon;

            if (essence?.mapIcon != null)
                return essence.mapIcon;

            return GetPlaceholderSprite();
        }

        public static Color GetPlaceholderColor(ItemData item)
        {
            if (item == null)
                return PlaceholderColor;

            ItemCategoryUiInfo info = ItemCategoryRegistry.Get(item.category);
            float hue = (info.SortOrder % 360) / 360f;
            return Color.HSVToRGB(hue, 0.25f, 0.55f);
        }

        static Sprite GetPlaceholderSprite()
        {
            if (_placeholderSprite != null)
                return _placeholderSprite;

            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = PlaceholderColor;
            texture.SetPixels(pixels);
            texture.Apply();

            _placeholderSprite = Sprite.Create(
                texture,
                new Rect(0, 0, 8, 8),
                new Vector2(0.5f, 0.5f),
                8f);
            return _placeholderSprite;
        }
    }
}
