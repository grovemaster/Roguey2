using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.Service.Sensing
{
    /// <summary>
    /// Stateless helper for acoustic propagation math.
    /// Volume falls off by 1 per tile of Chebyshev distance and by
    /// <see cref="WallPenalty"/> per wall tile crossed along the line of travel.
    /// </summary>
    public static class HearingUtility
    {
        public const int WallPenalty = 5;

        public static int CalculateEffectiveVolume(
            Vector3Int origin,
            Vector3Int listener,
            int volume,
            MapManager mapManager,
            int wallPenalty = WallPenalty)
        {
            int distance = ChebyshevDistance(origin, listener);
            int walls = mapManager != null
                ? CountWallsBetween(origin, listener, mapManager)
                : 0;
            return volume - distance - (walls * wallPenalty);
        }

        public static int ChebyshevDistance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        // Bresenham line trace from origin -> target, counting wall tiles strictly
        // between the endpoints. Origin and target tiles are never counted.
        public static int CountWallsBetween(Vector3Int origin, Vector3Int target, MapManager mapManager)
        {
            if (mapManager == null) return 0;
            if (origin.x == target.x && origin.y == target.y) return 0;

            int x = origin.x;
            int y = origin.y;
            int x1 = target.x;
            int y1 = target.y;

            int dx = Mathf.Abs(x1 - x);
            int dy = Mathf.Abs(y1 - y);
            int sx = x < x1 ? 1 : -1;
            int sy = y < y1 ? 1 : -1;
            int err = dx - dy;

            int walls = 0;
            while (true)
            {
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }

                if (x == x1 && y == y1) break;

                if (mapManager.IsWall(new Vector3Int(x, y, 0)))
                {
                    walls++;
                }
            }
            return walls;
        }
    }
}
