using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Market item shop — 5×4 exterior west of the general store; two-way healing potion merchant interior.</summary>
    public static class MarketItemShopLayout
    {
        public const string InteriorFloorId = "town_interior_market_item_shop";
        public const string EnterLinkId = "building_market_item_shop_enter";
        public const string ExitLinkId = "building_market_item_shop_exit";
        public const string NpcMarkerId = "market_item_shop_clerk";
        public const string NpcId = "market_item_shop_clerk";

        public const string ShopDefinitionResourcePath = "Shop/ShopNpc_MarketItemShopClerk";
        public const int InitialGold = 500;
        public const int InitialHealingPotionStock = 10;
        public const int HealingPotionBuyValue = 2;
        public const int HealingPotionSellValue = 1;

        public const int ExteriorWidth = 5;
        public const int ExteriorDepth = 4;

        /// <summary>5×4 footprint immediately west of the general store (cells 11–15, 16–19).</summary>
        public const int ExteriorOriginX = MarketGeneralStoreLayout.ExteriorOriginX - ExteriorWidth;
        public const int ExteriorOriginY = MarketGeneralStoreLayout.ExteriorOriginY;

        public const int ExteriorDoorLocalX = 2;

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
        public static readonly Vector3Int ClerkNpcCell = new Vector3Int(4, 6, 0);

        public static bool IsCounterCell(Vector3Int cell) =>
            cell.y == CounterRowY && cell.x >= CounterMinX && cell.x <= CounterMaxX;

        public static IEnumerable<Vector3Int> EnumerateCounterCells()
        {
            for (int x = CounterMinX; x <= CounterMaxX; x++)
                yield return new Vector3Int(x, CounterRowY, 0);
        }
    }
}
