using System;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public enum ZoneInterfaceEdge
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
    }

    public readonly struct ZoneInterface
    {
        public ZoneInterface(
            string pieceAId,
            string pieceBId,
            ZoneInterfaceEdge edgeOnA,
            int spanMin,
            int spanMax,
            int fixedCoordOnA)
        {
            PieceAId = pieceAId;
            PieceBId = pieceBId;
            EdgeOnA = edgeOnA;
            SpanMin = spanMin;
            SpanMax = spanMax;
            FixedCoordOnA = fixedCoordOnA;
        }

        public string PieceAId { get; }
        public string PieceBId { get; }
        public ZoneInterfaceEdge EdgeOnA { get; }
        public int SpanMin { get; }
        public int SpanMax { get; }
        public int FixedCoordOnA { get; }
        public bool IsExterior => PieceBId == ZoneIds.ExteriorNeighbor;
    }

    public struct ZoneBoundaryStats
    {
        public int OpenCells;
        public int WallCells;
        public int CorridorOpenings;
    }
}
