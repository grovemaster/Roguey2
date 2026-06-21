using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// Snaps party spawn to a walkable floor cell after zone geometry is painted.
    /// </summary>
    internal static class PlayerSpawnAnchorResolver
    {
        const int MaxFormationMembers = 6;
        const int NearestSearchRadius = 24;

        public static bool TryResolve(DungeonGenerationContext context, MapManager map)
        {
            if (context == null || map == null || context.Definition == null)
                return false;

            if (!context.UsesPaintedZoneMap)
                return map.IsWalkable(context.PlayerStart);

            string spawnZoneId = ResolveSpawnZoneId(context);
            PartyFormationSpawnProfile profile = context.Definition.FormationProfile;
            if (!TryGetMaxFormationOffsets(profile, out Vector3Int[] formationOffsets))
                formationOffsets = new[] { Vector3Int.zero };

            List<Vector3Int> candidates = CollectSpawnCandidates(map, context, spawnZoneId);
            if (candidates.Count == 0)
                candidates = CollectSpawnCandidates(map, context, zoneId: null);

            System.Random rng = ZoneGenerationRng.CreatePopulationRng(
                context.RunSeed,
                context.Definition.FloorId + "_playerStart");
            PopulationPlacementUtility.Shuffle(candidates, rng);

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3Int candidate = candidates[i];
                if (!FormationFitsAtAnchor(candidate, formationOffsets, map, context, spawnZoneId))
                    continue;

                context.PlayerStart = candidate;
                RebuildSafeZone(context);
                return true;
            }

            if (TryFindNearestFormationAnchor(
                    context.PlayerStart,
                    formationOffsets,
                    map,
                    context,
                    spawnZoneId,
                    out Vector3Int nearest))
            {
                context.PlayerStart = nearest;
                RebuildSafeZone(context);
                return true;
            }

            DungeonGenerationLog.Warn(
                $"{nameof(PlayerSpawnAnchorResolver)}: no walkable formation anchor " +
                $"(zone={spawnZoneId ?? "any"}, candidates={candidates.Count}).");
            return false;
        }

        public static bool FormationFitsAtAnchor(
            Vector3Int anchor,
            Vector3Int[] offsets,
            MapManager map,
            DungeonGenerationContext context,
            string requiredZoneId)
        {
            if (map == null || offsets == null || offsets.Length == 0)
                return false;

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3Int cell = anchor + offsets[i];
                if (!IsSpawnCell(map, context, cell, requiredZoneId))
                    return false;
            }

            return true;
        }

        static bool TryFindNearestFormationAnchor(
            Vector3Int origin,
            Vector3Int[] formationOffsets,
            MapManager map,
            DungeonGenerationContext context,
            string requiredZoneId,
            out Vector3Int anchor)
        {
            anchor = origin;
            if (!PopulationPlacementUtility.TryGetMapBounds(context, out int width, out int height))
                return false;

            var visited = new HashSet<Vector3Int>();
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(origin);
            visited.Add(origin);

            while (queue.Count > 0)
            {
                Vector3Int candidate = queue.Dequeue();
                if (FormationFitsAtAnchor(candidate, formationOffsets, map, context, requiredZoneId))
                {
                    anchor = candidate;
                    return true;
                }

                if (ManhattanDistance(origin, candidate) >= NearestSearchRadius)
                    continue;

                EnqueueNeighbor(candidate + Vector3Int.up, width, height, visited, queue);
                EnqueueNeighbor(candidate + Vector3Int.down, width, height, visited, queue);
                EnqueueNeighbor(candidate + Vector3Int.right, width, height, visited, queue);
                EnqueueNeighbor(candidate + Vector3Int.left, width, height, visited, queue);
            }

            return false;
        }

        static void EnqueueNeighbor(
            Vector3Int cell,
            int width,
            int height,
            HashSet<Vector3Int> visited,
            Queue<Vector3Int> queue)
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
                return;

            if (!visited.Add(cell))
                return;

            queue.Enqueue(cell);
        }

        static List<Vector3Int> CollectSpawnCandidates(
            MapManager map,
            DungeonGenerationContext context,
            string zoneId)
        {
            var candidates = new List<Vector3Int>();
            if (map == null || context == null)
                return candidates;

            if (!PopulationPlacementUtility.TryGetMapBounds(context, out int width, out int height))
                return candidates;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!IsSpawnCell(map, context, cell, zoneId))
                        continue;

                    candidates.Add(cell);
                }
            }

            return candidates;
        }

        static bool IsSpawnCell(
            MapManager map,
            DungeonGenerationContext context,
            Vector3Int cell,
            string requiredZoneId)
        {
            if (map.FloorMap == null || !map.FloorMap.HasTile(cell) || !map.IsWalkable(cell))
                return false;

            if (context.ReservedCells.Contains(cell))
                return false;

            if (!context.TryGetZoneId(cell, out string zoneId)
                || zoneId == ZoneIds.Empty
                || zoneId == ZoneIds.Rock)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(requiredZoneId) && zoneId != requiredZoneId)
                return false;

            return true;
        }

        static string ResolveSpawnZoneId(DungeonGenerationContext context)
        {
            ResolvedZonePiece[] pieces = context.ResolvedZonePieces;
            if (pieces == null)
                return null;

            for (int i = 0; i < pieces.Length; i++)
            {
                ResolvedZonePiece piece = pieces[i];
                if (piece.IsPlayerStartPiece && !string.IsNullOrEmpty(piece.ZoneId))
                    return piece.ZoneId;
            }

            return null;
        }

        static bool TryGetMaxFormationOffsets(
            PartyFormationSpawnProfile profile,
            out Vector3Int[] offsets)
        {
            offsets = null;
            int memberCount = ResolveMaxMemberCount(profile);
            if (profile != null && profile.TryGetOffsetsForCount(memberCount, out offsets))
                return offsets != null && offsets.Length > 0;

            offsets = new[] { Vector3Int.zero };
            return true;
        }

        static int ResolveMaxMemberCount(PartyFormationSpawnProfile profile) =>
            profile != null ? profile.GetMaxMemberCount() : 1;

        static void RebuildSafeZone(DungeonGenerationContext context)
        {
            PartyFormationSpawnProfile profile = context.Definition?.FormationProfile;
            if (profile != null
                && TryGetMaxFormationOffsets(profile, out Vector3Int[] offsets)
                && offsets != null
                && offsets.Length > 0)
            {
                var formationCells = new List<Vector3Int>(offsets.Length);
                for (int i = 0; i < offsets.Length; i++)
                    formationCells.Add(context.PlayerStart + offsets[i]);

                context.BuildSafeZone(formationCells, context.Definition.PlayerSafeRadius);
                return;
            }

            context.BuildSafeZoneForFloor(context.Definition);
        }

        static int ManhattanDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
