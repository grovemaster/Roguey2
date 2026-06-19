using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Market blacksmith — 6×5 exterior east of the general store; weapons-and-armor shop interior.</summary>
    public static class MarketBlacksmithLayout
    {
        public const string InteriorFloorId = "town_interior_market_blacksmith";
        public const string EnterLinkId = "building_market_blacksmith_enter";
        public const string ExitLinkId = "building_market_blacksmith_exit";
        public const string NpcMarkerId = "market_blacksmith";
        public const string NpcId = "market_blacksmith";

        public const string ShopDefinitionResourcePath = "Shop/ShopNpc_MarketBlacksmith";
        public const int InitialGold = 800;

        public const int ExteriorWidth = 6;
        public const int ExteriorDepth = 5;

        /// <summary>6×5 footprint immediately east of the general store.</summary>
        public const int ExteriorOriginX = MarketGeneralStoreLayout.ExteriorOriginX + MarketGeneralStoreLayout.ExteriorWidth;
        public const int ExteriorOriginY = MarketGeneralStoreLayout.ExteriorOriginY;

        public const int ExteriorDoorLocalX = 3;

        public static readonly Vector3Int ExteriorDoorCell =
            new Vector3Int(ExteriorOriginX + ExteriorDoorLocalX, ExteriorOriginY, 0);

        public const int InteriorWidth = 8;
        public const int InteriorHeight = 10;

        public const int CounterRowY = 5;
        public const int CustomerRowY = 4;
        public const int CounterMinX = 1;
        public const int CounterMaxX = 6;

        public static readonly Vector3Int InteriorArrivalCell = new Vector3Int(4, CustomerRowY, 0);
        public static readonly Vector3Int InteriorExitCell = new Vector3Int(4, 0, 0);
        public static readonly Vector3Int BlacksmithNpcCell = new Vector3Int(4, 6, 0);

        public static bool IsCounterCell(Vector3Int cell) =>
            cell.y == CounterRowY && cell.x >= CounterMinX && cell.x <= CounterMaxX;

        public static IEnumerable<Vector3Int> EnumerateCounterCells()
        {
            for (int x = CounterMinX; x <= CounterMaxX; x++)
                yield return new Vector3Int(x, CounterRowY, 0);
        }
    }

}
