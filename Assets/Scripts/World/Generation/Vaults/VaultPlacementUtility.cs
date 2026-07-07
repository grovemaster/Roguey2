using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Vaults
{
    internal static class VaultPlacementUtility
    {
        /// <summary>
        /// Footprint cells must stay inside the map interior so perimeter walls are never overwritten.
        /// </summary>
        internal const int MapPerimeterInset = 1;

        public static int ChebyshevDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        public static bool MeetsMinDistanceFromPlayerStart(
            VaultBlueprint blueprint,
            Vector3Int placementOrigin,
            Vector3Int playerStart,
            int minDistance)
        {
            if (minDistance <= 0)
                return true;

            foreach (VaultMapCell cell in blueprint.OccupiedCells())
            {
                Vector3Int world = blueprint.LocalToWorld(placementOrigin, cell.X, cell.Y);
                if (ChebyshevDistance(world, playerStart) < minDistance)
                    return false;
            }

            return true;
        }

        public static bool CanPlaceAt(
            VaultBlueprint blueprint,
            Vector3Int placementOrigin,
            DungeonGenerationContext context,
            MapManager map,
            int minDistanceFromPlayerStart,
            string requiredZoneId = null,
            bool requireReachableFromPlayerStart = false)
        {
            if (blueprint == null || context == null || map == null)
                return false;

            if (!PopulationPlacementUtility.TryGetMapBounds(context, out int mapWidth, out int mapHeight))
                return false;

            if (!MeetsMinDistanceFromPlayerStart(
                    blueprint,
                    placementOrigin,
                    context.PlayerStart,
                    minDistanceFromPlayerStart))
                return false;

            Vector3Int anchorWorld = blueprint.LocalToWorld(
                placementOrigin,
                blueprint.Origin.x,
                blueprint.Origin.y);

            if (!IsWithinInteriorBounds(anchorWorld, mapWidth, mapHeight))
                return false;

            if (!IsReplaceableCell(context, map, anchorWorld))
                return false;

            foreach (VaultMapCell cell in blueprint.OccupiedCells())
            {
                Vector3Int world = blueprint.LocalToWorld(placementOrigin, cell.X, cell.Y);

                if (!IsWithinInteriorBounds(world, mapWidth, mapHeight))
                    return false;

                if (!IsReplaceableCell(context, map, world))
                    return false;

                if (context.IsInSafeZone(world))
                    return false;

                if (context.ReservedCells.Contains(world))
                    return false;

                if (!MeetsRequiredZone(context, world, requiredZoneId))
                    return false;
            }

            if (requireReachableFromPlayerStart
                && !IsFootprintReachableFromPlayerStart(blueprint, placementOrigin, context, map))
            {
                return false;
            }

            return true;
        }

        internal static bool IsWithinInteriorBounds(
            Vector3Int world,
            int mapWidth,
            int mapHeight,
            int inset = MapPerimeterInset)
        {
            if (inset <= 0)
                return world.x >= 0 && world.y >= 0 && world.x < mapWidth && world.y < mapHeight;

            return world.x >= inset
                && world.y >= inset
                && world.x < mapWidth - inset
                && world.y < mapHeight - inset;
        }

        /// <summary>
        /// True when every walkable floor cell in the vault footprint lies in the same
        /// walkable component as <see cref="DungeonGenerationContext.PlayerStart"/>.
        /// </summary>
        public static bool IsFootprintReachableFromPlayerStart(
            VaultBlueprint blueprint,
            Vector3Int placementOrigin,
            DungeonGenerationContext context,
            MapManager map)
        {
            if (blueprint == null || context == null || map == null)
                return false;

            HashSet<Vector3Int> reachable = CollectReachableWalkableCells(context, map);
            if (reachable.Count == 0)
                return false;

            foreach (VaultMapCell cell in blueprint.OccupiedCells())
            {
                if (cell.Kind != VaultCellKind.Floor)
                    continue;

                Vector3Int world = blueprint.LocalToWorld(placementOrigin, cell.X, cell.Y);
                if (!reachable.Contains(world))
                    return false;
            }

            return true;
        }

        public static HashSet<Vector3Int> CollectReachableWalkableCells(
            DungeonGenerationContext context,
            MapManager map)
        {
            var reachable = new HashSet<Vector3Int>();
            if (context == null || map == null || !map.IsWalkable(context.PlayerStart))
                return reachable;

            var queue = new Queue<Vector3Int>();
            queue.Enqueue(context.PlayerStart);
            reachable.Add(context.PlayerStart);

            while (queue.Count > 0)
            {
                Vector3Int cell = queue.Dequeue();
                TryEnqueueWalkable(map, cell + Vector3Int.up, reachable, queue);
                TryEnqueueWalkable(map, cell + Vector3Int.down, reachable, queue);
                TryEnqueueWalkable(map, cell + Vector3Int.right, reachable, queue);
                TryEnqueueWalkable(map, cell + Vector3Int.left, reachable, queue);
            }

            return reachable;
        }

        static void TryEnqueueWalkable(
            MapManager map,
            Vector3Int cell,
            HashSet<Vector3Int> visited,
            Queue<Vector3Int> queue)
        {
            if (!visited.Add(cell) || !map.IsWalkable(cell))
                return;

            queue.Enqueue(cell);
        }

        static bool MeetsRequiredZone(DungeonGenerationContext context, Vector3Int world, string requiredZoneId)
        {
            if (string.IsNullOrEmpty(requiredZoneId))
                return true;

            if (!context.TryGetZoneId(world, out string zoneId))
                return false;

            return zoneId == requiredZoneId;
        }

        static bool IsReplaceableCell(DungeonGenerationContext context, MapManager map, Vector3Int world)
        {
            if (context.UsesPaintedZoneMap)
                return map.IsWalkable(world) || map.IsWall(world);

            DungeonLayoutStamp stamp = context.Definition?.LayoutStamp;
            if (stamp == null)
                return false;

            return stamp.IsFloor(world.x, world.y) || stamp.IsWall(world.x, world.y);
        }

        public static List<Vector3Int> CollectOriginCandidates(
            DungeonLayoutStamp stamp,
            DungeonGenerationContext context)
        {
            if (context != null && context.UsesPaintedZoneMap)
            {
                MapManager map = MapManager.Instance;
                return map != null
                    ? PopulationPlacementUtility.CollectFloorCandidates(map, context)
                    : new List<Vector3Int>();
            }

            var candidates = new List<Vector3Int>();
            if (stamp == null)
                return candidates;

            for (int y = 0; y < stamp.Height; y++)
            {
                for (int x = 0; x < stamp.Width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!stamp.IsFloor(x, y))
                        continue;

                    if (context != null && context.IsInSafeZone(cell))
                        continue;

                    candidates.Add(cell);
                }
            }

            return candidates;
        }

        public static int CountValidAnchors(
            VaultBlueprint blueprint,
            DungeonGenerationContext context,
            MapManager map,
            int minDistanceFromPlayerStart,
            string requiredZoneId = null)
        {
            if (context == null || map == null)
                return 0;

            if (!context.UsesPaintedZoneMap && context.Definition?.LayoutStamp == null)
                return 0;

            List<Vector3Int> candidates = CollectOriginCandidates(context.Definition?.LayoutStamp, context);
            int valid = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (CanPlaceAt(blueprint, candidates[i], context, map, minDistanceFromPlayerStart, requiredZoneId))
                    valid++;
            }

            return valid;
        }

        public static void Shuffle(List<Vector3Int> list, System.Random rng)
        {
            PopulationPlacementUtility.Shuffle(list, rng);
        }

        public static void ReserveFootprint(VaultBlueprint blueprint, Vector3Int placementOrigin, DungeonGenerationContext context)
        {
            foreach (VaultMapCell cell in blueprint.OccupiedCells())
                context.ReservedCells.Add(blueprint.LocalToWorld(placementOrigin, cell.X, cell.Y));
        }

        public static bool TryGetZoneGeographicCenter(
            DungeonGenerationContext context,
            string zoneId,
            out Vector3Int centerCell)
        {
            centerCell = default;
            if (context?.ZoneBoundsByZoneId == null || string.IsNullOrEmpty(zoneId))
                return false;

            if (!context.ZoneBoundsByZoneId.TryGetValue(zoneId, out RectInt bounds))
                return false;

            centerCell = new Vector3Int(
                bounds.x + bounds.width / 2,
                bounds.y + bounds.height / 2,
                0);
            return true;
        }

        public static bool TryResolveZoneCenterOrigin(
            DungeonGenerationContext context,
            VaultBlueprint blueprint,
            string zoneId,
            out Vector3Int placementOrigin)
        {
            placementOrigin = default;
            if (blueprint == null || context == null)
                return false;

            if (!TryGetZoneGeographicCenter(context, zoneId, out Vector3Int centerCell))
                return false;

            MapManager map = MapManager.Instance;
            if (map != null)
            {
                HashSet<Vector3Int> reachable = CollectReachableWalkableCells(context, map);
                int bestDist = int.MaxValue;
                bool foundReachable = false;
                foreach (Vector3Int cell in reachable)
                {
                    if (!MeetsRequiredZone(context, cell, zoneId))
                        continue;

                    int dist = ChebyshevDistance(cell, centerCell);
                    if (dist >= bestDist)
                        continue;

                    bestDist = dist;
                    placementOrigin = cell;
                    foundReachable = true;
                }

                if (foundReachable)
                    return true;
            }

            // Fallback when the map is not bound yet — keep legacy geographic center.
            placementOrigin = centerCell;
            return true;
        }

        public static List<Vector3Int> CollectZoneOriginCandidates(
            DungeonGenerationContext context,
            string requiredZoneId)
        {
            var candidates = CollectOriginCandidates(context?.Definition?.LayoutStamp, context);
            if (string.IsNullOrEmpty(requiredZoneId))
                return candidates;

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                Vector3Int candidate = candidates[i];
                if (!context.TryGetZoneId(candidate, out string zoneId) || zoneId != requiredZoneId)
                    candidates.RemoveAt(i);
            }

            return candidates;
        }

        public static List<Vector3Int> CollectNearZoneNorthEdgeCandidates(
            DungeonGenerationContext context,
            VaultBlueprint blueprint,
            string requiredZoneId,
            int northMapRow,
            int maxChebyshevFromNorthEdge)
        {
            List<Vector3Int> candidates = CollectZoneOriginCandidates(context, requiredZoneId);
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (!DescentPlinthPlacementLogic.TryGetPortalCell(blueprint, candidates[i], out Vector3Int portalCell)
                    || !DescentPlinthPlacementLogic.IsNearNorthMapEdge(
                        portalCell,
                        northMapRow,
                        maxChebyshevFromNorthEdge))
                {
                    candidates.RemoveAt(i);
                }
            }

            return candidates;
        }

        public static void CollectChebyshevRingOrigins(
            Vector3Int center,
            int maxRadius,
            List<Vector3Int> buffer)
        {
            if (buffer == null || maxRadius <= 0)
                return;

            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius)
                            continue;

                        buffer.Add(new Vector3Int(center.x + dx, center.y + dy, 0));
                    }
                }
            }
        }

        public static void LogCandidateRejectionSummary(
            VaultBlueprint blueprint,
            IReadOnlyList<Vector3Int> candidates,
            DungeonGenerationContext context,
            MapManager map,
            int minDistanceFromPlayerStart,
            string requiredZoneId,
            bool requireReachableFromPlayerStart)
        {
            if (blueprint == null || candidates == null || candidates.Count == 0 || context == null || map == null)
                return;

            if (!PopulationPlacementUtility.TryGetMapBounds(context, out int mapWidth, out int mapHeight))
                return;

            int interior = 0;
            int replaceable = 0;
            int safeZone = 0;
            int reserved = 0;
            int zone = 0;
            int reachable = 0;
            int minDistance = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3Int origin = candidates[i];
                VaultPlacementRejectReason reason = GetPlacementRejectReason(
                    blueprint,
                    origin,
                    context,
                    map,
                    mapWidth,
                    mapHeight,
                    minDistanceFromPlayerStart,
                    requiredZoneId,
                    requireReachableFromPlayerStart);

                switch (reason)
                {
                    case VaultPlacementRejectReason.InteriorBounds:
                        interior++;
                        break;
                    case VaultPlacementRejectReason.ReplaceableCell:
                        replaceable++;
                        break;
                    case VaultPlacementRejectReason.SafeZone:
                        safeZone++;
                        break;
                    case VaultPlacementRejectReason.ReservedCell:
                        reserved++;
                        break;
                    case VaultPlacementRejectReason.RequiredZone:
                        zone++;
                        break;
                    case VaultPlacementRejectReason.ReachableFromPlayerStart:
                        reachable++;
                        break;
                    case VaultPlacementRejectReason.MinDistanceFromPlayerStart:
                        minDistance++;
                        break;
                }
            }

            DungeonGenerationLog.Warn(
                $"Vault '{blueprint.VaultId}' rejection summary (candidates={candidates.Count}): " +
                $"interior={interior} replaceable={replaceable} safeZone={safeZone} reserved={reserved} " +
                $"zone={zone} reachable={reachable} minDistance={minDistance}");
        }

        internal enum VaultPlacementRejectReason
        {
            None,
            MinDistanceFromPlayerStart,
            InteriorBounds,
            ReplaceableCell,
            SafeZone,
            ReservedCell,
            RequiredZone,
            ReachableFromPlayerStart,
        }

        internal static VaultPlacementRejectReason GetPlacementRejectReason(
            VaultBlueprint blueprint,
            Vector3Int placementOrigin,
            DungeonGenerationContext context,
            MapManager map,
            int mapWidth,
            int mapHeight,
            int minDistanceFromPlayerStart,
            string requiredZoneId,
            bool requireReachableFromPlayerStart)
        {
            if (!MeetsMinDistanceFromPlayerStart(
                    blueprint,
                    placementOrigin,
                    context.PlayerStart,
                    minDistanceFromPlayerStart))
                return VaultPlacementRejectReason.MinDistanceFromPlayerStart;

            Vector3Int anchorWorld = blueprint.LocalToWorld(
                placementOrigin,
                blueprint.Origin.x,
                blueprint.Origin.y);

            if (!IsWithinInteriorBounds(anchorWorld, mapWidth, mapHeight))
                return VaultPlacementRejectReason.InteriorBounds;

            if (!IsReplaceableCell(context, map, anchorWorld))
                return VaultPlacementRejectReason.ReplaceableCell;

            foreach (VaultMapCell cell in blueprint.OccupiedCells())
            {
                Vector3Int world = blueprint.LocalToWorld(placementOrigin, cell.X, cell.Y);

                if (!IsWithinInteriorBounds(world, mapWidth, mapHeight))
                    return VaultPlacementRejectReason.InteriorBounds;

                if (!IsReplaceableCell(context, map, world))
                    return VaultPlacementRejectReason.ReplaceableCell;

                if (context.IsInSafeZone(world))
                    return VaultPlacementRejectReason.SafeZone;

                if (context.ReservedCells.Contains(world))
                    return VaultPlacementRejectReason.ReservedCell;

                if (!MeetsRequiredZone(context, world, requiredZoneId))
                    return VaultPlacementRejectReason.RequiredZone;
            }

            if (requireReachableFromPlayerStart
                && !IsFootprintReachableFromPlayerStart(blueprint, placementOrigin, context, map))
            {
                return VaultPlacementRejectReason.ReachableFromPlayerStart;
            }

            return VaultPlacementRejectReason.None;
        }
    }
}
