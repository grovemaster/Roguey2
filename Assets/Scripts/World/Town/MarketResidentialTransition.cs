using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Shared west/east strip linking town_residential and town_market (bottom-aligned, full residential height).</summary>
    public static class MarketResidentialTransition
    {
        public const string ResidentialToMarketLinkId = "district_residential_to_market";
        public const string MarketToResidentialLinkId = "district_market_to_residential";

        public const int StripMinY = 0;
        public const int StripMaxY = ResidentialTownLayout.MapHeight - 1;

        public const int ResidentialEastEdgeX = ResidentialTownLayout.MapWidth - 1;
        public const int MarketWestEdgeX = 0;

        public static readonly Vector3Int MarketArrivalCell = new Vector3Int(1, 15, 0);
        public static readonly Vector3Int ResidentialArrivalCell = new Vector3Int(ResidentialEastEdgeX - 1, 15, 0);

        public static bool IsResidentialEastTransitionCell(Vector3Int cell) =>
            cell.x == ResidentialEastEdgeX && cell.y >= StripMinY && cell.y <= StripMaxY;

        public static bool IsMarketWestTransitionCell(Vector3Int cell) =>
            cell.x == MarketWestEdgeX && cell.y >= StripMinY && cell.y <= StripMaxY;
    }
}
