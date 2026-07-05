using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Adventurer's Guild Hall — west facade on dimension_square + scene-painted interior.</summary>
    public static class AdventureGuildHallLayout
    {
        public const string InteriorFloorId = "town_interior_adventure_guild_hall";
        public const string EnterLinkId = "building_adventure_guild_hall_enter";
        public const string ExitLinkId = "building_adventure_guild_hall_exit";
        public const string NpcMarkerId = "adventure_guild_secretary";
        public const string NpcId = "adventure_guild_secretary";

        public const int ExteriorWidth = 5;
        public const int ExteriorDepth = 5;
        public const int ExteriorOriginX = 7;
        public const int ExteriorOriginY = 19;

        public static readonly Vector3Int ExteriorDoorCell = new Vector3Int(9, 19, 0);

        public const int InteriorWidth = 8;
        public const int InteriorHeight = 10;

        public const int CounterRowY = 5;
        public const int CustomerRowY = 4;
        public const int CounterMinX = 1;
        public const int CounterMaxX = 6;

        public static readonly Vector3Int InteriorArrivalCell = new Vector3Int(4, CustomerRowY, 0);
        public static readonly Vector3Int InteriorExitCell = new Vector3Int(4, 0, 0);
        public static readonly Vector3Int SecretaryNpcCell = new Vector3Int(4, 6, 0);

        public static bool IsCounterCell(Vector3Int cell) =>
            cell.y == CounterRowY && cell.x >= CounterMinX && cell.x <= CounterMaxX;

        public static IEnumerable<Vector3Int> EnumerateCounterCells()
        {
            for (int x = CounterMinX; x <= CounterMaxX; x++)
                yield return new Vector3Int(x, CounterRowY, 0);
        }
    }
}
