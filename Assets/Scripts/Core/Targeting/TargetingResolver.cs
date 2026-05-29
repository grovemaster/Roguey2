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

        /// <summary>Battle targets whose footprint includes <paramref name="tile"/> (multi-tile aware).</summary>
        public static List<IBattleTarget> GetTargetsOnTile(Vector3Int tile)
        {
            var results = new List<IBattleTarget>();
            if (GridManager.Instance == null)
                return results;

            foreach (IBattleTarget actor in GridManager.Instance.GetAllActors())
            {
                if (actor == null)
                    continue;

                if (actor is IGridFootprint footprint)
                {
                    if (GridFootprintUtility.Occupies(footprint, tile))
                        results.Add(actor);
                }
                else if (actor.GridPosition == tile)
                {
                    results.Add(actor);
                }
            }

            return results;
        }

        // AOE Check for Fireball
        public static List<IBattleTarget> GetTargetsInRadius(Vector3Int center, int radius)
        {
            List<IBattleTarget> results = new List<IBattleTarget>();

            foreach (IBattleTarget actor in GridManager.Instance.GetAllActors())
            {
                int dist = NearestFootprintDistance(center, actor);
                if (dist <= radius)
                    results.Add(actor);
            }

            return results;
        }

        static int NearestFootprintDistance(Vector3Int from, IBattleTarget target)
        {
            if (target is IGridFootprint footprint)
                return GridFootprintUtility.ManhattanDistanceToFootprint(from, footprint);

            return Mathf.Abs(from.x - target.GridPosition.x) + Mathf.Abs(from.y - target.GridPosition.y);
        }
    }
}