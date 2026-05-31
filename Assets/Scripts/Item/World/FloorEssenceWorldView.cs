using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Item.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FloorEssenceWorldView : MonoBehaviour
    {
        const string EntitiesSortingLayer = "Entities";
        const string DefaultIconResource = "Item/Essence/Essence_MapIcon_Fallback";

        static Sprite _placeholderSprite;

        public string EntryId { get; private set; }
        public Vector3Int GridCell { get; private set; }

        SpriteRenderer _spriteRenderer;

        void Awake() => _spriteRenderer = GetComponent<SpriteRenderer>();

        public void Bind(string entryId, EssenceData essence)
        {
            EntryId = entryId;
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            _spriteRenderer.sortingLayerName = EntitiesSortingLayer;
            _spriteRenderer.sortingOrder = 2;
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sprite = ResolveSprite(essence);
            name = essence != null ? $"FloorEssence_{essence.essenceName}" : "FloorEssence";
        }

        public void SetGridCell(Vector3Int cell) => GridCell = new Vector3Int(cell.x, cell.y, 0);

        public void SetVisible(bool visible)
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = visible;
        }

        public static FloorEssenceWorldView CreateDefault(Transform parent)
        {
            var go = new GameObject("FloorEssence");
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            var view = go.AddComponent<FloorEssenceWorldView>();
            view._spriteRenderer = sr;
            return view;
        }

        static Sprite ResolveSprite(EssenceData essence)
        {
            if (essence?.mapIcon != null)
                return essence.mapIcon;

            var fallback = Resources.Load<EssenceData>("Item/Essence/SuddenStrength");
            if (fallback?.mapIcon != null)
                return fallback.mapIcon;

            return GetPlaceholderSprite();
        }

        static Sprite GetPlaceholderSprite()
        {
            if (_placeholderSprite != null)
                return _placeholderSprite;

            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color c = new Color(0.95f, 0.85f, 0.2f, 1f);
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                tex.SetPixel(x, y, c);
            tex.Apply();

            _placeholderSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            return _placeholderSprite;
        }
    }
}
