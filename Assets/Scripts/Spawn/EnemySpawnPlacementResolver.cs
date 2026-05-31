using System.Collections.Generic;
using JRogue.Core.Actor;
using JRogue.Interactables;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.Spawn
{
    /// <summary>
    /// Resolves a grid anchor for spawning an enemy footprint near an origin cell.
    /// </summary>
    public static class EnemySpawnPlacementResolver
    {
        static readonly List<Vector3Int> FootprintScratch = new List<Vector3Int>(8);

        public static bool TryResolveAnchor(
            Vector3Int originCell,
            EnemySpawnPlacementPolicy policy,
            Vector3Int primaryOffset,
            FootprintLayout footprintLayout,
            int footprintWidth,
            int footprintHeight,
            FacingDirection footprintFacing,
            MapManager map,
            GridManager grid,
            InteractableTileService interactables,
            out Vector3Int anchor)
        {
            anchor = default;
            if (map == null || grid == null)
                return false;

            if (policy == EnemySpawnPlacementPolicy.NorthOfOriginThenNearestUnoccupiedFloor)
            {
                Vector3Int primary = originCell + primaryOffset;
                if (CanPlaceFootprintAt(
                        primary,
                        footprintLayout,
                        footprintWidth,
                        footprintHeight,
                        footprintFacing,
                        map,
                        grid,
                        interactables))
                {
                    anchor = primary;
                    return true;
                }
            }

            return TryFindNearestFootprintAnchor(
                originCell,
                footprintLayout,
                footprintWidth,
                footprintHeight,
                footprintFacing,
                map,
                grid,
                interactables,
                out anchor);
        }

        static bool TryFindNearestFootprintAnchor(
            Vector3Int origin,
            FootprintLayout layout,
            int width,
            int height,
            FacingDirection facing,
            MapManager map,
            GridManager grid,
            InteractableTileService interactables,
            out Vector3Int anchor)
        {
            anchor = default;
            var visited = new HashSet<Vector3Int> { origin };
            var frontier = new Queue<Vector3Int>();
            frontier.Enqueue(origin);

            Vector3Int best = origin;
            int bestDist = int.MaxValue;
            bool found = false;

            while (frontier.Count > 0)
            {
                Vector3Int cell = frontier.Dequeue();
                int dist = Manhattan(origin, cell);

                if (cell != origin
                    && CanPlaceFootprintAt(cell, layout, width, height, facing, map, grid, interactables))
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = cell;
                        found = true;
                    }
                }

                foreach (Vector3Int offset in GridManager.EightDirectionOffsets)
                {
                    Vector3Int next = cell + offset;
                    if (!visited.Add(next) || !map.IsWalkable(next))
                        continue;

                    frontier.Enqueue(next);
                }
            }

            if (!found)
                return false;

            anchor = best;
            return true;
        }

        public static bool CanPlaceFootprintAt(
            Vector3Int anchor,
            FootprintLayout layout,
            int width,
            int height,
            FacingDirection facing,
            MapManager map,
            GridManager grid,
            InteractableTileService interactables)
        {
            if (map == null || grid == null)
                return false;

            GridFootprintUtility.GetOccupiedCells(anchor, layout, width, height, facing, FootprintScratch);
            if (FootprintScratch.Count == 0)
                return false;

            for (int i = 0; i < FootprintScratch.Count; i++)
            {
                Vector3Int cell = FootprintScratch[i];
                if (!map.IsWalkable(cell))
                    return false;

                if (GridFeatures.MapCellOccupancy.BlocksActorEntry(cell))
                    return false;

                if (grid.GetActorAt(cell) != null)
                    return false;
            }

            return true;
        }

        static int Manhattan(Vector3Int a, Vector3Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
