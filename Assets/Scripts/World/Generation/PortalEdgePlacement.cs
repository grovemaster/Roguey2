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

            inset = Mathf.Max(1, inset);

            return edge switch
            {
                MapEdge.South => TryScanRow(stamp, map, inset, out cell),
                MapEdge.North => TryScanRow(stamp, map, stamp.Height - 1 - inset, out cell),
                MapEdge.West => TryScanColumn(stamp, map, inset, out cell),
                MapEdge.East => TryScanColumn(stamp, map, stamp.Width - 1 - inset, out cell),
                _ => false,
            };
        }

        static bool TryScanRow(DungeonLayoutStamp stamp, MapManager map, int y, out Vector3Int cell)
        {
            cell = default;
            if (y < 0 || y >= stamp.Height)
                return false;

            for (int x = 0; x < stamp.Width; x++)
            {
                cell = new Vector3Int(x, y, 0);
                if (stamp.IsFloor(x, y) && map.IsWalkable(cell))
                    return true;
            }

            cell = default;
            return false;
        }

        static bool TryScanColumn(DungeonLayoutStamp stamp, MapManager map, int x, out Vector3Int cell)
        {
            cell = default;
            if (x < 0 || x >= stamp.Width)
                return false;

            for (int y = 0; y < stamp.Height; y++)
            {
                cell = new Vector3Int(x, y, 0);
                if (stamp.IsFloor(x, y) && map.IsWalkable(cell))
                    return true;
            }

            cell = default;
            return false;
        }
    }
}
