using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Hazards;
using JRogue.Core.Actor;
using JRogue.Interactables;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.Pathfinding
{
    /// <summary>
    /// Grid A* with 8-connected moves, octile heuristic, orthogonal/diagonal step costs (10/14),
    /// and corner-cutting prevention (diagonal only if both orthogonals are map-walkable).
    /// </summary>
    public static class GridAStarPathfinder
    {
        public const int OrthogonalCost = 10;
        public const int DiagonalCost = 14;

        /// <summary>
        /// Octile (Chebyshev-style) distance for integer costs D (orthogonal) and D2 (diagonal).
        /// </summary>
        public static int OctileHeuristic(Vector3Int from, Vector3Int to)
        {
            int dx = Mathf.Abs(from.x - to.x);
            int dy = Mathf.Abs(from.y - to.y);
            return OrthogonalCost * (dx + dy) + (DiagonalCost - 2 * OrthogonalCost) * Mathf.Min(dx, dy);
        }

        /// <summary>
        /// Returns the first grid step from <paramref name="start"/> along a lowest-cost path
        /// toward <paramref name="goal"/>, or false if there is no path.
        /// Walkability matches <see cref="MapManager.IsWalkable"/>; occupancy uses <see cref="GridManager.GetActorAt"/>
        /// (same sources of truth as <c>BaseActor.TryMove</c> for blocked tiles). The goal tile may be occupied
        /// (e.g. by the player) and is still a valid end node so the path can reach it; every other occupied cell blocks.
        /// </summary>
        public static bool TryGetFirstStepTowards(
            Vector3Int start,
            Vector3Int goal,
            GameObject seeker,
            MapManager mapManager,
            GridManager gridManager,
            out Vector3Int firstStep)
        {
            firstStep = default;
            if (seeker == null || mapManager == null || gridManager == null)
                return false;
            BaseActor seekerActor = seeker.GetComponent<BaseActor>();
            HazardService hazards = HazardService.Instance;

            IGridFootprint footprint = seeker.GetComponent<IGridFootprint>();
            if (footprint != null && !GridFootprintUtility.IsSingleCell(footprint))
            {
                return TryGetFirstStepTowardsFootprint(
                    start, goal, seeker, footprint, mapManager, gridManager, out firstStep);
            }

            bool CanEnter(Vector3Int c)
            {
                InteractableTileService interactables = InteractableTileService.Instance;
                if (interactables != null && interactables.BlocksOccupancy(c))
                    return false;

                if (c == goal)
                    return mapManager.IsWalkable(c) && CanEnterHazard(c);

                if (!mapManager.IsWalkable(c))
                    return false;

                if (!CanEnterHazard(c))
                    return false;

                IBattleTarget occupant = gridManager.GetActorAt(c);
                return occupant == null || occupant.Owner == seeker;
            }

            bool CanEnterHazard(Vector3Int c)
            {
                if (hazards == null || seekerActor == null)
                    return true;
                return hazards.CanEnter(c, seekerActor);
            }

            bool CornerClearForDiagonal(Vector3Int from, Vector3Int to)
            {
                Vector3Int d = to - from;
                if (d.x == 0 || d.y == 0)
                    return true;

                Vector3Int orthA = from + new Vector3Int(d.x, 0, 0);
                Vector3Int orthB = from + new Vector3Int(0, d.y, 0);
                return mapManager.IsWalkable(orthA) && mapManager.IsWalkable(orthB);
            }

            return TryGetFirstStepInternal(start, goal, CanEnter, CornerClearForDiagonal, out firstStep);
        }

        static readonly List<Vector3Int> PathFootprintBuffer = new List<Vector3Int>(16);

        static bool TryGetFirstStepTowardsFootprint(
            Vector3Int start,
            Vector3Int goal,
            GameObject seeker,
            IGridFootprint footprint,
            MapManager mapManager,
            GridManager gridManager,
            out Vector3Int firstStep)
        {
            firstStep = default;
            BaseActor seekerActor = seeker.GetComponent<BaseActor>();
            HazardService hazards = HazardService.Instance;

            bool CanEnterAnchor(Vector3Int anchor)
            {
                GridFootprintUtility.GetOccupiedCells(
                    anchor,
                    footprint.Layout,
                    footprint.FootprintWidth,
                    footprint.FootprintHeight,
                    footprint.Facing,
                    PathFootprintBuffer);

                for (int i = 0; i < PathFootprintBuffer.Count; i++)
                {
                    Vector3Int cell = PathFootprintBuffer[i];
                    InteractableTileService interactables = InteractableTileService.Instance;
                    if (interactables != null && interactables.BlocksOccupancy(cell))
                        return false;

                    if (!mapManager.IsWalkable(cell))
                        return false;

                    if (hazards != null && seekerActor != null && !hazards.CanEnter(cell, seekerActor))
                        return false;

                    IBattleTarget occupant = gridManager.GetActorAt(cell);
                    if (occupant == null || occupant.Owner == seeker)
                        continue;

                    if (cell == goal)
                        continue;

                    return false;
                }

                return true;
            }

            bool CornerClearForDiagonal(Vector3Int from, Vector3Int to)
            {
                Vector3Int d = to - from;
                if (d.x == 0 || d.y == 0)
                    return true;

                Vector3Int orthA = from + new Vector3Int(d.x, 0, 0);
                Vector3Int orthB = from + new Vector3Int(0, d.y, 0);
                if (!CanEnterAnchor(orthA) || !CanEnterAnchor(orthB))
                    return false;

                return mapManager.IsWalkable(orthA) && mapManager.IsWalkable(orthB);
            }

            return TryGetFirstStepInternal(start, goal, CanEnterAnchor, CornerClearForDiagonal, out firstStep);
        }

        /// <summary>
        /// Test hook: inject walkability and corner rules without Unity map/grid objects.
        /// </summary>
        internal static bool TryGetFirstStepInternal(
            Vector3Int start,
            Vector3Int goal,
            Func<Vector3Int, bool> canEnterCell,
            Func<Vector3Int, Vector3Int, bool> isDiagonalCornerClear,
            out Vector3Int firstStep)
        {
            firstStep = default;
            if (start == goal)
                return false;

            if (!canEnterCell(start) || !canEnterCell(goal))
                return false;

            var open = new List<Vector3Int> { start };
            var gScore = new Dictionary<Vector3Int, int> { [start] = 0 };
            var fScore = new Dictionary<Vector3Int, int> { [start] = OctileHeuristic(start, goal) };
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var closed = new HashSet<Vector3Int>();

            while (open.Count > 0)
            {
                int bestIdx = 0;
                int bestF = fScore[open[0]];
                for (int i = 1; i < open.Count; i++)
                {
                    int f = fScore[open[i]];
                    if (f < bestF)
                    {
                        bestF = f;
                        bestIdx = i;
                    }
                }

                Vector3Int current = open[bestIdx];
                open[bestIdx] = open[^1];
                open.RemoveAt(open.Count - 1);

                if (closed.Contains(current))
                    continue;

                closed.Add(current);

                if (current == goal)
                {
                    firstStep = ReconstructFirstStep(start, goal, cameFrom);
                    return firstStep != start;
                }

                foreach (Vector3Int offset in GridManager.EightDirectionOffsets)
                {
                    Vector3Int neighbor = current + offset;
                    if (closed.Contains(neighbor))
                        continue;

                    if (!canEnterCell(neighbor))
                        continue;

                    bool diagonal = offset.x != 0 && offset.y != 0;
                    if (diagonal && !isDiagonalCornerClear(current, neighbor))
                        continue;

                    int moveCost = diagonal ? DiagonalCost : OrthogonalCost;
                    int tentativeG = gScore[current] + moveCost;

                    int knownG = gScore.TryGetValue(neighbor, out int g) ? g : int.MaxValue;
                    if (tentativeG >= knownG)
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + OctileHeuristic(neighbor, goal);

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }

            return false;
        }

        private static Vector3Int ReconstructFirstStep(
            Vector3Int start,
            Vector3Int goal,
            IReadOnlyDictionary<Vector3Int, Vector3Int> cameFrom)
        {
            Vector3Int cursor = goal;
            while (cameFrom.TryGetValue(cursor, out Vector3Int prev))
            {
                if (prev == start)
                    return cursor;

                cursor = prev;
            }

            return start;
        }
    }
}
