using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Market General Store — 8×8 exterior on town_market with twin bottom doors; scene-painted shop interior.</summary>
    public static class MarketGeneralStoreLayout
    {
        public const string InteriorFloorId = "town_interior_market_general_store";
        public const string EnterWestLinkId = "building_market_general_store_enter_west";
        public const string ExitWestLinkId = "building_market_general_store_exit_west";
        public const string EnterEastLinkId = "building_market_general_store_enter_east";
        public const string ExitEastLinkId = "building_market_general_store_exit_east";
        public const string NpcMarkerId = "market_general_store_keeper";
        public const string NpcId = "market_general_store_keeper";

        public const int ExteriorWidth = 8;
        public const int ExteriorDepth = 8;

        /// <summary>8×8 footprint centered on the 40×40 market (cells 16–23, 16–23).</summary>
        public const int ExteriorOriginX = (MarketTownLayout.MapSize - ExteriorWidth) / 2;
        public const int ExteriorOriginY = (MarketTownLayout.MapSize - ExteriorDepth) / 2;

        public const int ExteriorWestDoorLocalX = 1;
        public const int ExteriorEastDoorLocalX = 6;

        public static readonly Vector3Int ExteriorWestDoorCell =
            new Vector3Int(ExteriorOriginX + ExteriorWestDoorLocalX, ExteriorOriginY, 0);
        public static readonly Vector3Int ExteriorEastDoorCell =
            new Vector3Int(ExteriorOriginX + ExteriorEastDoorLocalX, ExteriorOriginY, 0);

        public const int InteriorWidth = 8;
        public const int InteriorHeight = 10;

        public const int CounterRowY = 5;
        public const int CustomerRowY = 4;
        public const int CounterMinX = 1;
        public const int CounterMaxX = 6;

        public static readonly Vector3Int InteriorWestExitCell = new Vector3Int(1, 0, 0);
        public static readonly Vector3Int InteriorEastExitCell = new Vector3Int(6, 0, 0);
        public static readonly Vector3Int InteriorWestArrivalCell = new Vector3Int(2, CustomerRowY, 0);
        public static readonly Vector3Int InteriorEastArrivalCell = new Vector3Int(5, CustomerRowY, 0);
        public static readonly Vector3Int ClerkNpcCell = new Vector3Int(4, 6, 0);

        public static bool IsCounterCell(Vector3Int cell) =>
            cell.y == CounterRowY && cell.x >= CounterMinX && cell.x <= CounterMaxX;

        public static bool IsInteriorExitCell(Vector3Int cell) =>
            cell == InteriorWestExitCell || cell == InteriorEastExitCell;

        public static IEnumerable<Vector3Int> EnumerateCounterCells()
        {
            for (int x = CounterMinX; x <= CounterMaxX; x++)
                yield return new Vector3Int(x, CounterRowY, 0);
        }
    }
}
