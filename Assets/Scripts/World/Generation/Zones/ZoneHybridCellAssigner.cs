using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneHybridCellAssigner
    {
        public static Dictionary<Vector3Int, string> Assign(
            DungeonLayoutStamp skeleton,
            int floorWidth,
            int floorHeight,
            string fallbackZoneId,
            IReadOnlyList<ResolvedZonePiece> seedPieces,
            out ResolvedZonePiece[] rebuiltPieces)
        {
            rebuiltPieces = System.Array.Empty<ResolvedZonePiece>();
            string fallback = string.IsNullOrEmpty(fallbackZoneId) ? ZoneIds.Rock : fallbackZoneId;
            var zoneMap = ZoneCellMapBuilder.Build(floorWidth, floorHeight, fallback, System.Array.Empty<ResolvedZonePiece>());
            if (skeleton == null || seedPieces == null || seedPieces.Count == 0)
                return zoneMap;

            var walkable = CollectSkeletonWalkable(skeleton, floorWidth, floorHeight);
            if (walkable.Count == 0)
                return zoneMap;

            var pieceById = new Dictionary<string, ResolvedZonePiece>();
            for (int i = 0; i < seedPieces.Count; i++)
                pieceById[seedPieces[i].PieceId] = seedPieces[i];

            var assignedPieceByCell = new Dictionary<Vector3Int, string>();
            var queue = new Queue<(Vector3Int cell, string pieceId)>();

            for (int i = 0; i < seedPieces.Count; i++)
            {
                ResolvedZonePiece seed = seedPieces[i];
                if (seed.ZoneId == ZoneIds.Empty)
                    continue;

                if (!TryFindSeedCell(seed, walkable, out Vector3Int seedCell))
                    continue;

                assignedPieceByCell[seedCell] = seed.PieceId;
                queue.Enqueue((seedCell, seed.PieceId));
            }

            while (queue.Count > 0)
            {
                (Vector3Int cell, string pieceId) = queue.Dequeue();
                if (!pieceById.TryGetValue(pieceId, out ResolvedZonePiece piece))
                    continue;

                zoneMap[cell] = piece.ZoneId;
                TryEnqueueNeighbor(cell + Vector3Int.up, pieceId, walkable, assignedPieceByCell, queue);
                TryEnqueueNeighbor(cell + Vector3Int.down, pieceId, walkable, assignedPieceByCell, queue);
                TryEnqueueNeighbor(cell + Vector3Int.right, pieceId, walkable, assignedPieceByCell, queue);
                TryEnqueueNeighbor(cell + Vector3Int.left, pieceId, walkable, assignedPieceByCell, queue);
            }

            foreach (Vector3Int cell in walkable)
            {
                if (assignedPieceByCell.ContainsKey(cell))
                    continue;

                zoneMap[cell] = fallback;
            }

            rebuiltPieces = RebuildPieces(seedPieces, assignedPieceByCell, pieceById);
            return zoneMap;
        }

        static HashSet<Vector3Int> CollectSkeletonWalkable(
            DungeonLayoutStamp skeleton,
            int floorWidth,
            int floorHeight)
        {
            var walkable = new HashSet<Vector3Int>();
            int width = Mathf.Min(skeleton.Width, floorWidth);
            int height = Mathf.Min(skeleton.Height, floorHeight);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!skeleton.IsFloor(x, y) || skeleton.IsWall(x, y))
                        continue;

                    walkable.Add(new Vector3Int(x, y, 0));
                }
            }

            return walkable;
        }

        static bool TryFindSeedCell(
            ResolvedZonePiece seed,
            HashSet<Vector3Int> walkable,
            out Vector3Int seedCell)
        {
            seedCell = ZoneCompassRectResolver.ResolvePlayerStart(seed.Bounds);
            if (walkable.Contains(seedCell))
                return true;

            RectInt bounds = seed.Bounds;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (walkable.Contains(cell))
                    {
                        seedCell = cell;
                        return true;
                    }
                }
            }

            return false;
        }

        static void TryEnqueueNeighbor(
            Vector3Int cell,
            string pieceId,
            HashSet<Vector3Int> walkable,
            Dictionary<Vector3Int, string> assignedPieceByCell,
            Queue<(Vector3Int cell, string pieceId)> queue)
        {
            if (!walkable.Contains(cell) || assignedPieceByCell.ContainsKey(cell))
                return;

            assignedPieceByCell[cell] = pieceId;
            queue.Enqueue((cell, pieceId));
        }

        static ResolvedZonePiece[] RebuildPieces(
            IReadOnlyList<ResolvedZonePiece> seedPieces,
            Dictionary<Vector3Int, string> assignedPieceByCell,
            Dictionary<string, ResolvedZonePiece> pieceById)
        {
            var boundsByPiece = new Dictionary<string, RectInt>();
            foreach (KeyValuePair<Vector3Int, string> entry in assignedPieceByCell)
            {
                if (!pieceById.TryGetValue(entry.Value, out ResolvedZonePiece piece))
                    continue;

                if (!boundsByPiece.TryGetValue(piece.PieceId, out RectInt bounds))
                {
                    bounds = new RectInt(entry.Key.x, entry.Key.y, 1, 1);
                }
                else
                {
                    int xMin = Mathf.Min(bounds.xMin, entry.Key.x);
                    int yMin = Mathf.Min(bounds.yMin, entry.Key.y);
                    int xMax = Mathf.Max(bounds.xMax, entry.Key.x + 1);
                    int yMax = Mathf.Max(bounds.yMax, entry.Key.y + 1);
                    bounds = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
                }

                boundsByPiece[piece.PieceId] = bounds;
            }

            var rebuilt = new ResolvedZonePiece[seedPieces.Count];
            for (int i = 0; i < seedPieces.Count; i++)
            {
                ResolvedZonePiece seed = seedPieces[i];
                RectInt bounds = boundsByPiece.TryGetValue(seed.PieceId, out RectInt computed)
                    ? computed
                    : seed.Bounds;
                rebuilt[i] = new ResolvedZonePiece(
                    seed.PieceId,
                    seed.ZoneId,
                    bounds,
                    seed.IsPlayerStartPiece);
            }

            return rebuilt;
        }
    }
}
