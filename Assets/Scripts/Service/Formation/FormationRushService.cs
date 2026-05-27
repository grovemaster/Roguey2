using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Hazards;
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

                Vector3Int finalTarget = follower.GridPosition;
                float dist = Vector3Int.Distance(follower.GridPosition, historicalTarget);

                if (dist <= MaxRushDistance)
                {
                    finalTarget = historicalTarget;
                }
                else
                {
                    // Burst-step toward the breadcrumb, capped at MaxRushDistance.
                    Vector3 direction =
                        ((Vector3)(historicalTarget - follower.GridPosition)).normalized;
                    finalTarget = Vector3Int.RoundToInt(
                        (Vector3)follower.GridPosition + (direction * MaxRushDistance));
                }

                if (IsValidMove(map, grid, finalTarget, plannedMoves, follower: follower))
                {
                    plannedMoves.Add(follower, finalTarget);
                    Debug.Log($"[RUSH-PLAN] {follower.name} accepted target {finalTarget}");
                }
                else
                {
                    // Couldn't take the ideal slot — search neighbors for the
                    // best alternative within burst range.
                    Vector3Int bestSmartTile = follower.GridPosition;
                    float bestDistToBreadcrumb = float.MaxValue;
                    bool foundSpot = false;

                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if (x == 0 && y == 0) continue;
                            Vector3Int neighbor = finalTarget + new Vector3Int(x, y, 0);
                            if (Vector3Int.Distance(follower.GridPosition, neighbor)
                                > MaxRushDistance + 0.5f)
                            {
                                continue;
                            }

                            if (IsValidMove(map, grid, neighbor, plannedMoves, follower: follower))
                            {
                                float d = Vector3Int.Distance(neighbor, historicalTarget);
                                if (d < bestDistToBreadcrumb)
                                {
                                    bestDistToBreadcrumb = d;
                                    bestSmartTile = neighbor;
                                    foundSpot = true;
                                }
                            }
                        }
                    }
                    plannedMoves.Add(follower, foundSpot ? bestSmartTile : follower.GridPosition);
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
                    HazardService.Instance?.NotifyActorMovedOntoCell(actor);
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
    }
}
