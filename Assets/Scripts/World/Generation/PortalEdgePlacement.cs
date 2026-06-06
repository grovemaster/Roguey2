using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.World.Generation
{
    public enum MapEdge
    {
        South,
        North,
        East,
        West,
    }

    public struct ResolvedEdgePortal
    {
        public Vector3Int cell;
        public MapEdge edge;
    }

    public static class PortalEdgePlacement
    {
        public static bool TryFindEdgePortalCell(
            DungeonLayoutStamp stamp,
            MapManager map,
            MapEdge edge,
            int inset,
            out Vector3Int cell)
        {
            cell = default;
            if (stamp == null || map == null)
                return false;

            return TryFindEdgePortalCell(stamp.Width, stamp.Height, stamp, map, edge, inset, out cell);
        }

        public static bool TryFindEdgePortalCell(
            int mapWidth,
            int mapHeight,
            MapManager map,
            MapEdge edge,
            int inset,
            out Vector3Int cell)
        {
            return TryFindEdgePortalCell(mapWidth, mapHeight, null, map, edge, inset, out cell);
        }

        static bool TryFindEdgePortalCell(
            int mapWidth,
            int mapHeight,
            DungeonLayoutStamp stamp,
            MapManager map,
            MapEdge edge,
            int inset,
            out Vector3Int cell)
        {
            cell = default;
            if (map == null || mapWidth <= 0 || mapHeight <= 0)
                return false;

            inset = Mathf.Max(1, inset);

            return edge switch
            {
                MapEdge.South => TryScanRow(stamp, map, mapWidth, inset, out cell),
                MapEdge.North => TryScanRow(stamp, map, mapWidth, mapHeight - 1 - inset, out cell),
                MapEdge.West => TryScanColumn(stamp, map, mapHeight, inset, out cell),
                MapEdge.East => TryScanColumn(stamp, map, mapHeight, mapWidth - 1 - inset, out cell),
                _ => false,
            };
        }

        static bool TryScanRow(
            DungeonLayoutStamp stamp,
            MapManager map,
            int mapWidth,
            int y,
            out Vector3Int cell)
        {
            cell = default;
            if (y < 0)
                return false;

            for (int x = 0; x < mapWidth; x++)
            {
                cell = new Vector3Int(x, y, 0);
                if (IsWalkablePortalCell(stamp, map, x, y, cell))
                    return true;
            }

            cell = default;
            return false;
        }

        static bool TryScanColumn(
            DungeonLayoutStamp stamp,
            MapManager map,
            int mapHeight,
            int x,
            out Vector3Int cell)
        {
            cell = default;
            if (x < 0)
                return false;

            for (int y = 0; y < mapHeight; y++)
            {
                cell = new Vector3Int(x, y, 0);
                if (IsWalkablePortalCell(stamp, map, x, y, cell))
                    return true;
            }

            cell = default;
            return false;
        }

        static bool IsWalkablePortalCell(
            DungeonLayoutStamp stamp,
            MapManager map,
            int x,
            int y,
            Vector3Int cell)
        {
            if (stamp != null && !stamp.IsFloor(x, y))
                return false;

            return map.IsWalkable(cell);
        }
    }
}
