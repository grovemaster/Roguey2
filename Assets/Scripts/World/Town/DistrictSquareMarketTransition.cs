using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Shared north/south strip linking dimension_square and town_market.</summary>
    public static class DistrictSquareMarketTransition
    {
        public const string SquareToMarketLinkId = "district_square_to_market";
        public const string MarketToSquareLinkId = "district_market_to_square";

        public const int StripMinX = DimensionSquareLayout.ArmMin;
        public const int StripMaxX = DimensionSquareLayout.ArmMax;

        public const int SquareNorthEdgeY = DimensionSquareLayout.MapSize - 1;
        public const int MarketSouthEdgeY = 0;

        public static readonly Vector3Int SquareArrivalCell = new Vector3Int(DimensionSquareLayout.Center, 38, 0);
        public static readonly Vector3Int MarketArrivalCell = new Vector3Int(DimensionSquareLayout.Center, 1, 0);

        public static bool IsSquareNorthTransitionCell(Vector3Int cell) =>
            cell.y == SquareNorthEdgeY && cell.x >= StripMinX && cell.x <= StripMaxX;

        public static bool IsMarketSouthTransitionCell(Vector3Int cell) =>
            cell.y == MarketSouthEdgeY && cell.x >= StripMinX && cell.x <= StripMaxX;
    }
}
