using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.World.Generation.Phases;
using UnityEngine;

namespace JRogue.World.Generation.Vaults
{
    internal static class VaultPlacementUtility
    {
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
            int minDistanceFromPlayerStart)
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

            if (!IsReplaceableCell(context, map, anchorWorld))
                return false;

            foreach (VaultMapCell cell in blueprint.OccupiedCells())
            {
                Vector3Int world = blueprint.LocalToWorld(placementOrigin, cell.X, cell.Y);

                if (world.x < 0 || world.y < 0 || world.x >= mapWidth || world.y >= mapHeight)
                    return false;

                if (!IsReplaceableCell(context, map, world))
                    return false;

                if (context.IsInSafeZone(world))
                    return false;

                if (context.ReservedCells.Contains(world))
                    return false;
            }

            return true;
        }

        static bool IsReplaceableCell(DungeonGenerationContext context, MapManager map, Vector3Int world)
        {
            if (context.UsesZoneComposite)
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
            if (context != null && context.UsesZoneComposite)
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
            int minDistanceFromPlayerStart)
        {
            if (context == null || map == null)
                return 0;

            if (!context.UsesZoneComposite && context.Definition?.LayoutStamp == null)
                return 0;

            List<Vector3Int> candidates = CollectOriginCandidates(context.Definition?.LayoutStamp, context);
            int valid = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (CanPlaceAt(blueprint, candidates[i], context, map, minDistanceFromPlayerStart))
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
    }
}
