using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.World.Generation;
using JRogue.World.Generation.Phases;
using UnityEngine;

namespace JRogue.World.Generation.Vaults
{
    internal static class VaultPlacer
    {
        public static bool TryPlaceOnce(
            VaultBlueprint blueprint,
            VaultAssetRegistry registry,
            DungeonGenerationContext context,
            System.Random rng,
            int minDistanceOverride,
            string requiredZoneId,
            out Vector3Int placedOrigin,
            bool requireReachableFromPlayerStart = false) =>
            TryPlaceFromCandidates(
                blueprint,
                registry,
                context,
                VaultPlacementUtility.CollectOriginCandidates(context.Definition?.LayoutStamp, context),
                rng,
                minDistanceOverride,
                requiredZoneId,
                out placedOrigin,
                requireReachableFromPlayerStart);

        public static bool TryPlaceAtOrigin(
            VaultBlueprint blueprint,
            VaultAssetRegistry registry,
            DungeonGenerationContext context,
            Vector3Int origin,
            int minDistanceOverride,
            string requiredZoneId,
            out Vector3Int placedOrigin,
            bool requireReachableFromPlayerStart = false)
        {
            placedOrigin = origin;
            if (blueprint == null || registry == null || context == null)
                return false;

            MapManager map = MapManager.Instance;
            if (map == null)
                return false;

            int minDistance = minDistanceOverride > 0
                ? minDistanceOverride
                : blueprint.MinDistanceFromPlayerStart;

            if (!VaultPlacementUtility.CanPlaceAt(
                    blueprint,
                    origin,
                    context,
                    map,
                    minDistance,
                    requiredZoneId,
                    requireReachableFromPlayerStart))
                return false;

            if (!VaultStamper.TryStamp(blueprint, registry, origin, context, out string error))
            {
                DungeonGenerationLog.Warn(
                    $"Vault '{blueprint.VaultId}' stamp failed at {origin}: {error}");
                return false;
            }

            placedOrigin = origin;
            return true;
        }

        public static bool TryPlaceFromCandidates(
            VaultBlueprint blueprint,
            VaultAssetRegistry registry,
            DungeonGenerationContext context,
            List<Vector3Int> candidates,
            System.Random rng,
            int minDistanceOverride,
            string requiredZoneId,
            out Vector3Int placedOrigin,
            bool requireReachableFromPlayerStart = false)
        {
            placedOrigin = default;
            if (blueprint == null || registry == null || context == null || candidates == null)
                return false;

            MapManager map = MapManager.Instance;
            if (map == null)
                return false;

            if (!context.UsesPaintedZoneMap && context.Definition?.LayoutStamp == null)
                return false;

            if (candidates.Count == 0)
            {
                DungeonGenerationLog.Warn(
                    $"Vault '{blueprint.VaultId}': no origin candidates outside safe zone.");
                return false;
            }

            if (rng != null)
                VaultPlacementUtility.Shuffle(candidates, rng);

            int minDistance = minDistanceOverride > 0
                ? minDistanceOverride
                : blueprint.MinDistanceFromPlayerStart;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3Int origin = candidates[i];
                if (!VaultPlacementUtility.CanPlaceAt(
                        blueprint,
                        origin,
                        context,
                        map,
                        minDistance,
                        requiredZoneId,
                        requireReachableFromPlayerStart))
                    continue;

                if (!VaultStamper.TryStamp(blueprint, registry, origin, context, out string error))
                {
                    DungeonGenerationLog.Warn(
                        $"Vault '{blueprint.VaultId}' stamp failed at {origin}: {error}");
                    continue;
                }

                placedOrigin = origin;
                return true;
            }

            PopulationPlacementUtility.TryGetMapBounds(context, out int mapWidth, out int mapHeight);
            VaultPlacementUtility.LogCandidateRejectionSummary(
                blueprint,
                candidates,
                context,
                map,
                minDistance,
                requiredZoneId,
                requireReachableFromPlayerStart);
            DungeonGenerationLog.Warn(
                $"Vault '{blueprint.VaultId}': no valid anchor (minDistance={minDistance}, " +
                $"candidates={candidates.Count}, map={mapWidth}x{mapHeight}, playerStart={context.PlayerStart}).");
            return false;
        }

        public static bool TryPlaceNearPreferredOrigin(
            VaultBlueprint blueprint,
            VaultAssetRegistry registry,
            DungeonGenerationContext context,
            System.Random rng,
            Vector3Int preferredOrigin,
            int minDistanceOverride,
            string requiredZoneId,
            int maxSearchRadius,
            out Vector3Int placedOrigin,
            bool requireReachableFromPlayerStart = false)
        {
            placedOrigin = default;
            if (blueprint == null || registry == null || context == null)
                return false;

            var searchOrigins = new List<Vector3Int>(1 + maxSearchRadius * maxSearchRadius * 4);
            searchOrigins.Add(preferredOrigin);
            VaultPlacementUtility.CollectChebyshevRingOrigins(preferredOrigin, maxSearchRadius, searchOrigins);

            return TryPlaceFromCandidates(
                blueprint,
                registry,
                context,
                searchOrigins,
                rng,
                minDistanceOverride,
                requiredZoneId,
                out placedOrigin,
                requireReachableFromPlayerStart);
        }
    }
}
