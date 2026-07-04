using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Shared south/north strip linking dimension_square and holy_land_nexus.</summary>
    public static class DistrictSquareHolyNexusTransition
    {
        public const string SquareToNexusLinkId = HolyLandTransitionIds.SquareToNexus;
        public const string NexusToSquareLinkId = HolyLandTransitionIds.NexusToSquare;

        public const int StripMinX = DimensionSquareLayout.ArmMin;
        public const int StripMaxX = DimensionSquareLayout.ArmMax;

        public const int SquareSouthEdgeY = 0;
        public const int NexusNorthEdgeY = DimensionSquareLayout.MapSize - 1;

        public static readonly Vector3Int SquareArrivalCell = new Vector3Int(DimensionSquareLayout.Center, 1, 0);
        public static readonly Vector3Int NexusArrivalCell = new Vector3Int(DimensionSquareLayout.Center, 38, 0);

        public static bool IsSquareSouthTransitionCell(Vector3Int cell) =>
            cell.y == SquareSouthEdgeY && cell.x >= StripMinX && cell.x <= StripMaxX;

        public static bool IsNexusNorthTransitionCell(Vector3Int cell) =>
            cell.y == NexusNorthEdgeY && cell.x >= StripMinX && cell.x <= StripMaxX;
    }
}
