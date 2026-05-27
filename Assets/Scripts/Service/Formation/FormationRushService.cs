using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Hazards;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Pathfinding;
using UnityEngine;

namespace JRogue.Service.Formation
{
    /// <summary>
    /// Stateless service that drives the "follower rush" formation behavior:
    /// after a leader's action, every follower attempts to advance up to
    /// <see cref="MaxRushDistance"/> tiles toward their breadcrumb slot in
    /// <see cref="PartyManager.positionHistory"/>. Pulled out of InputHandler
    /// so the rush algorithm and its tile-validity rules live in one place
    /// and can be exercised independently of input.
    /// </summary>
    public static class FormationRushService
    {
        public const int MaxRushDistance = 2;

        /// <summary>
        /// Plan and execute a follower rush. Marks every party member's turn
        /// as complete and force-ends the player turn.
        /// </summary>
        public static void Rush(
            PartyManager party,
            TurnManager turn,
            GridManager grid,
            MapManager map)
        {
            if (party == null || turn == null || grid == null || map == null) return;

            List<BaseActor> members = party.partyMembers;
            List<Vector3Int> history = party.positionHistory;
            BaseActor leader = party.GetActiveMember();
            if (leader == null) return;

            Debug.Log($"[RUSH] Starting Rush. Party: {members.Count}, History: {history.Count}");

            // 1. Lift everyone off the spatial hash so planning has a clean slate.
            //    We unregister even already-acted followers; they'll be re-claimed
            //    in the planning pass so subsequent followers can't overlap them.
            foreach (BaseActor member in members)
            {
                if (member != null) grid.UnregisterActor(member.GridPosition);
            }

            // 2. Lock the leader at their new tile so no follower can plan into it.
            grid.RegisterActor(leader.GridPosition, leader);

            Dictionary<BaseActor, Vector3Int> plannedMoves = new Dictionary<BaseActor, Vector3Int>();

            // 3. Plan a destination for each follower (skip leader at index 0).
            for (int i = 1; i < members.Count; i++)
            {
                BaseActor follower = members[i];
                if (follower == null) continue;

                if (!turn.CanActorTakeAction(follower.gameObject))
                {
                    // Already acted: claim current tile so others can't take it.
                    plannedMoves.Add(follower, follower.GridPosition);
                    grid.RegisterActor(follower.GridPosition, follower);
                    continue;
                }

                Vector3Int historicalTarget = (i < history.Count)
                    ? history[i]
                    : follower.GridPosition;

                Vector3Int finalTarget = ComputeRushTarget(
                    follower,
                    historicalTarget,
                    map,
                    grid,
                    plannedMoves);

                if (IsValidMove(map, grid, finalTarget, plannedMoves, follower: follower))
                {
                    plannedMoves.Add(follower, finalTarget);
                    Debug.Log($"[RUSH-PLAN] {follower.name} accepted target {finalTarget}");
                }
                else
                {
                    Vector3Int bestSmartTile = FindBestBurstTile(
                        follower,
                        historicalTarget,
                        map,
                        grid,
                        plannedMoves);
                    plannedMoves.Add(follower, bestSmartTile);
                }
            }

            // 4. Land everyone at their planned destination.
            foreach (var move in plannedMoves)
            {
                BaseActor actor = move.Key;
                Vector3Int dest = move.Value;

                if (actor.GridPosition != dest)
                {
                    Debug.Log($"[RUSH-LAND] {actor.name} moving {actor.GridPosition} -> {dest}");
                    actor.ApplyPositionChange(dest);
                }
                else
                {
                    grid.RegisterActor(actor.GridPosition, actor);
                }

                turn.OnPlayerActionComplete(actor.gameObject);
            }

            turn.OnPlayerActionComplete(leader.gameObject);

            Debug.Log("[RUSH-COMPLETE] Grid synchronized. Ending player turn.");
            turn.ForceEndPlayerTurn();
        }

        /// <summary>
        /// Tile-validity check shared by rush planning and the leader's manual
        /// move path. <paramref name="allowAllies"/>=true lets the leader bump
        /// into any occupant (combat or swap is decided downstream).
        /// </summary>
        public static bool IsValidMove(
            MapManager map,
            GridManager grid,
            Vector3Int tile,
            Dictionary<BaseActor, Vector3Int> plannedMoves,
            bool allowAllies = false,
            BaseActor follower = null)
        {
            if (map == null || grid == null) return false;
            if (!map.IsWalkable(tile)) return false;

            if (follower != null
                && !HasLegalRushPath(map, grid, follower, tile, plannedMoves))
            {
                return false;
            }

            if (follower != null && HazardService.Instance != null
                && !HazardService.Instance.CanEnter(tile, follower))
            {
                return false;
            }

            IBattleTarget occupant = grid.GetActorAt(tile);
            if (occupant != null)
            {
                // Leader manual move: allow so OnBump() can resolve.
                // Follower rush: never plan into an occupied tile.
                return allowAllies;
            }

            if (plannedMoves != null && plannedMoves.ContainsValue(tile)) return false;
            return true;
        }

        /// <summary>
        /// Advances up to <see cref="MaxRushDistance"/> steps toward
        /// <paramref name="goal"/> using A* (hazards, allies, planned tiles).
        /// </summary>
        static Vector3Int ComputeRushTarget(
            BaseActor follower,
            Vector3Int goal,
            MapManager map,
            GridManager grid,
            Dictionary<BaseActor, Vector3Int> plannedMoves)
        {
            Vector3Int current = follower.GridPosition;
            if (current == goal)
                return current;

            int steps = 0;
            while (steps < MaxRushDistance && current != goal)
            {
                if (!TryGetRushFirstStep(
                        current,
                        goal,
                        follower,
                        map,
                        grid,
                        plannedMoves,
                        out Vector3Int next))
                {
                    break;
                }

                current = next;
                steps++;
            }

            return current;
        }

        static bool TryGetRushFirstStep(
            Vector3Int start,
            Vector3Int goal,
            BaseActor follower,
            MapManager map,
            GridManager grid,
            Dictionary<BaseActor, Vector3Int> plannedMoves,
            out Vector3Int firstStep)
        {
            firstStep = default;
            if (follower == null || map == null || grid == null || start == goal)
                return false;

            HazardService hazards = HazardService.Instance;

            bool CanEnter(Vector3Int cell)
            {
                if (cell == goal)
                {
                    if (!map.IsWalkable(cell))
                        return false;

                    if (hazards != null && !hazards.CanEnter(cell, follower))
                        return false;

                    return !IsBlockedByPlannedMove(cell, plannedMoves, goal);
                }

                return IsTraversableForRush(cell, follower, map, grid, plannedMoves, goal);
            }

            bool CornerClear(Vector3Int from, Vector3Int to)
            {
                Vector3Int d = to - from;
                if (d.x == 0 || d.y == 0)
                    return true;

                Vector3Int orthA = from + new Vector3Int(d.x, 0, 0);
                Vector3Int orthB = from + new Vector3Int(0, d.y, 0);
                return CanEnter(orthA) && CanEnter(orthB);
            }

            return GridAStarPathfinder.TryGetFirstStepInternal(
                start,
                goal,
                CanEnter,
                CornerClear,
                out firstStep);
        }

        static Vector3Int FindBestBurstTile(
            BaseActor follower,
            Vector3Int breadcrumb,
            MapManager map,
            GridManager grid,
            Dictionary<BaseActor, Vector3Int> plannedMoves)
        {
            Vector3Int best = follower.GridPosition;
            float bestDist = float.MaxValue;
            bool found = false;

            var frontier = new Queue<Vector3Int>();
            var visited = new HashSet<Vector3Int> { follower.GridPosition };
            frontier.Enqueue(follower.GridPosition);

            while (frontier.Count > 0)
            {
                Vector3Int cell = frontier.Dequeue();
                int stepsFromStart = Mathf.Max(
                    Mathf.Abs(cell.x - follower.GridPosition.x),
                    Mathf.Abs(cell.y - follower.GridPosition.y));

                if (cell != follower.GridPosition
                    && IsValidMove(map, grid, cell, plannedMoves, follower: follower))
                {
                    float d = Vector3Int.Distance(cell, breadcrumb);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = cell;
                        found = true;
                    }
                }

                if (stepsFromStart >= MaxRushDistance)
                    continue;

                foreach (Vector3Int offset in GridManager.EightDirectionOffsets)
                {
                    Vector3Int neighbor = cell + offset;
                    if (!visited.Add(neighbor))
                        continue;

                    if (!map.IsWalkable(neighbor))
                        continue;

                    frontier.Enqueue(neighbor);
                }
            }

            return found ? best : follower.GridPosition;
        }

        static bool IsTraversableForRush(
            Vector3Int cell,
            BaseActor follower,
            MapManager map,
            GridManager grid,
            Dictionary<BaseActor, Vector3Int> plannedMoves,
            Vector3Int goal)
        {
            if (!map.IsWalkable(cell))
                return false;

            if (HazardService.Instance != null && !HazardService.Instance.CanEnter(cell, follower))
                return false;

            if (IsBlockedByPlannedMove(cell, plannedMoves, goal))
                return false;

            IBattleTarget occupant = grid.GetActorAt(cell);
            return occupant == null || occupant.Owner == follower.gameObject;
        }

        static bool IsBlockedByPlannedMove(
            Vector3Int cell,
            Dictionary<BaseActor, Vector3Int> plannedMoves,
            Vector3Int goal)
        {
            return plannedMoves != null
                && plannedMoves.ContainsValue(cell)
                && cell != goal;
        }

        static bool HasLegalRushPath(
            MapManager map,
            GridManager grid,
            BaseActor follower,
            Vector3Int destination,
            Dictionary<BaseActor, Vector3Int> plannedMoves)
        {
            return ComputeRushTarget(follower, destination, map, grid, plannedMoves) == destination;
        }
    }
}
