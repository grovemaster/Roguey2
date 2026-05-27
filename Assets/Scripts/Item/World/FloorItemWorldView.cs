using JRogue.Item;
using UnityEngine;

namespace JRogue.Item.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FloorItemWorldView : MonoBehaviour
    {
        const string EntitiesSortingLayer = "Entities";
        const string DiamondResourcePath = "Item/Currency/ManaStone_Tier9";
        const string WeaponIconResourcePath = "Item/Weapon/Giants_Blade";

        static Sprite _placeholderSprite;

        public string EntryId { get; private set; }
        public Vector3Int GridCell { get; private set; }

        SpriteRenderer _spriteRenderer;

        void Awake() => _spriteRenderer = GetComponent<SpriteRenderer>();

        public void Bind(string entryId, ItemInstance instance)
        {
            EntryId = entryId;
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            ApplySorting();
            ApplySprite(instance);
            name = BuildName(instance);
        }

        public void SetGridCell(Vector3Int cell) => GridCell = new Vector3Int(cell.x, cell.y, 0);

        public void SetVisible(bool visible)
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = visible;
        }

        public static FloorItemWorldView CreateDefault(Transform parent)
        {
            var go = new GameObject("FloorItem");
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            var view = go.AddComponent<FloorItemWorldView>();
            view._spriteRenderer = sr;
            view.ApplySorting();
            return view;
        }

        void ApplySorting()
        {
            _spriteRenderer.sortingLayerName = EntitiesSortingLayer;
            _spriteRenderer.sortingOrder = 2;
        }

        void ApplySprite(ItemInstance instance)
        {
            ItemData def = instance?.Definition;
            Sprite sprite = ResolveSprite(def);

            if (sprite == null)
            {
                sprite = GetPlaceholderSprite();
                _spriteRenderer.color = PlaceholderTint(def);
                if (def != null && def.icon == null)
                    Debug.LogWarning(
                        $"[LOOT] Floor item '{def.itemName}' has no ItemData.icon; showing placeholder on tile.");
            }
            else
            {
                _spriteRenderer.color = Color.white;
            }

            _spriteRenderer.sprite = sprite;
        }

        static Sprite ResolveSprite(ItemData def)
        {
            if (def == null)
                return null;

            if (def.icon != null)
                return def.icon;

            if (def is ManaStoneItemData)
                return LoadManaStoneFallbackIcon();

            if (def.category == ItemCategory.Weapon)
                return LoadItemDataIcon(WeaponIconResourcePath);

            if (def.category == ItemCategory.Potion)
                return LoadItemDataIcon("Item/Potion/PotionOfExperience");

            return null;
        }

        static Sprite LoadItemDataIcon(string resourcesPath)
        {
            var data = Resources.Load<ItemData>(resourcesPath);
            return data != null ? data.icon : null;
        }

        static Sprite LoadManaStoneFallbackIcon()
        {
            var tier9 = Resources.Load<ManaStoneItemData>(DiamondResourcePath);
            return tier9 != null ? tier9.icon : null;
        }

        static Sprite GetPlaceholderSprite()
        {
            if (_placeholderSprite != null)
                return _placeholderSprite;

            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color c = new Color(0.85f, 0.75f, 0.35f, 1f);
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                tex.SetPixel(x, y, c);
            tex.Apply();

            _placeholderSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            return _placeholderSprite;
        }

        static Color PlaceholderTint(ItemData def)
        {
            if (def == null)
                return new Color(0.7f, 0.7f, 0.75f, 1f);

            return def.category switch
            {
                ItemCategory.Weapon => new Color(0.9f, 0.85f, 0.55f, 1f),
                ItemCategory.Potion => new Color(0.55f, 0.75f, 0.95f, 1f),
                ItemCategory.Currency => new Color(0.75f, 0.9f, 0.7f, 1f),
                _ => new Color(0.75f, 0.78f, 0.85f, 1f)
            };
        }

        static string BuildName(ItemInstance instance)
        {
            if (instance?.Definition == null)
                return "FloorItem";

            if (instance.Definition is ManaStoneItemData ms)
                return $"ManaStone_T{ms.tier}_{instance.ManaStoneSourceSpeciesId}";

            return $"FloorItem_{instance.Definition.itemName}";
        }
    }
}
