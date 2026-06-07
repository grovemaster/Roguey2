using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneInterfaceResolver
    {
        public static List<ZoneInterface> ResolveInterfaces(
            IReadOnlyList<ResolvedZonePiece> pieces,
            int mapWidth,
            int mapHeight)
        {
            var interfaces = new List<ZoneInterface>();
            if (pieces == null || pieces.Count == 0)
                return interfaces;

            for (int i = 0; i < pieces.Count; i++)
                AddExteriorInterfaces(interfaces, pieces[i], mapWidth, mapHeight);

            for (int i = 0; i < pieces.Count; i++)
            {
                ResolvedZonePiece a = pieces[i];
                for (int j = i + 1; j < pieces.Count; j++)
                    TryAddAdjacency(interfaces, a, pieces[j]);
            }

            return interfaces;
        }

        static void TryAddAdjacency(List<ZoneInterface> interfaces, ResolvedZonePiece a, ResolvedZonePiece b)
        {
            if (a.Bounds.xMax == b.Bounds.xMin)
                AddSharedEdge(interfaces, a, b, ZoneInterfaceEdge.East, a.Bounds.xMax - 1);
            else if (b.Bounds.xMax == a.Bounds.xMin)
                AddSharedEdge(interfaces, a, b, ZoneInterfaceEdge.West, a.Bounds.xMin);

            if (a.Bounds.yMax == b.Bounds.yMin)
                AddSharedEdge(interfaces, a, b, ZoneInterfaceEdge.North, a.Bounds.yMax - 1);
            else if (b.Bounds.yMax == a.Bounds.yMin)
                AddSharedEdge(interfaces, a, b, ZoneInterfaceEdge.South, a.Bounds.yMin);
        }

        static void AddSharedEdge(
            List<ZoneInterface> interfaces,
            ResolvedZonePiece pieceA,
            ResolvedZonePiece pieceB,
            ZoneInterfaceEdge edgeOnA,
            int fixedCoordOnA)
        {
            int spanMin = ComputeOverlapSpanMin(pieceA.Bounds, pieceB.Bounds, edgeOnA);
            int spanMax = ComputeOverlapSpanMax(pieceA.Bounds, pieceB.Bounds, edgeOnA);
            if (spanMax <= spanMin)
                return;

            interfaces.Add(new ZoneInterface(
                pieceA.PieceId,
                pieceB.PieceId,
                edgeOnA,
                spanMin,
                spanMax,
                fixedCoordOnA));
        }

        static void AddExteriorInterfaces(
            List<ZoneInterface> interfaces,
            ResolvedZonePiece piece,
            int mapWidth,
            int mapHeight)
        {
            RectInt bounds = piece.Bounds;
            if (bounds.xMin <= 0)
            {
                interfaces.Add(new ZoneInterface(
                    piece.PieceId,
                    ZoneIds.ExteriorNeighbor,
                    ZoneInterfaceEdge.West,
                    bounds.yMin,
                    bounds.yMax,
                    bounds.xMin));
            }

            if (bounds.xMax >= mapWidth)
            {
                interfaces.Add(new ZoneInterface(
                    piece.PieceId,
                    ZoneIds.ExteriorNeighbor,
                    ZoneInterfaceEdge.East,
                    bounds.yMin,
                    bounds.yMax,
                    bounds.xMax - 1));
            }

            if (bounds.yMin <= 0)
            {
                interfaces.Add(new ZoneInterface(
                    piece.PieceId,
                    ZoneIds.ExteriorNeighbor,
                    ZoneInterfaceEdge.South,
                    bounds.xMin,
                    bounds.xMax,
                    bounds.yMin));
            }

            if (bounds.yMax >= mapHeight)
            {
                interfaces.Add(new ZoneInterface(
                    piece.PieceId,
                    ZoneIds.ExteriorNeighbor,
                    ZoneInterfaceEdge.North,
                    bounds.xMin,
                    bounds.xMax,
                    bounds.yMax - 1));
            }
        }

        public static int ComputeOverlapSpanMin(RectInt boundsA, RectInt boundsB, ZoneInterfaceEdge edgeOnA)
        {
            if (edgeOnA == ZoneInterfaceEdge.North || edgeOnA == ZoneInterfaceEdge.South)
                return Mathf.Max(boundsA.xMin, boundsB.xMin);

            return Mathf.Max(boundsA.yMin, boundsB.yMin);
        }

        public static int ComputeOverlapSpanMax(RectInt boundsA, RectInt boundsB, ZoneInterfaceEdge edgeOnA)
        {
            if (edgeOnA == ZoneInterfaceEdge.North || edgeOnA == ZoneInterfaceEdge.South)
                return Mathf.Min(boundsA.xMax, boundsB.xMax);

            return Mathf.Min(boundsA.yMax, boundsB.yMax);
        }
    }
}
