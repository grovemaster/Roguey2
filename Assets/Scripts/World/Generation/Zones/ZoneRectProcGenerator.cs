using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneRectProcGenerator
    {
        public static bool[,] GenerateRoomCorridor(RectInt bounds, System.Random rng, bool ensureConnectivity)
        {
            int width = bounds.width;
            int height = bounds.height;
            if (width <= 2 || height <= 2 || rng == null)
                return null;

            var floor = new bool[width, height];
            var rooms = new List<RectInt>();
            int roomCount = Mathf.Clamp((width * height) / 80, 2, 8);

            for (int attempt = 0; attempt < roomCount * 12 && rooms.Count < roomCount; attempt++)
            {
                int roomWidth = rng.Next(3, Mathf.Min(9, width - 1));
                int roomHeight = rng.Next(3, Mathf.Min(9, height - 1));
                int x = rng.Next(1, width - roomWidth - 1);
                int y = rng.Next(1, height - roomHeight - 1);
                var room = new RectInt(x, y, roomWidth, roomHeight);

                if (OverlapsAny(room, rooms, padding: 1))
                    continue;

                rooms.Add(room);
                CarveRect(floor, room);
            }

            if (rooms.Count == 0)
            {
                var fallback = new RectInt(1, 1, width - 2, height - 2);
                CarveRect(floor, fallback);
                rooms.Add(fallback);
            }

            for (int i = 1; i < rooms.Count; i++)
            {
                Vector2Int from = RoomCenter(rooms[i - 1]);
                Vector2Int to = RoomCenter(rooms[i]);
                CarveCorridor(floor, from, to);
            }

            if (ensureConnectivity)
                KeepLargestFloorComponent(floor);

            return floor;
        }

        public static bool[,] GenerateCave(RectInt bounds, System.Random rng, int wallDensity, bool ensureConnectivity)
        {
            int width = bounds.width;
            int height = bounds.height;
            if (width <= 2 || height <= 2 || rng == null)
                return null;

            var floor = new bool[width, height];
            int fillProbability = Mathf.Clamp(100 - wallDensity, 25, 75);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        continue;

                    floor[x, y] = rng.Next(100) < fillProbability;
                }
            }

            for (int iteration = 0; iteration < 5; iteration++)
                floor = SmoothCave(floor);

            ForceBorderWalls(floor);

            if (ensureConnectivity)
                KeepLargestFloorComponent(floor);

            return floor;
        }

        static bool[,] SmoothCave(bool[,] floor)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            var next = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int wallNeighbors = CountWallNeighbors(floor, x, y);
                    next[x, y] = wallNeighbors >= 5;
                }
            }

            return next;
        }

        static int CountWallNeighbors(bool[,] floor, int x, int y)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            int walls = 0;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height || !floor[nx, ny])
                        walls++;
                }
            }

            return walls;
        }

        static void ForceBorderWalls(bool[,] floor)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                floor[x, 0] = false;
                floor[x, height - 1] = false;
            }

            for (int y = 0; y < height; y++)
            {
                floor[0, y] = false;
                floor[width - 1, y] = false;
            }
        }

        static void KeepLargestFloorComponent(bool[,] floor)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            var visited = new bool[width, height];
            var best = new List<Vector2Int>();
            var current = new List<Vector2Int>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!floor[x, y] || visited[x, y])
                        continue;

                    current.Clear();
                    FloodFloor(floor, visited, x, y, current);
                    if (current.Count > best.Count)
                    {
                        best.Clear();
                        best.AddRange(current);
                    }
                }
            }

            if (best.Count == 0)
                return;

            var keep = new HashSet<Vector2Int>(best);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (floor[x, y] && !keep.Contains(new Vector2Int(x, y)))
                        floor[x, y] = false;
                }
            }
        }

        static void FloodFloor(
            bool[,] floor,
            bool[,] visited,
            int startX,
            int startY,
            List<Vector2Int> cells)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(startX, startY));

            while (stack.Count > 0)
            {
                Vector2Int cell = stack.Pop();
                if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
                    continue;

                if (visited[cell.x, cell.y] || !floor[cell.x, cell.y])
                    continue;

                visited[cell.x, cell.y] = true;
                cells.Add(cell);
                stack.Push(cell + Vector2Int.up);
                stack.Push(cell + Vector2Int.down);
                stack.Push(cell + Vector2Int.left);
                stack.Push(cell + Vector2Int.right);
            }
        }

        static void CarveCorridor(bool[,] floor, Vector2Int from, Vector2Int to)
        {
            int x = from.x;
            int y = from.y;
            while (x != to.x)
            {
                floor[x, y] = true;
                x += x < to.x ? 1 : -1;
            }

            while (y != to.y)
            {
                floor[x, y] = true;
                y += y < to.y ? 1 : -1;
            }

            floor[x, y] = true;
        }

        static void CarveRect(bool[,] floor, RectInt rect)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++)
                    floor[x, y] = true;
            }
        }

        static Vector2Int RoomCenter(RectInt room) =>
            new Vector2Int((room.xMin + room.xMax) / 2, (room.yMin + room.yMax) / 2);

        static bool OverlapsAny(RectInt candidate, List<RectInt> rooms, int padding)
        {
            var expanded = new RectInt(
                candidate.xMin - padding,
                candidate.yMin - padding,
                candidate.width + padding * 2,
                candidate.height + padding * 2);

            for (int i = 0; i < rooms.Count; i++)
            {
                if (ZoneCompassRectResolver.RectsOverlap(expanded, rooms[i]))
                    return true;
            }

            return false;
        }
    }
}
