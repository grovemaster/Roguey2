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
            occupiedCells = new List<Vector3Int>();
            PartyManager party = PartyManager.Instance;
            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;
            if (party == null || map == null || grid == null)
                return false;

            List<BaseActor> living = CollectLivingMembers(party);
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
            {
                BaseActor actor = living[i];
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
            return true;
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
            for (int radius = 1; radius <= 2; radius++)
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

            return AllValid(resolved, map, grid, partyMembers);
        }

        static bool AllValid(
            List<Vector3Int> cells,
            MapManager map,
            GridManager grid,
            List<BaseActor> partyMembers)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3Int cell = cells[i];
                if (!map.IsWalkable(cell))
                    return false;

                IBattleTarget occupant = grid.GetActorAt(cell);
                if (occupant == null)
                    continue;

                bool partyOccupant = false;
                for (int p = 0; p < partyMembers.Count; p++)
                {
                    if (partyMembers[p] != null && occupant.Owner == partyMembers[p].gameObject)
                    {
                        partyOccupant = true;
                        break;
                    }
                }

                if (!partyOccupant)
                    return false;
            }

            return true;
        }
    }
}
