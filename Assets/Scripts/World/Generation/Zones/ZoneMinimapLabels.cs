using System.Collections.Generic;
using JRogue.Manager.Floor;
using JRogue.Manager.Map;
using JRogue.Manager.Visibility;
using JRogue.World.Generation;
using TMPro;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    /// <summary>
    /// Shows zone display names at habitat centroids once any cell in the zone has been seen.
    /// </summary>
    [DefaultExecutionOrder(270)]
    public sealed class ZoneMinimapLabels : MonoBehaviour
    {
        [SerializeField] bool showLabels = true;
        [SerializeField] float labelWorldHeight = 0.35f;
        [SerializeField] int fontSize = 18;
        [SerializeField] Color labelColor = new Color(0.95f, 0.92f, 0.75f, 0.9f);

        readonly Dictionary<string, TextMeshPro> _labelsByZoneId = new Dictionary<string, TextMeshPro>();

        void LateUpdate()
        {
            if (!showLabels)
            {
                HideAllLabels();
                return;
            }

            RefreshLabels();
        }

        void RefreshLabels()
        {
            DungeonFloorInstance floor = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            VisibilityManager visibility = FindAnyObjectByType<VisibilityManager>();
            MapManager map = MapManager.Instance;
            if (floor == null || visibility == null || map == null || floor.ResolvedZonePieces.Count == 0)
            {
                HideAllLabels();
                return;
            }

            var activeZones = new HashSet<string>();
            for (int i = 0; i < floor.ResolvedZonePieces.Count; i++)
            {
                ResolvedZonePiece piece = floor.ResolvedZonePieces[i];
                if (piece.ZoneId == ZoneIds.Empty || piece.ZoneId == ZoneIds.Rock)
                    continue;

                if (!IsZoneVisible(piece, floor, visibility, map))
                {
                    SetLabelVisible(piece.ZoneId, false);
                    continue;
                }

                activeZones.Add(piece.ZoneId);
                TextMeshPro label = GetOrCreateLabel(piece.ZoneId);
                label.text = ResolveDisplayName(piece.ZoneId, floor);
                label.color = labelColor;
                label.fontSize = fontSize;
                label.gameObject.SetActive(true);

                Vector3 world = ZoneCentroidWorld(piece.Bounds);
                world.y += labelWorldHeight;
                label.transform.position = world;
            }

            foreach (KeyValuePair<string, TextMeshPro> pair in _labelsByZoneId)
            {
                if (!activeZones.Contains(pair.Key) && pair.Value != null)
                    pair.Value.gameObject.SetActive(false);
            }
        }

        static bool IsZoneVisible(
            ResolvedZonePiece piece,
            DungeonFloorInstance floor,
            VisibilityManager visibility,
            MapManager map)
        {
            RectInt bounds = piece.Bounds;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!map.IsWalkable(cell))
                        continue;

                    if (!floor.TryGetZoneId(cell, out string zoneId) || zoneId != piece.ZoneId)
                        continue;

                    if (visibility.IsVisible(cell) || visibility.IsLitVisible(cell))
                        return true;
                }
            }

            return false;
        }

        static string ResolveDisplayName(string zoneId, DungeonFloorInstance floor)
        {
            DungeonFloorZoneLayout layout = floor.Definition?.ZoneLayout;
            if (layout != null
                && layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef)
                && !string.IsNullOrWhiteSpace(zoneDef.DisplayName))
            {
                return zoneDef.DisplayName;
            }

            return zoneId;
        }

        static Vector3 ZoneCentroidWorld(RectInt bounds)
        {
            Vector3Int center = new Vector3Int(
                (bounds.xMin + bounds.xMax) / 2,
                (bounds.yMin + bounds.yMax) / 2,
                0);
            return FloorItemPileService.TileCenterWorld(center);
        }

        TextMeshPro GetOrCreateLabel(string zoneId)
        {
            if (_labelsByZoneId.TryGetValue(zoneId, out TextMeshPro existing) && existing != null)
                return existing;

            var go = new GameObject($"ZoneLabel_{zoneId}");
            go.transform.SetParent(transform, false);
            TextMeshPro label = go.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.sortingOrder = 50;
            _labelsByZoneId[zoneId] = label;
            return label;
        }

        void SetLabelVisible(string zoneId, bool visible)
        {
            if (_labelsByZoneId.TryGetValue(zoneId, out TextMeshPro label) && label != null)
                label.gameObject.SetActive(visible);
        }

        void HideAllLabels()
        {
            foreach (KeyValuePair<string, TextMeshPro> pair in _labelsByZoneId)
            {
                if (pair.Value != null)
                    pair.Value.gameObject.SetActive(false);
            }
        }
    }
}
