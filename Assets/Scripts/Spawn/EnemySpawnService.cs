using JRogue.Actors.Components;
using JRogue.Controller.Enemy;
using JRogue.Core.Actor;
using JRogue.Interactables;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.Spawn
{
    public static class EnemySpawnService
    {
        public static bool TrySpawn(
            EnemySpawnDefinition definition,
            Vector3Int originCell,
            out EnemyController spawned,
            Transform parent = null)
        {
            spawned = null;

            if (definition == null || definition.enemyPrefab == null)
            {
                Debug.LogWarning("[EnemySpawn] Missing spawn definition or enemy prefab.");
                return false;
            }

            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;
            if (map == null || grid == null)
            {
                Debug.LogWarning("[EnemySpawn] MapManager or GridManager not available.");
                return false;
            }

            EnemyController prefab = definition.enemyPrefab;
            if (!EnemySpawnPlacementResolver.TryResolveAnchor(
                    originCell,
                    definition.placementPolicy,
                    definition.primaryOffset,
                    prefab.footprintLayout,
                    prefab.footprintWidth,
                    prefab.footprintHeight,
                    prefab.currentFacing,
                    map,
                    grid,
                    InteractableTileService.Instance,
                    out Vector3Int anchor))
            {
                Debug.LogWarning(
                    $"[EnemySpawn] No valid tile for {prefab.name} near ({originCell.x},{originCell.y}).");
                return false;
            }

            EnemyController instance = Object.Instantiate(prefab, parent);
            instance.name = prefab.name;

            GridMover mover = instance.GetComponent<GridMover>();
            if (mover == null)
            {
                Debug.LogError($"[EnemySpawn] {prefab.name} has no GridMover.");
                Object.Destroy(instance.gameObject);
                return false;
            }

            mover.InitializeAtGridAnchor(anchor);
            spawned = instance;

            Debug.Log(
                $"[EnemySpawn] Spawned {instance.name} at ({anchor.x},{anchor.y}) from origin ({originCell.x},{originCell.y}).");
            return true;
        }
    }
}
