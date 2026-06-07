using System.Collections.Generic;
using JRogue.GridFeatures;
using JRogue.Manager.Map;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Zones
{
    /// <summary>
    /// Dev overlay: tint explored walkable cells by zone id. Toggle with <see cref="toggleKey"/> (default Z).
    /// </summary>
    [DefaultExecutionOrder(260)]
    public sealed class ZoneDebugOverlay : MonoBehaviour
    {
        [SerializeField] bool enabledByDefaultInEditor;
        [SerializeField] Key toggleKey = Key.Z;
        [SerializeField] Tilemap overlayMap;
        [SerializeField] int overlaySortingOrder = 5;

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

        public void SetOverlayActive(bool active)
        {
            if (_overlayActive == active)
                return;

            _overlayActive = active;
            Debug.Log($"[ZoneDebugOverlay] {(_overlayActive ? "on" : "off")}");

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

            DungeonFloorInstance floor = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            MapManager map = MapManager.Instance;
            if (floor == null || map == null || map.FloorMap == null)
                return;

            ClearOverlay();

            Tilemap source = map.FloorMap;
            BoundsInt bounds = source.cellBounds;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (!source.HasTile(pos))
                    continue;

                Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
                if (!map.IsWalkable(cell))
                    continue;

                if (!floor.TryGetZoneId(cell, out string zoneId) || string.IsNullOrEmpty(zoneId))
                    zoneId = ZoneIds.Rock;

                Color tint = ColorForZone(zoneId);
                overlayMap.SetTile(cell, _fillTile);
                overlayMap.SetColor(cell, tint);
            }
        }

        static Color ColorForZone(string zoneId)
        {
            int hash = zoneId.GetHashCode();
            float r = 0.35f + ((hash & 0xFF) / 255f) * 0.45f;
            float g = 0.35f + (((hash >> 8) & 0xFF) / 255f) * 0.45f;
            float b = 0.35f + (((hash >> 16) & 0xFF) / 255f) * 0.45f;
            return new Color(r, g, b, 0.45f);
        }

        void ClearOverlay()
        {
            overlayMap?.ClearAllTiles();
        }

        void EnsureFillTile()
        {
            if (_fillTile != null)
                return;

            _fillTile = ScriptableObject.CreateInstance<Tile>();
            _fillTile.color = Color.white;
            _fillTile.sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f);
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

            Transform existing = grid.transform.Find("Zone_Debug_Overlay");
            if (existing != null)
            {
                overlayMap = existing.GetComponent<Tilemap>();
                if (overlayMap != null)
                {
                    GridOverlayPainter.ConfigureRenderer(overlayMap, overlaySortingOrder);
                    return;
                }
            }

            var go = new GameObject("Zone_Debug_Overlay");
            go.transform.SetParent(grid.transform, false);
            overlayMap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();
            GridOverlayPainter.ConfigureRenderer(overlayMap, overlaySortingOrder);
        }
    }
}
