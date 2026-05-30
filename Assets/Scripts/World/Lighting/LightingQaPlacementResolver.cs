using JRogue.Manager.Map;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Lighting
{
    /// <summary>Shared placement rules for lighting QA (editor + tests).</summary>
    public static class LightingQaPlacementResolver
    {
        public const int QaAmbientRegionId = 99;
        public const string TieflingAnchorName = "Party_Tiefling_Mage";

        public static Vector3Int GridCellFromWorld(Vector3 world) =>
            new Vector3Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y), 0);

        public static int Manhattan(Vector3Int a, Vector3Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        /// <summary>
        /// Nearest wall on <paramref name="wallMap"/> to <paramref name="anchor"/>;
        /// ties: lowest Y, then lowest X.
        /// </summary>
        public static bool TryFindNearestWallCell(
            Tilemap wallMap,
            Vector3Int anchor,
            out Vector3Int wallCell)
        {
            wallCell = default;
            if (wallMap == null)
                return false;

            int bestDist = int.MaxValue;
            bool found = false;

            BoundsInt bounds = wallMap.cellBounds;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (!wallMap.HasTile(pos))
                    continue;

                Vector3Int cell = Flatten(pos);
                int dist = Manhattan(cell, anchor);
                if (!found || dist < bestDist)
                {
                    bestDist = dist;
                    wallCell = cell;
                    found = true;
                    continue;
                }

                if (dist == bestDist && CompareTieBreak(cell, wallCell) < 0)
                    wallCell = cell;
            }

            return found;
        }

        public static bool TryFindNearestWallCell(
            MapManager map,
            Vector3Int anchor,
            out Vector3Int wallCell) =>
            TryFindNearestWallCell(map != null ? map.WallMap : null, anchor, out wallCell);

        /// <summary>First orthogonally adjacent walkable floor cell to a wall (for bump QA).</summary>
        public static bool TryFindAdjacentFloorCell(
            MapManager map,
            Vector3Int wallCell,
            out Vector3Int floorCell)
        {
            floorCell = default;
            if (map == null)
                return false;

            Vector3Int[] dirs =
            {
                Vector3Int.up,
                Vector3Int.down,
                Vector3Int.left,
                Vector3Int.right
            };

            for (int i = 0; i < dirs.Length; i++)
            {
                Vector3Int candidate = Flatten(wallCell + dirs[i]);
                if (map.IsWalkable(candidate))
                {
                    floorCell = candidate;
                    return true;
                }
            }

            return false;
        }

        public static GameObject FindAnchor(string anchorName = TieflingAnchorName)
        {
            if (string.IsNullOrWhiteSpace(anchorName))
                return null;

            GameObject found = GameObject.Find(anchorName);
            return found;
        }

        static int CompareTieBreak(Vector3Int candidate, Vector3Int current)
        {
            if (candidate.y != current.y)
                return candidate.y.CompareTo(current.y);

            return candidate.x.CompareTo(current.x);
        }

        static Vector3Int Flatten(Vector3Int cell) => new Vector3Int(cell.x, cell.y, 0);
    }
}
