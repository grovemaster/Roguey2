using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.World.Generation
{
    public static class PartySpawnService
    {
        public static bool TrySpawnFormationAtAnchor(
            Vector3Int anchor,
            PartyFormationSpawnProfile profile,
            out List<Vector3Int> occupiedCells)
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
            {
                occupiedCells = new List<Vector3Int>();
                return false;
            }

            return TrySpawnFormationAtAnchor(anchor, profile, CollectLivingMembers(party), out occupiedCells);
        }

        public static bool TrySpawnFormationAtAnchor(
            Vector3Int anchor,
            PartyFormationSpawnProfile profile,
            IReadOnlyList<BaseActor> members,
            out List<Vector3Int> occupiedCells)
        {
            occupiedCells = new List<Vector3Int>();
            PartyManager party = PartyManager.Instance;
            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;
            if (party == null || map == null || grid == null || members == null || members.Count == 0)
                return false;

            List<BaseActor> living = new List<BaseActor>(members.Count);
            for (int i = 0; i < members.Count; i++)
            {
                BaseActor member = members[i];
                if (member != null)
                    living.Add(member);
            }

            if (living.Count == 0)
                return false;

            profile ??= PartyFormationSpawnProfile.CreateDefaultRuntime();
            if (!profile.TryGetOffsetsForCount(living.Count, out Vector3Int[] offsets))
            {
                offsets = new Vector3Int[living.Count];
                for (int i = 0; i < offsets.Length; i++)
                    offsets[i] = i == 0 ? Vector3Int.zero : new Vector3Int(0, -i, 0);
            }

            var targets = new List<Vector3Int>(living.Count);
            for (int i = 0; i < living.Count; i++)
            {
                Vector3Int offset = i < offsets.Length ? offsets[i] : Vector3Int.zero;
                targets.Add(anchor + offset);
            }

            if (!TryResolveBlockedTargets(targets, map, grid, living, out List<Vector3Int> resolved))
                return false;

            for (int i = 0; i < living.Count; i++)
                UnregisterFromGrid(living[i], grid);

            for (int i = 0; i < living.Count; i++)
            {
                BaseActor actor = living[i];
                JRogue.View.PlayerRaceWorldSpriteApplier.Apply(actor.gameObject);
                GridMover mover = actor.GetComponent<GridMover>();
                if (mover == null)
                    continue;

                mover.InitializeAtGridAnchor(resolved[i]);
                if (!mover.enabled)
                    mover.enabled = true;

                occupiedCells.Add(resolved[i]);
            }

            party.SnapHistoryToCurrentPositions();
            party.RefreshCameraFollow();
            party.InitializeRosterAfterDeferredSpawn();
            PortalEntryService.Instance?.SubscribePartyMembers();
            return true;
        }

        /// <summary>
        /// Places a newly recruited member on a nearby open tile without repositioning the rest of the party.
        /// </summary>
        public static bool TryPlaceRecruitNearParty(BaseActor recruit, PartyManager party)
        {
            if (recruit == null || party?.partyMembers == null)
                return false;

            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;
            if (map == null || grid == null)
                return false;

            GridMover mover = recruit.GetComponent<GridMover>();
            if (mover == null)
                return false;

            var searchOrigins = new List<Vector3Int>();
            BaseActor leader = party.GetFormationLeader();
            if (leader != null && leader != recruit)
                searchOrigins.Add(leader.GridPosition);

            BaseActor active = party.GetActiveMember();
            if (active != null && active != recruit && active != leader)
                searchOrigins.Add(active.GridPosition);

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member == recruit)
                    continue;

                Vector3Int pos = member.GridPosition;
                if (!searchOrigins.Contains(pos))
                    searchOrigins.Add(pos);
            }

            if (searchOrigins.Count == 0
                || !TryFindRecruitPlacementCell(searchOrigins, recruit, map, grid, out Vector3Int cell))
                return false;

            JRogue.View.PlayerRaceWorldSpriteApplier.Apply(recruit.gameObject);
            mover.InitializeAtGridAnchor(cell);
            if (!mover.enabled)
                mover.enabled = true;

            party.SnapHistoryToCurrentPositions();
            return true;
        }

        static bool TryFindRecruitPlacementCell(
            List<Vector3Int> origins,
            BaseActor recruit,
            MapManager map,
            GridManager grid,
            out Vector3Int cell)
        {
            cell = default;
            var visited = new HashSet<Vector3Int>();

            for (int radius = 1; radius <= 8; radius++)
            {
                for (int o = 0; o < origins.Count; o++)
                {
                    Vector3Int origin = origins[o];
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius)
                                continue;

                            Vector3Int candidate = origin + new Vector3Int(dx, dy, 0);
                            if (!visited.Add(candidate))
                                continue;

                            if (!IsOpenRecruitCell(candidate, recruit, map, grid))
                                continue;

                            cell = candidate;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        static bool IsOpenRecruitCell(
            Vector3Int cell,
            BaseActor recruit,
            MapManager map,
            GridManager grid)
        {
            if (!map.IsWalkable(cell))
                return false;

            IBattleTarget occupant = grid.GetActorAt(cell);
            if (occupant == null)
                return true;

            return occupant.Owner == recruit.gameObject;
        }

        static void UnregisterFromGrid(BaseActor actor, GridManager grid)
        {
            if (actor == null || grid == null)
                return;

            GridMover mover = actor.GetComponent<GridMover>();
            if (mover == null)
                return;

            IBattleTarget self = actor.GetComponent<IBattleTarget>();
            IGridFootprint footprint = actor.GetComponent<IGridFootprint>();
            if (footprint != null)
                grid.UnregisterFootprint(self);
            else
                grid.UnregisterActor(mover.GridPosition);
        }

        static List<BaseActor> CollectLivingMembers(PartyManager party)
        {
            var living = new List<BaseActor>();
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member != null)
                    living.Add(member);
            }

            return living;
        }

        static bool TryResolveBlockedTargets(
            List<Vector3Int> targets,
            MapManager map,
            GridManager grid,
            List<BaseActor> partyMembers,
            out List<Vector3Int> resolved)
        {
            resolved = new List<Vector3Int>(targets);
            if (AllValid(resolved, map, grid, partyMembers))
                return true;

            Vector3Int anchor = targets[0];
            for (int radius = 1; radius <= 12; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius)
                            continue;

                        Vector3Int shift = new Vector3Int(dx, dy, 0);
                        resolved.Clear();
                        for (int i = 0; i < targets.Count; i++)
                            resolved.Add(targets[i] + shift);

                        if (AllValid(resolved, map, grid, partyMembers))
                            return true;
                    }
                }
            }

            return false;
        }

        static bool AllValid(
            List<Vector3Int> cells,
            MapManager map,
            GridManager grid,
            List<BaseActor> partyMembers)
        {
            var used = new HashSet<Vector3Int>();
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3Int cell = cells[i];
                if (!used.Add(cell))
                    return false;

                if (!map.IsWalkable(cell))
                    return false;

                IBattleTarget occupant = grid.GetActorAt(cell);
                if (occupant == null)
                    continue;

                BaseActor expected = i < partyMembers.Count ? partyMembers[i] : null;
                if (expected != null && occupant.Owner == expected.gameObject)
                    continue;

                return false;
            }

            return true;
        }
    }
}
