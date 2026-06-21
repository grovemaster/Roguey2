using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneBoundaryOpeningPlanner
    {
        public static int[] RollOpeningWidths(int corridorCount, int minWidth, int maxWidth, System.Random rng)
        {
            corridorCount = Mathf.Max(1, corridorCount);
            minWidth = Mathf.Max(1, minWidth);
            maxWidth = Mathf.Max(minWidth, maxWidth);

            var widths = new int[corridorCount];
            for (int i = 0; i < corridorCount; i++)
                widths[i] = minWidth == maxWidth ? minWidth : rng.Next(minWidth, maxWidth + 1);

            return widths;
        }

        public static bool[] BuildOpeningMask(int spanMin, int spanMax, ZoneBoundaryKind kind, int[] openingWidths)
        {
            if (kind != ZoneBoundaryKind.Corridor && kind != ZoneBoundaryKind.Mixed)
                return null;

            int spanLength = spanMax - spanMin;
            if (spanLength <= 0 || openingWidths == null || openingWidths.Length == 0)
                return null;

            var mask = new bool[spanLength];
            int corridorCount = openingWidths.Length;

            if (corridorCount == 1)
            {
                int width = Mathf.Clamp(openingWidths[0], 1, spanLength);
                int start = spanMin + (spanLength - width) / 2;
                MarkOpening(mask, spanMin, start, width);
                return mask;
            }

            int spacing = spanLength / (corridorCount + 1);
            for (int i = 0; i < corridorCount; i++)
            {
                int width = Mathf.Clamp(openingWidths[i], 1, spanLength);
                spacing = Mathf.Max(spacing, width);
                int center = spanMin + spacing * (i + 1);
                int start = Mathf.Clamp(center - width / 2, spanMin, spanMax - width);
                MarkOpening(mask, spanMin, start, width);
            }

            return mask;
        }

        public static List<Vector3Int> CollectOpeningEdgeCells(
            ResolvedZoneBoundary boundary,
            ResolvedZonePiece piece,
            ResolvedZonePiece neighborPiece)
        {
            var cells = new List<Vector3Int>();
            if (boundary.Kind != ZoneBoundaryKind.Corridor && boundary.Kind != ZoneBoundaryKind.Mixed)
                return cells;

            ZoneInterface iface = boundary.Interface;
            bool onPieceA = iface.PieceAId == piece.PieceId;
            bool onPieceB = iface.PieceBId == piece.PieceId;
            if (!onPieceA && !onPieceB)
                return cells;

            int spanMin = iface.SpanMin;
            int spanMax = iface.SpanMax;
            if (!iface.IsExterior && neighborPiece.PieceId != null)
            {
                ResolvedZonePiece pieceA = onPieceA ? piece : neighborPiece;
                ResolvedZonePiece pieceB = onPieceA ? neighborPiece : piece;
                spanMin = ZoneInterfaceResolver.ComputeOverlapSpanMin(pieceA.Bounds, pieceB.Bounds, iface.EdgeOnA);
                spanMax = ZoneInterfaceResolver.ComputeOverlapSpanMax(pieceA.Bounds, pieceB.Bounds, iface.EdgeOnA);
            }

            ZoneInterfaceEdge edge = onPieceA ? iface.EdgeOnA : OppositeEdge(iface.EdgeOnA);
            int fixedCoord = onPieceA ? iface.FixedCoordOnA : NeighborFixedCoord(iface.EdgeOnA, iface.FixedCoordOnA);

            bool[] mask = BuildOpeningMask(spanMin, spanMax, boundary.Kind, boundary.OpeningWidths);
            if (mask == null)
                return cells;

            for (int span = spanMin; span < spanMax; span++)
            {
                if (!mask[span - spanMin])
                    continue;

                cells.Add(EdgeCell(edge, fixedCoord, span));
            }

            return cells;
        }

        static ZoneInterfaceEdge OppositeEdge(ZoneInterfaceEdge edge) =>
            edge switch
            {
                ZoneInterfaceEdge.North => ZoneInterfaceEdge.South,
                ZoneInterfaceEdge.South => ZoneInterfaceEdge.North,
                ZoneInterfaceEdge.East => ZoneInterfaceEdge.West,
                _ => ZoneInterfaceEdge.East,
            };

        static int NeighborFixedCoord(ZoneInterfaceEdge edgeOnA, int fixedCoordOnA) =>
            edgeOnA switch
            {
                ZoneInterfaceEdge.North => fixedCoordOnA + 1,
                ZoneInterfaceEdge.South => fixedCoordOnA - 1,
                ZoneInterfaceEdge.East => fixedCoordOnA + 1,
                _ => fixedCoordOnA - 1,
            };

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
    }
}
