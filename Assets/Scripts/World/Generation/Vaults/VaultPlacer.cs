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
            out Vector3Int placedOrigin)
        {
            placedOrigin = default;
            if (blueprint == null || registry == null || context == null || rng == null)
                return false;

            MapManager map = MapManager.Instance;
            if (map == null)
                return false;

            if (!context.UsesZoneComposite && context.Definition?.LayoutStamp == null)
                return false;

            int minDistance = minDistanceOverride > 0
                ? minDistanceOverride
                : blueprint.MinDistanceFromPlayerStart;

            List<Vector3Int> candidates =
                VaultPlacementUtility.CollectOriginCandidates(context.Definition?.LayoutStamp, context);
            if (candidates.Count == 0)
            {
                DungeonGenerationLog.Warn(
                    $"Vault '{blueprint.VaultId}': no origin candidates outside safe zone.");
                return false;
            }

            VaultPlacementUtility.Shuffle(candidates, rng);

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3Int origin = candidates[i];
                if (!VaultPlacementUtility.CanPlaceAt(blueprint, origin, context, map, minDistance))
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
            DungeonGenerationLog.Warn(
                $"Vault '{blueprint.VaultId}': no valid anchor (minDistance={minDistance}, " +
                $"candidates={candidates.Count}, map={mapWidth}x{mapHeight}, playerStart={context.PlayerStart}).");
            return false;
        }
    }
}
