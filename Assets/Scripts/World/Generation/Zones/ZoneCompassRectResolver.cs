using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneCompassRectResolver
    {
        const float Floor01NorthBandFraction = 0.65f;
        const float Floor01EastBandFraction = 0.70f;

        public static RectInt ResolveRect(NormalizedRect normalized, int floorWidth, int floorHeight)
        {
            int xMin = Mathf.Clamp(Mathf.FloorToInt(normalized.xMin * floorWidth), 0, floorWidth - 1);
            int yMin = Mathf.Clamp(Mathf.FloorToInt(normalized.yMin * floorHeight), 0, floorHeight - 1);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(normalized.xMax * floorWidth) - 1, xMin, floorWidth - 1);
            int yMax = Mathf.Clamp(Mathf.CeilToInt(normalized.yMax * floorHeight) - 1, yMin, floorHeight - 1);
            return FromInclusiveBounds(xMin, yMin, xMax, yMax);
        }

        public static RectInt FromInclusiveBounds(int xMin, int yMin, int xMaxInclusive, int yMaxInclusive) =>
            new RectInt(xMin, yMin, xMaxInclusive - xMin + 1, yMaxInclusive - yMin + 1);

        public static RectInt ResolvePieceRect(ZoneLayoutPiece piece, int floorWidth, int floorHeight)
        {
            if (piece.anchorKind == ZonePieceAnchorKind.Compass)
                return ResolveCompassPreset(piece.compassDirection, floorWidth, floorHeight);

            return ResolveRect(piece.normalizedRect, floorWidth, floorHeight);
        }

        /// <summary>
        /// Non-overlapping Floor 1-style compass partition (center + north + east).
        /// Shared edges go to the north/east piece so optional bands do not overlap center.
        /// </summary>
        public static RectInt ResolveCompassPreset(CompassDirection direction, int floorWidth, int floorHeight)
        {
            int northRow = NorthBandStartRow(floorHeight);
            int eastColumn = EastBandStartColumn(floorWidth);
            int lastX = floorWidth - 1;
            int lastY = floorHeight - 1;
            int lowerMaxY = northRow - 1;
            int westMaxX = eastColumn - 1;

            return direction switch
            {
                CompassDirection.North => FromInclusiveBounds(0, northRow, lastX, lastY),
                CompassDirection.East => FromInclusiveBounds(eastColumn, 0, lastX, lowerMaxY),
                CompassDirection.Center => FromInclusiveBounds(0, 0, westMaxX, lowerMaxY),
                CompassDirection.South => FromInclusiveBounds(0, 0, lastX, Mathf.Max(0, lowerMaxY / 2)),
                CompassDirection.West => FromInclusiveBounds(
                    0,
                    Mathf.CeilToInt(floorHeight * 0.35f),
                    Mathf.Max(0, westMaxX / 2),
                    lowerMaxY),
                _ => FromInclusiveBounds(
                    floorWidth / 4,
                    floorHeight / 4,
                    lastX - floorWidth / 4,
                    lastY - floorHeight / 4),
            };
        }

        public static int NorthBandStartRow(int floorHeight) =>
            Mathf.CeilToInt(floorHeight * Floor01NorthBandFraction);

        public static int EastBandStartColumn(int floorWidth) =>
            Mathf.CeilToInt(floorWidth * Floor01EastBandFraction);

        public static bool RectsOverlap(RectInt a, RectInt b) =>
            a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;

        public static Vector3Int ResolvePlayerStart(RectInt bounds) =>
            new Vector3Int((bounds.xMin + bounds.xMax) / 2, (bounds.yMin + bounds.yMax) / 2, 0);
    }
}
