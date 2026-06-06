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
            if (map == null || context?.Definition == null)
                return candidates;

            if (!TryGetMapBounds(context, out int width, out int height))
                return candidates;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!IsPopulationCell(map, context, cell, excludeReserved))
                        continue;

                    candidates.Add(cell);
                }
            }

            return candidates;
        }

        public static List<Vector3Int> CollectZoneCandidates(
            MapManager map,
            DungeonGenerationContext context,
            string zoneId,
            bool excludeReserved = true)
        {
            var candidates = new List<Vector3Int>();
            if (map == null || context == null || string.IsNullOrEmpty(zoneId))
                return candidates;

            List<Vector3Int> floorCandidates = CollectFloorCandidates(map, context, excludeReserved);
            for (int i = 0; i < floorCandidates.Count; i++)
            {
                Vector3Int cell = floorCandidates[i];
                if (!context.TryGetZoneId(cell, out string cellZoneId))
                    continue;

                if (cellZoneId != zoneId)
                    continue;

                candidates.Add(cell);
            }

            return candidates;
        }

        public static bool TryGetMapBounds(DungeonGenerationContext context, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (context?.Definition == null)
                return false;

            if (context.UsesZoneComposite && context.Definition.ZoneLayout != null)
            {
                width = context.MapWidth > 0
                    ? context.MapWidth
                    : context.Definition.ZoneLayout.FloorWidth;
                height = context.MapHeight > 0
                    ? context.MapHeight
                    : context.Definition.ZoneLayout.FloorHeight;
                return width > 0 && height > 0;
            }

            DungeonLayoutStamp stamp = context.Definition.LayoutStamp;
            if (stamp == null)
                return false;

            width = stamp.Width;
            height = stamp.Height;
            return width > 0 && height > 0;
        }

        public static bool IsPopulationCell(
            MapManager map,
            DungeonGenerationContext context,
            Vector3Int cell,
            bool excludeReserved = true)
        {
            if (map == null || context?.Definition == null)
                return false;

            if (!TryGetMapBounds(context, out int width, out int height))
                return false;

            if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
                return false;

            if (!context.UsesZoneComposite)
            {
                DungeonLayoutStamp stamp = context.Definition.LayoutStamp;
                if (stamp == null || !stamp.IsFloor(cell.x, cell.y))
                    return false;
            }

            Tilemap floor = map.FloorMap;
            if (floor == null || !floor.HasTile(cell))
                return false;

            if (!map.IsWalkable(cell))
                return false;

            if (context.IsInSafeZone(cell))
                return false;

            if (excludeReserved && context.ReservedCells.Contains(cell))
                return false;

            return true;
        }

        public static bool IsPopulationCell(
            MapManager map,
            DungeonLayoutStamp stamp,
            Vector3Int cell,
            DungeonGenerationContext context,
            bool excludeReserved = true)
        {
            if (context != null && context.UsesZoneComposite)
                return IsPopulationCell(map, context, cell, excludeReserved);

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
