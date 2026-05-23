using JRogue.Item;
using UnityEngine;

namespace JRogue.Item.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FloorItemWorldView : MonoBehaviour
    {
        const string EntitiesSortingLayer = "Entities";
        const string DiamondResourcePath = "Item/Currency/ManaStone_Tier9";

        public string EntryId { get; private set; }

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
            Sprite sprite = instance?.Definition?.icon;
            if (sprite == null && instance?.Definition is ManaStoneItemData)
                sprite = LoadManaStoneFallbackIcon();

            if (sprite != null)
            {
                _spriteRenderer.sprite = sprite;
                return;
            }

            Debug.LogWarning(
                $"[LOOT] Floor item view has no sprite for '{instance?.Definition?.itemName ?? "unknown"}'.");
        }

        static Sprite LoadManaStoneFallbackIcon()
        {
            var tier9 = Resources.Load<ManaStoneItemData>(DiamondResourcePath);
            return tier9 != null ? tier9.icon : null;
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
