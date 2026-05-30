using JRogue.GridFeatures;
using JRogue.Manager.Map;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace JRogue.World.Lighting
{
    /// <summary>
    /// Dev QA overlay: warm tint on visible floor cells by live received light.
    /// Toggle with <see cref="toggleKey"/> (default L). See Lighting-QA-And-Torch-v0-Requirements.md §3.2.
    /// </summary>
    [DefaultExecutionOrder(250)]
    public sealed class LightingDebugOverlay : MonoBehaviour
    {
        static readonly Color WarmFull = new Color(0.91f, 0.63f, 0.25f, 0.55f);
        static readonly Color WarmFaint = new Color(0.91f, 0.63f, 0.25f, 0.22f);

        [SerializeField] bool enabledByDefaultInEditor;
        [SerializeField] Key toggleKey = Key.L;
        [SerializeField] Tilemap overlayMap;
        [SerializeField] int overlaySortingOrder = 4;
        [SerializeField] int lightThreshold = 3;

        bool _overlayActive;
        Tile _fillTile;

        public bool OverlayActive => _overlayActive;

        void Awake()
        {
#if UNITY_EDITOR
            if (enabledByDefaultInEditor)
                _overlayActive = true;
#endif
            EnsureOverlayMap();
            EnsureFillTile();
        }

        void Update()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            if (_overlayActive)
                SetOverlayActive(false);
            return;
#else
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
                SetOverlayActive(!_overlayActive);

            if (_overlayActive)
                RefreshOverlay();
#endif
        }

        [ContextMenu("Toggle Lighting Debug Overlay")]
        public void ToggleFromContextMenu() => SetOverlayActive(!_overlayActive);

        public void SetOverlayActive(bool active)
        {
            if (_overlayActive == active)
                return;

            _overlayActive = active;
            Debug.Log($"[Lighting:DebugOverlay] {(_overlayActive ? "on" : "off")}");

            if (!_overlayActive)
                ClearOverlay();
            else
                RefreshOverlay();
        }

        void RefreshOverlay()
        {
            EnsureOverlayMap();
            if (overlayMap == null)
                return;

            VisibilityManager visibility = FindAnyObjectByType<VisibilityManager>();
            LightingService lighting = LightingService.Instance;
            MapManager map = MapManager.Instance;
            if (visibility == null || lighting == null || map == null || map.FloorMap == null)
                return;

            ClearOverlay();

            Tilemap floor = map.FloorMap;
            BoundsInt bounds = floor.cellBounds;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (!floor.HasTile(pos))
                    continue;

                Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
                if (!visibility.IsVisible(cell))
                    continue;

                int received = lighting.GetReceivedLight(cell);
                if (received <= 0)
                    continue;

                Color tint = received >= lightThreshold ? WarmFull : WarmFaint;
                float strength = Mathf.Clamp01(received / (float)LightLevel.Max);
                tint.a *= strength;

                overlayMap.SetTile(cell, _fillTile);
                overlayMap.SetColor(cell, tint);
            }
        }

        void ClearOverlay()
        {
            if (overlayMap == null)
                return;

            overlayMap.ClearAllTiles();
        }

        void EnsureFillTile()
        {
            if (_fillTile != null)
                return;

            _fillTile = ScriptableObject.CreateInstance<Tile>();
            _fillTile.color = Color.white;
            var tex = Texture2D.whiteTexture;
            _fillTile.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        void EnsureOverlayMap()
        {
            if (overlayMap != null)
            {
                GridOverlayPainter.ConfigureRenderer(overlayMap, overlaySortingOrder);
                return;
            }

            Grid grid = FindAnyObjectByType<Grid>();
            if (grid == null)
                return;

            Transform existing = grid.transform.Find("Lighting_Debug_Overlay");
            if (existing != null)
            {
                overlayMap = existing.GetComponent<Tilemap>();
                if (overlayMap != null)
                {
                    GridOverlayPainter.ConfigureRenderer(overlayMap, overlaySortingOrder);
                    return;
                }
            }

            var go = new GameObject("Lighting_Debug_Overlay");
            go.transform.SetParent(grid.transform, false);
            overlayMap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();
            GridOverlayPainter.ConfigureRenderer(overlayMap, overlaySortingOrder);
        }
    }
}
