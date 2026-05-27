using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Traps;
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

                Vector3Int rushGoal = ResolveRushGoal(
                    follower,
                    historicalTarget,
                    map,
                    grid,
                    plannedMoves);

                Vector3Int dest = PickFollowerRushDestination(
                    follower,
                    historicalTarget,
                    rushGoal,
                    map,
                    grid,
                    plannedMoves);

                plannedMoves.Add(follower, dest);
                Debug.Log($"[RUSH-PLAN] {follower.name} destination {dest}");
            }

            // 4. Land everyone at their planned destination.
            foreach (var move in plannedMoves)
            {
                BaseActor actor = move.Key;
                Vector3Int dest = move.Value;

                LandFollowerAt(grid, map, actor, dest, plannedMoves);

                turn.OnPlayerActionComplete(actor.gameObject);
            }

            turn.OnPlayerActionComplete(leader.gameObject);

            ReconcilePartyOnGrid(grid, map, members);

            Debug.Log("[RUSH-COMPLETE] Grid synchronized. Ending player turn.");
            turn.ForceEndPlayerTurn();
        }

        /// <summary>
        /// Ensures every party member is registered on the spatial hash at their current cell.
        /// </summary>
        static void ReconcilePartyOnGrid(
            GridManager grid,
            MapManager map,
            List<BaseActor> members)
        {
            if (grid == null || map == null || members == null)
                return;

            foreach (BaseActor member in members)
            {
                if (member == null)
                    continue;

                if (TryRegisterOnGrid(grid, member, member.GridPosition))
                    continue;

                Vector3Int hold = FindEmergencyHoldTile(
                    member,
                    map,
                    grid,
                    plannedMoves: null,
                    preferNear: member.GridPosition);

                if (hold != member.GridPosition)
                    member.ApplyPositionChange(hold);

                if (!TryRegisterOnGrid(grid, member, member.GridPosition))
                {
                    Debug.LogError(
                        $"[RUSH-RECONCILE] {member.name} still unregistered at {member.GridPosition}.");
                }
            }
        }

        /// <summary>
        /// Tile-validity check shared by rush planning and formation leader gating.
        /// <paramref name="allowAllies"/>=true validates only the destination tile
        /// (leader manual move); followers use full <see cref="HasLegalRushPath"/>.
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

            InteractableTileService interactables = InteractableTileService.Instance;
            if (interactables != null && interactables.BlocksOccupancy(tile))
                return false;

            TrapService traps = TrapService.Instance;
            if (traps != null && !allowAllies && traps.IsPathingAvoidCell(tile))
                return false;

            if (!allowAllies && IsTileClaimedByAnotherFollower(plannedMoves, tile, follower))
                return false;

            if (follower != null
                && !allowAllies
                && !HasLegalRushPath(map, grid, follower, tile, plannedMoves))
            {
                return false;
            }

            if (follower != null && HazardService.Instance != null)
            {
                HazardService hazards = HazardService.Instance;
                if (!hazards.CanEnter(tile, follower))
                    return false;

                if (!allowAllies && hazards.IsPathingAvoidCell(tile))
                    return false;
            }

            IBattleTarget occupant = grid.GetActorAt(tile);
            if (occupant != null)
            {
                // Leader manual move: allow so OnBump() can resolve.
                // Follower rush: never plan into an occupied tile.
                return allowAllies;
            }

            return true;
        }

        static bool IsTileClaimedByAnotherFollower(
            Dictionary<BaseActor, Vector3Int> plannedMoves,
            Vector3Int tile,
            BaseActor excludeActor = null)
        {
            if (plannedMoves == null)
                return false;

            foreach (KeyValuePair<BaseActor, Vector3Int> entry in plannedMoves)
            {
                if (excludeActor != null && entry.Key == excludeActor)
                    continue;

                if (entry.Value == tile)
                    return true;
            }

            return false;
        }

        static Vector3Int PickFollowerRushDestination(
            BaseActor follower,
            Vector3Int historicalTarget,
            Vector3Int rushGoal,
            MapManager map,
            GridManager grid,
            Dictionary<BaseActor, Vector3Int> plannedMoves)
        {
            Vector3Int finalTarget = ComputeRushTarget(
                follower,
                rushGoal,
                map,
                grid,
                plannedMoves);

            if (IsValidMove(map, grid, finalTarget, plannedMoves, follower: follower))
                return finalTarget;

            Vector3Int burst = FindBestBurstTile(
                follower,
                historicalTarget,
                map,
                grid,
                plannedMoves);

            if (!IsTileClaimedByAnotherFollower(plannedMoves, burst, follower))
                return burst;

            return FindEmergencyHoldTile(follower, map, grid, plannedMoves, historicalTarget);
        }

        /// <summary>
        /// Advances up to <see cref="MaxRushDistance"/> steps toward
        /// <paramref name="goal"/> using A* (hazards, allies, planned tiles).
        /// </summary>
        /// <summary>
        /// Breadcrumb slot, unless it is a revealed avoidable hazard — then the nearest
        /// safe tile within rush range, if one exists.
        /// </summary>
        static Vector3Int ResolveRushGoal(
            BaseActor follower,
            Vector3Int breadcrumb,
            MapManager map,
            GridManager grid,
            Dictionary<BaseActor, Vector3Int> plannedMoves)
        {
            HazardService hazards = HazardService.Instance;
            TrapService traps = TrapService.Instance;
            bool avoidBreadcrumb = (hazards != null && hazards.IsPathingAvoidCell(breadcrumb))
                || (traps != null && traps.IsPathingAvoidCell(breadcrumb));
            if (!avoidBreadcrumb)
                return breadcrumb;

            Vector3Int safe = FindBestBurstTile(
                follower,
                breadcrumb,
                map,
                grid,
                plannedMoves);

            if (safe != follower.GridPosition
                && IsValidMove(map, grid, safe, plannedMoves, follower: follower))
            {
                return safe;
            }

            return breadcrumb;
        }

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

                    if (IsTileClaimedByAnotherFollower(plannedMoves, cell, follower))
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
                    && !IsTileClaimedByAnotherFollower(plannedMoves, cell, follower)
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

            if (found)
                return best;

            if (IsValidMove(map, grid, follower.GridPosition, plannedMoves, follower: follower))
                return follower.GridPosition;

            return FindNearestHoldTile(follower, map, grid, plannedMoves, breadcrumb);
        }

        /// <summary>
        /// Walkable tile within one step that is not reserved by another follower's plan.
        /// </summary>
        static Vector3Int FindNearestHoldTile(
            BaseActor follower,
            MapManager map,
            GridManager grid,
            Dictionary<BaseActor, Vector3Int> plannedMoves,
            Vector3Int preferNear)
        {
            Vector3Int best = follower.GridPosition;
            float bestDist = float.MaxValue;
            bool found = false;

            foreach (Vector3Int offset in GridManager.EightDirectionOffsets)
            {
                Vector3Int cell = follower.GridPosition + offset;
                if (IsTileClaimedByAnotherFollower(plannedMoves, cell, follower))
                    continue;

                if (!IsValidMove(map, grid, cell, plannedMoves, follower: follower))
                    continue;

                float d = Vector3Int.Distance(cell, preferNear);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = cell;
                    found = true;
                }
            }

            if (found)
                return best;

            return FindEmergencyHoldTile(follower, map, grid, plannedMoves, preferNear);
        }

        /// <summary>
        /// Nearest walkable tile not reserved by another follower's plan and not occupied on the grid.
        /// </summary>
        static Vector3Int FindEmergencyHoldTile(
            BaseActor follower,
            MapManager map,
            GridManager grid,
            Dictionary<BaseActor, Vector3Int> plannedMoves,
            Vector3Int preferNear)
        {
            Vector3Int origin = follower.GridPosition;
            var visited = new HashSet<Vector3Int> { origin };
            var frontier = new Queue<Vector3Int>();
            frontier.Enqueue(origin);

            Vector3Int best = origin;
            float bestDist = float.MaxValue;
            bool found = false;

            while (frontier.Count > 0)
            {
                Vector3Int cell = frontier.Dequeue();
                int steps = Mathf.Max(
                    Mathf.Abs(cell.x - origin.x),
                    Mathf.Abs(cell.y - origin.y));

                if (cell != origin && IsClearRushLandingTile(map, grid, cell, plannedMoves, follower))
                {
                    float d = Vector3Int.Distance(cell, preferNear);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = cell;
                        found = true;
                    }
                }

                if (steps >= MaxRushDistance + 2)
                    continue;

                foreach (Vector3Int offset in GridManager.EightDirectionOffsets)
                {
                    Vector3Int next = cell + offset;
                    if (visited.Add(next) && map.IsWalkable(next))
                        frontier.Enqueue(next);
                }
            }

            if (found)
                return best;

            Debug.LogError(
                $"[RUSH-PLAN] {follower.name} has no free hold tile near {origin}; staying put.");
            return origin;
        }

        static bool IsClearRushLandingTile(
            MapManager map,
            GridManager grid,
            Vector3Int cell,
            Dictionary<BaseActor, Vector3Int> plannedMoves,
            BaseActor actor)
        {
            if (!map.IsWalkable(cell))
                return false;

            if (IsTileClaimedByAnotherFollower(plannedMoves, cell, actor))
                return false;

            IBattleTarget occupant = grid.GetActorAt(cell);
            if (occupant != null && occupant.Owner != actor.gameObject)
                return false;

            HazardService hazards = HazardService.Instance;
            if (hazards != null)
            {
                if (!hazards.CanEnter(cell, actor))
                    return false;

                if (hazards.IsPathingAvoidCell(cell))
                    return false;
            }

            TrapService traps = TrapService.Instance;
            if (traps != null && traps.IsPathingAvoidCell(cell))
                return false;

            return true;
        }

        static void LandFollowerAt(
            GridManager grid,
            MapManager map,
            BaseActor actor,
            Vector3Int dest,
            Dictionary<BaseActor, Vector3Int> plannedMoves)
        {
            if (actor == null || grid == null)
                return;

            if (actor.GridPosition != dest)
            {
                Debug.Log($"[RUSH-LAND] {actor.name} moving {actor.GridPosition} -> {dest}");
                actor.ApplyPositionChange(dest);
            }

            if (TryRegisterOnGrid(grid, actor, actor.GridPosition))
                return;

            Vector3Int hold = FindEmergencyHoldTile(
                actor,
                map,
                grid,
                plannedMoves,
                actor.GridPosition);

            if (hold != actor.GridPosition)
            {
                Debug.LogWarning(
                    $"[RUSH-LAND] {actor.name} grid conflict at {actor.GridPosition}; relocating to {hold}.");
                actor.ApplyPositionChange(hold);
            }

            if (!TryRegisterOnGrid(grid, actor, actor.GridPosition))
            {
                Debug.LogError(
                    $"[RUSH-LAND] {actor.name} could not register at {actor.GridPosition}.");
            }
        }

        static bool TryRegisterOnGrid(GridManager grid, BaseActor actor, Vector3Int cell)
        {
            IBattleTarget occupant = grid.GetActorAt(cell);
            if (occupant != null && occupant.Owner != actor.gameObject)
                return false;

            grid.RegisterActor(cell, actor);
            return grid.GetActorAt(cell)?.Owner == actor.gameObject;
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

            HazardService hazards = HazardService.Instance;
            if (hazards != null)
            {
                if (!hazards.CanEnter(cell, follower))
                    return false;

                if (hazards.IsPathingAvoidCell(cell))
                    return false;
            }

            TrapService traps = TrapService.Instance;
            if (traps != null && traps.IsPathingAvoidCell(cell))
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
            if (IsTileClaimedByAnotherFollower(plannedMoves, destination, follower))
                return false;

            return ComputeRushTarget(follower, destination, map, grid, plannedMoves) == destination;
        }
    }
}
