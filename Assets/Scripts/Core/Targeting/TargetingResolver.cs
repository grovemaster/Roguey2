using System.Collections.Generic;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using UnityEngine;

namespace JRogue.Core.Targeting
{
    public static class TargetingResolver
    {
        // High-performance check for Teleport: Is the tile empty?
        public static bool IsTileOccupied(Vector3Int tile)
        {
            return GridManager.Instance.GetActorAt(tile) != null;
        }

        // AOE Check for Fireball
        public static List<IBattleTarget> GetTargetsInRadius(Vector3Int center, int radius)
        {
            List<IBattleTarget> results = new List<IBattleTarget>();

            // We only loop through ACTORS, not every tile in the world
            foreach (var actor in GridManager.Instance.GetAllActors())
            {
                // Manhattan distance is often better for grid games, 
                // but Vector3Int.Distance works for Euclidean circles
                if (Vector3Int.Distance(actor.GridPosition, center) <= radius)
                // if (Vector3Int.Distance(actor.GridPosition, center) <= radius)
                {
                    results.Add(actor);
                }
            }
            return results;
        }
    }
}