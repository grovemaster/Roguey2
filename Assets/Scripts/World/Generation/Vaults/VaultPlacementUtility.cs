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

            DungeonLayoutStamp stamp = context.Definition?.LayoutStamp;
            if (stamp == null)
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

            if (!stamp.IsFloor(anchorWorld.x, anchorWorld.y))
                return false;

            foreach (VaultMapCell cell in blueprint.OccupiedCells())
            {
                Vector3Int world = blueprint.LocalToWorld(placementOrigin, cell.X, cell.Y);

                if (world.x < 0 || world.y < 0 || world.x >= stamp.Width || world.y >= stamp.Height)
                    return false;

                if (!IsReplaceableStampCell(stamp, world.x, world.y))
                    return false;

                if (context.IsInSafeZone(world))
                    return false;

                if (context.ReservedCells.Contains(world))
                    return false;
            }

            return true;
        }

        /// <summary>Vault may overwrite floor or wall cells; void (neither) is rejected.</summary>
        static bool IsReplaceableStampCell(DungeonLayoutStamp stamp, int x, int y) =>
            stamp.IsFloor(x, y) || stamp.IsWall(x, y);

        public static List<Vector3Int> CollectOriginCandidates(
            DungeonLayoutStamp stamp,
            DungeonGenerationContext context)
        {
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
            DungeonLayoutStamp stamp = context?.Definition?.LayoutStamp;
            if (stamp == null || map == null)
                return 0;

            List<Vector3Int> candidates = CollectOriginCandidates(stamp, context);
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
