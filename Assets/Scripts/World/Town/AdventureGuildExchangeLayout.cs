using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Adventure Guild Exchange — exterior on dimension_square, scene-painted interior shop.</summary>
    public static class AdventureGuildExchangeLayout
    {
        public const string InteriorFloorId = "town_interior_adventure_guild_exchange";
        public const string EnterLinkId = "building_adventure_guild_enter";
        public const string ExitLinkId = "building_adventure_guild_exit";
        public const string NpcMarkerId = "adventure_guild_clerk";
        public const string NpcId = "adventure_guild_clerk";

        public const string ShopDefinitionResourcePath = "Shop/ShopNpc_AdventureGuildClerk";
        public const int InitialGold = 9999;

        public const int ExteriorWidth = 5;
        public const int ExteriorDepth = 5;
        public const int ExteriorOriginX = 29;
        public const int ExteriorOriginY = 19;

        public static readonly Vector3Int ExteriorDoorCell = new Vector3Int(31, 19, 0);

        public const int InteriorWidth = 8;
        public const int InteriorHeight = 10;

        /// <summary>Ortho size for district hub and building interiors.</summary>
        public const float DistrictHubCameraOrthoSize = 12f;

        public const int CounterRowY = 5;
        public const int CustomerRowY = 4;
        public const int CounterMinX = 1;
        public const int CounterMaxX = 6;

        /// <summary>Party spawn on the customer row, centered on the counter.</summary>
        public static readonly Vector3Int InteriorArrivalCell = new Vector3Int(4, CustomerRowY, 0);

        public static readonly Vector3Int InteriorExitCell = new Vector3Int(4, 0, 0);
        public static readonly Vector3Int ClerkNpcCell = new Vector3Int(4, 6, 0);

        /// <summary>North-center, one row behind the counter row (<see cref="CounterRowY"/>).</summary>
        public const int ClerkRowY = 6;

        /// <summary>Default customer spot centered on the counter (talk facing north).</summary>
        public static readonly Vector3Int CustomerTalkCell = new Vector3Int(4, CustomerRowY, 0);

        public static bool IsCounterCell(Vector3Int cell) =>
            cell.y == CounterRowY && cell.x >= CounterMinX && cell.x <= CounterMaxX;

        public static IEnumerable<Vector3Int> EnumerateCounterCells()
        {
            for (int x = CounterMinX; x <= CounterMaxX; x++)
                yield return new Vector3Int(x, CounterRowY, 0);
        }
    }
}
