using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.Traps
{
    static class TrapWallTopology
    {
        static readonly Vector3Int[] OrthogonalOffsets =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right,
        };

        public static bool IsCornerWall(Vector3Int wallCell, MapManager map)
        {
            if (map == null || !map.IsWall(wallCell))
                return false;

            bool north = map.IsWalkable(wallCell + Vector3Int.up);
            bool south = map.IsWalkable(wallCell + Vector3Int.down);
            bool east = map.IsWalkable(wallCell + Vector3Int.right);
            bool west = map.IsWalkable(wallCell + Vector3Int.left);

            int floorNeighborCount = (north ? 1 : 0) + (south ? 1 : 0) + (east ? 1 : 0) + (west ? 1 : 0);
            if (floorNeighborCount != 2)
                return false;

            return (north && east) || (north && west) || (south && east) || (south && west);
        }

        public static void CollectTriggerTiles(
            Vector3Int wallHost,
            int triggerRange,
            MapManager map,
            System.Collections.Generic.List<Vector3Int> buffer)
        {
            buffer.Clear();
            if (map == null || triggerRange < 1)
                return;

            for (int i = 0; i < OrthogonalOffsets.Length; i++)
            {
                Vector3Int floorCell = wallHost + OrthogonalOffsets[i];
                if (!map.IsWalkable(floorCell))
                    continue;

                int distance = Mathf.Abs(floorCell.x - wallHost.x) + Mathf.Abs(floorCell.y - wallHost.y);
                if (distance <= triggerRange)
                    buffer.Add(floorCell);
            }
        }
    }
}
