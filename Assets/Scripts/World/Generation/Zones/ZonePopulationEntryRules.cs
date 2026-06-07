using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public enum ZonePopulationDensityMode
    {
        ScatterCount = 0,
        DensityPer100Tiles = 1,
    }

    public static class ZonePopulationEntryRules
    {
        public static bool MeetsTagRequirement(
            DungeonFloorZoneLayout layout,
            string zoneId,
            string requiresTag)
        {
            if (string.IsNullOrEmpty(requiresTag))
                return true;

            if (layout == null || !layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition definition))
                return false;

            return ZoneHasTag(definition, requiresTag);
        }

        public static bool ZoneHasTag(DungeonZoneDefinition definition, string tag)
        {
            if (definition == null || string.IsNullOrEmpty(tag))
                return false;

            string[] tags = definition.Tags;
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                    return true;
            }

            return false;
        }

        public static int ChebyshevDistanceToAabbEdge(Vector3Int cell, RectInt bounds)
        {
            int dx = Mathf.Min(cell.x - bounds.xMin, bounds.xMax - 1 - cell.x);
            int dy = Mathf.Min(cell.y - bounds.yMin, bounds.yMax - 1 - cell.y);
            return Mathf.Min(dx, dy);
        }

        public static bool MeetsEdgeRequirement(Vector3Int cell, RectInt bounds, int forbiddenNearEdge)
        {
            if (forbiddenNearEdge <= 0)
                return true;

            return ChebyshevDistanceToAabbEdge(cell, bounds) >= forbiddenNearEdge;
        }

        public static int CountEligibleCandidates(
            IReadOnlyList<Vector3Int> candidates,
            int startIndex,
            RectInt bounds,
            int forbiddenNearEdge,
            System.Func<Vector3Int, bool> isCandidateValid)
        {
            if (candidates == null || isCandidateValid == null)
                return 0;

            int count = 0;
            for (int i = startIndex; i < candidates.Count; i++)
            {
                Vector3Int cell = candidates[i];
                if (!MeetsEdgeRequirement(cell, bounds, forbiddenNearEdge))
                    continue;

                if (!isCandidateValid(cell))
                    continue;

                count++;
            }

            return count;
        }

        public static int RollSpawnCount(
            ZonePopulationDensityMode densityMode,
            int minCount,
            int maxCount,
            int eligibleCandidateCount,
            System.Random rng)
        {
            if (eligibleCandidateCount <= 0 || rng == null)
                return 0;

            if (densityMode == ZonePopulationDensityMode.DensityPer100Tiles)
            {
                int density = rng.Next(minCount, maxCount + 1);
                return eligibleCandidateCount * density / 100;
            }

            return rng.Next(minCount, maxCount + 1);
        }
    }
}
