using System.Collections.Generic;
using JRogue.Manager.Map;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Phases
{
    internal static class PopulationPlacementUtility
    {
        public static List<Vector3Int> CollectFloorCandidates(
            MapManager map,
            DungeonGenerationContext context,
            bool excludeReserved = true)
        {
            var candidates = new List<Vector3Int>();
            DungeonLayoutStamp stamp = context.Definition?.LayoutStamp;
            if (stamp == null || map == null)
                return candidates;

            for (int y = 0; y < stamp.Height; y++)
            {
                for (int x = 0; x < stamp.Width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!IsPopulationCell(map, stamp, cell, context, excludeReserved))
                        continue;

                    candidates.Add(cell);
                }
            }

            return candidates;
        }

        public static bool IsPopulationCell(
            MapManager map,
            DungeonLayoutStamp stamp,
            Vector3Int cell,
            DungeonGenerationContext context,
            bool excludeReserved = true)
        {
            if (stamp == null || map == null)
                return false;

            if (cell.x < 0 || cell.y < 0 || cell.x >= stamp.Width || cell.y >= stamp.Height)
                return false;

            if (!stamp.IsFloor(cell.x, cell.y))
                return false;

            Tilemap floor = map.FloorMap;
            if (floor == null || !floor.HasTile(cell))
                return false;

            if (!map.IsWalkable(cell))
                return false;

            if (context != null)
            {
                if (context.IsInSafeZone(cell))
                    return false;

                if (excludeReserved && context.ReservedCells.Contains(cell))
                    return false;
            }

            return true;
        }

        public static void Shuffle(List<Vector3Int> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swap = rng.Next(i + 1);
                (list[i], list[swap]) = (list[swap], list[i]);
            }
        }
    }
}
