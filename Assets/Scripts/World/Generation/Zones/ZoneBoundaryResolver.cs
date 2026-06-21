using System;
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
            int corridorWidth,
            int[] openingWidths)
        {
            Interface = interfaceDef;
            Kind = kind;
            CorridorCount = corridorCount;
            CorridorWidth = corridorWidth;
            OpeningWidths = openingWidths ?? Array.Empty<int>();
        }

        public ZoneInterface Interface { get; }
        public ZoneBoundaryKind Kind { get; }
        public int CorridorCount { get; }
        public int CorridorWidth { get; }
        public int[] OpeningWidths { get; }
    }

    public static class ZoneBoundaryResolver
    {
        public static List<ResolvedZoneBoundary> ResolveAll(
            DungeonFloorZoneLayout layout,
            IReadOnlyList<ResolvedZonePiece> pieces,
            IReadOnlyList<ZoneInterface> interfaces,
            System.Random boundaryRng)
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
                ResolveCorridorParams(
                    layoutPiece,
                    iface,
                    kind,
                    boundaryRng,
                    out int corridorCount,
                    out int corridorWidth,
                    out int[] openingWidths);

                resolved.Add(new ResolvedZoneBoundary(iface, kind, corridorCount, corridorWidth, openingWidths));
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
            System.Random boundaryRng,
            out int corridorCount,
            out int corridorWidth,
            out int[] openingWidths)
        {
            corridorCount = 1;
            corridorWidth = 1;
            openingWidths = new[] { 1 };

            if (kind != ZoneBoundaryKind.Corridor && kind != ZoneBoundaryKind.Mixed)
                return;

            int minWidth = 1;
            int maxWidth = 1;
            if (TryGetEdgeBoundary(layoutPiece, iface.PieceBId, out ZoneEdgeBoundary edge))
            {
                corridorCount = Mathf.Max(1, edge.corridorCount);
                minWidth = edge.corridorWidthMin > 0 ? edge.corridorWidthMin : edge.corridorWidth;
                maxWidth = edge.corridorWidthMax > 0 ? edge.corridorWidthMax : edge.corridorWidth;
                minWidth = Mathf.Max(1, minWidth);
                maxWidth = Mathf.Max(minWidth, maxWidth);
            }

            openingWidths = ZoneBoundaryOpeningPlanner.RollOpeningWidths(
                corridorCount,
                minWidth,
                maxWidth,
                boundaryRng ?? new System.Random(0));
            corridorWidth = openingWidths.Length > 0 ? openingWidths[0] : 1;
        }

        static bool IsHabitatZone(string zoneId) =>
            !string.IsNullOrEmpty(zoneId)
            && zoneId != ZoneIds.Empty
            && zoneId != ZoneIds.Rock;
    }
}
