using System.Collections.Generic;
using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneBoundaryApplicator
    {
        public static ZoneBoundaryStats ApplyAll(
            MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            IReadOnlyList<ResolvedZonePiece> pieces,
            IReadOnlyList<ResolvedZoneBoundary> boundaries,
            ZoneTilePaintContext paintContext)
        {
            var stats = new ZoneBoundaryStats();
            if (map == null || layout == null || pieces == null || boundaries == null)
                return stats;

            var pieceById = new Dictionary<string, ResolvedZonePiece>();
            var zoneByPieceId = new Dictionary<string, string>();
            for (int i = 0; i < pieces.Count; i++)
            {
                pieceById[pieces[i].PieceId] = pieces[i];
                zoneByPieceId[pieces[i].PieceId] = pieces[i].ZoneId;
            }

            for (int i = 0; i < boundaries.Count; i++)
            {
                ResolvedZoneBoundary boundary = boundaries[i];
                if (boundary.Kind == ZoneBoundaryKind.None)
                    continue;

                if (!pieceById.TryGetValue(boundary.Interface.PieceAId, out ResolvedZonePiece pieceA))
                    continue;

                pieceById.TryGetValue(boundary.Interface.PieceBId, out ResolvedZonePiece pieceB);
                ApplyBoundary(
                    map,
                    floorDef,
                    layout,
                    boundary,
                    pieceA,
                    pieceB,
                    zoneByPieceId,
                    paintContext,
                    ref stats);
            }

            map.FloorMap?.CompressBounds();
            map.WallMap?.CompressBounds();
            return stats;
        }

        static void ApplyBoundary(
            MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            ResolvedZoneBoundary boundary,
            ResolvedZonePiece pieceA,
            ResolvedZonePiece pieceB,
            IReadOnlyDictionary<string, string> zoneByPieceId,
            ZoneTilePaintContext paintContext,
            ref ZoneBoundaryStats stats)
        {
            ZoneInterface iface = boundary.Interface;
            int spanMin = iface.SpanMin;
            int spanMax = iface.SpanMax;

            if (!iface.IsExterior && !string.IsNullOrEmpty(pieceB.PieceId))
            {
                spanMin = ZoneInterfaceResolver.ComputeOverlapSpanMin(pieceA.Bounds, pieceB.Bounds, iface.EdgeOnA);
                spanMax = ZoneInterfaceResolver.ComputeOverlapSpanMax(pieceA.Bounds, pieceB.Bounds, iface.EdgeOnA);
            }

            if (spanMax <= spanMin)
                return;

            string zoneAId = ResolveZoneId(zoneByPieceId, pieceA, layout.FallbackZoneId);
            string zoneBId = ResolveZoneId(zoneByPieceId, pieceB, layout.FallbackZoneId);

            bool[] openings = BuildOpeningMask(
                spanMin,
                spanMax,
                boundary.Kind,
                boundary.CorridorCount,
                boundary.CorridorWidth);

            if (openings != null)
                stats.CorridorOpenings += boundary.CorridorCount;

            for (int span = spanMin; span < spanMax; span++)
            {
                Vector3Int edgeCell = EdgeCell(iface.EdgeOnA, iface.FixedCoordOnA, span);
                Vector3Int neighborCell = NeighborCell(iface.EdgeOnA, edgeCell);
                bool open = boundary.Kind == ZoneBoundaryKind.Open
                    || (openings != null && openings[span - spanMin]);

                if (open)
                {
                    ZoneTilePainter.PaintFloor(map, edgeCell, layout, floorDef, zoneAId, paintContext);
                    stats.OpenCells++;

                    if (InBounds(neighborCell, layout))
                    {
                        string neighborZone = iface.IsExterior
                            ? layout.FallbackZoneId
                            : zoneBId ?? layout.FallbackZoneId;
                        ZoneTilePainter.PaintFloor(map, neighborCell, layout, floorDef, neighborZone, paintContext);
                        stats.OpenCells++;
                    }
                }
                else
                {
                    ZoneTilePainter.PaintWall(map, edgeCell, layout, floorDef, zoneAId, paintContext);
                    stats.WallCells++;

                    if (InBounds(neighborCell, layout))
                    {
                        string neighborZone = iface.IsExterior
                            ? layout.FallbackZoneId
                            : zoneBId ?? layout.FallbackZoneId;
                        ZoneTilePainter.PaintWall(map, neighborCell, layout, floorDef, neighborZone, paintContext);
                        stats.WallCells++;
                    }
                }
            }
        }

        static string ResolveZoneId(
            IReadOnlyDictionary<string, string> zoneByPieceId,
            ResolvedZonePiece piece,
            string fallback)
        {
            if (string.IsNullOrEmpty(piece.PieceId))
                return fallback;

            return zoneByPieceId.TryGetValue(piece.PieceId, out string zoneId) && !string.IsNullOrEmpty(zoneId)
                ? zoneId
                : fallback;
        }

        static bool[] BuildOpeningMask(
            int spanMin,
            int spanMax,
            ZoneBoundaryKind kind,
            int corridorCount,
            int corridorWidth)
        {
            if (kind != ZoneBoundaryKind.Corridor && kind != ZoneBoundaryKind.Mixed)
                return null;

            int spanLength = spanMax - spanMin;
            if (spanLength <= 0)
                return null;

            var mask = new bool[spanLength];
            corridorCount = Mathf.Max(1, corridorCount);
            corridorWidth = Mathf.Clamp(corridorWidth, 1, spanLength);

            if (corridorCount == 1)
            {
                int start = spanMin + (spanLength - corridorWidth) / 2;
                MarkOpening(mask, spanMin, start, corridorWidth);
                return mask;
            }

            int spacing = spanLength / (corridorCount + 1);
            spacing = Mathf.Max(spacing, corridorWidth);
            for (int i = 0; i < corridorCount; i++)
            {
                int center = spanMin + spacing * (i + 1);
                int start = Mathf.Clamp(center - corridorWidth / 2, spanMin, spanMax - corridorWidth);
                MarkOpening(mask, spanMin, start, corridorWidth);
            }

            return mask;
        }

        static void MarkOpening(bool[] mask, int spanMin, int start, int width)
        {
            for (int i = 0; i < width; i++)
            {
                int index = start - spanMin + i;
                if (index >= 0 && index < mask.Length)
                    mask[index] = true;
            }
        }

        static Vector3Int EdgeCell(ZoneInterfaceEdge edge, int fixedCoord, int span) =>
            edge switch
            {
                ZoneInterfaceEdge.North => new Vector3Int(span, fixedCoord, 0),
                ZoneInterfaceEdge.South => new Vector3Int(span, fixedCoord, 0),
                ZoneInterfaceEdge.East => new Vector3Int(fixedCoord, span, 0),
                _ => new Vector3Int(fixedCoord, span, 0),
            };

        static Vector3Int NeighborCell(ZoneInterfaceEdge edge, Vector3Int edgeCell) =>
            edge switch
            {
                ZoneInterfaceEdge.North => edgeCell + Vector3Int.up,
                ZoneInterfaceEdge.South => edgeCell + Vector3Int.down,
                ZoneInterfaceEdge.East => edgeCell + Vector3Int.right,
                _ => edgeCell + Vector3Int.left,
            };

        static bool InBounds(Vector3Int cell, DungeonFloorZoneLayout layout) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < layout.FloorWidth && cell.y < layout.FloorHeight;
    }
}
