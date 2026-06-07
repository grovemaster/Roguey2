using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public readonly struct ResolvedZoneBoundary
    {
        public ResolvedZoneBoundary(
            ZoneInterface interfaceDef,
            ZoneBoundaryKind kind,
            int corridorCount,
            int corridorWidth)
        {
            Interface = interfaceDef;
            Kind = kind;
            CorridorCount = corridorCount;
            CorridorWidth = corridorWidth;
        }

        public ZoneInterface Interface { get; }
        public ZoneBoundaryKind Kind { get; }
        public int CorridorCount { get; }
        public int CorridorWidth { get; }
    }

    public static class ZoneBoundaryResolver
    {
        public static List<ResolvedZoneBoundary> ResolveAll(
            DungeonFloorZoneLayout layout,
            IReadOnlyList<ResolvedZonePiece> pieces,
            IReadOnlyList<ZoneInterface> interfaces)
        {
            var resolved = new List<ResolvedZoneBoundary>();
            if (layout == null || pieces == null || interfaces == null)
                return resolved;

            var pieceById = new Dictionary<string, ResolvedZonePiece>();
            for (int i = 0; i < pieces.Count; i++)
                pieceById[pieces[i].PieceId] = pieces[i];

            for (int i = 0; i < interfaces.Count; i++)
            {
                ZoneInterface iface = interfaces[i];
                if (!pieceById.TryGetValue(iface.PieceAId, out ResolvedZonePiece pieceA))
                    continue;

                pieceById.TryGetValue(iface.PieceBId, out ResolvedZonePiece pieceB);
                layout.TryGetLayoutPiece(iface.PieceAId, out ZoneLayoutPiece layoutPiece);

                ZoneBoundaryKind kind = ResolveKind(layout, layoutPiece, iface, pieceA, pieceB);
                ResolveCorridorParams(layoutPiece, iface, kind, out int corridorCount, out int corridorWidth);

                resolved.Add(new ResolvedZoneBoundary(iface, kind, corridorCount, corridorWidth));
            }

            return resolved;
        }

        public static ZoneBoundaryKind ResolveKind(
            DungeonFloorZoneLayout layout,
            ZoneLayoutPiece layoutPiece,
            ZoneInterface iface,
            ResolvedZonePiece pieceA,
            ResolvedZonePiece pieceB)
        {
            if (iface.IsExterior)
                return layout.DefaultOuterBoundary;

            if (!IsHabitatZone(pieceA.ZoneId) || !IsHabitatZone(pieceB.ZoneId))
                return ZoneBoundaryKind.Wall;

            if (TryGetEdgeBoundary(layoutPiece, iface.PieceBId, out ZoneEdgeBoundary edge))
                return edge.boundaryKind;

            if (layoutPiece.defaultBoundary != ZoneBoundaryKind.None)
                return layoutPiece.defaultBoundary;

            return ZoneBoundaryKind.None;
        }

        static bool TryGetEdgeBoundary(ZoneLayoutPiece layoutPiece, string neighborPieceId, out ZoneEdgeBoundary edge)
        {
            edge = default;
            if (layoutPiece.edgeBoundaries == null || string.IsNullOrEmpty(neighborPieceId))
                return false;

            for (int i = 0; i < layoutPiece.edgeBoundaries.Length; i++)
            {
                ZoneEdgeBoundary candidate = layoutPiece.edgeBoundaries[i];
                if (candidate.neighborPieceId != neighborPieceId)
                    continue;

                edge = candidate;
                return true;
            }

            return false;
        }

        static void ResolveCorridorParams(
            ZoneLayoutPiece layoutPiece,
            ZoneInterface iface,
            ZoneBoundaryKind kind,
            out int corridorCount,
            out int corridorWidth)
        {
            corridorCount = 1;
            corridorWidth = 1;

            if (kind != ZoneBoundaryKind.Corridor && kind != ZoneBoundaryKind.Mixed)
                return;

            if (TryGetEdgeBoundary(layoutPiece, iface.PieceBId, out ZoneEdgeBoundary edge))
            {
                corridorCount = Mathf.Max(1, edge.corridorCount);
                corridorWidth = Mathf.Max(1, edge.corridorWidth);
            }
        }

        static bool IsHabitatZone(string zoneId) =>
            !string.IsNullOrEmpty(zoneId)
            && zoneId != ZoneIds.Empty
            && zoneId != ZoneIds.Rock;
    }
}
