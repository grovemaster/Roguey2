using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Generation
{
    public static class TownBuildingDoorInteraction
    {
        public const string LogPrefix = "[BuildingDoor]";

        static readonly List<Vector3Int> NeighborBuffer = new List<Vector3Int>(4);
        static readonly List<TownBuildingDoorInteractable> CandidateBuffer = new List<TownBuildingDoorInteractable>(4);

        public static bool TryUseFacing(BaseActor actor)
        {
            if (actor == null)
                return false;

            CollectAdjacentDoors(actor, CandidateBuffer);
            if (CandidateBuffer.Count == 0)
                return false;

            TownBuildingDoorInteractable matched = ResolveDoor(actor, CandidateBuffer);
            if (matched == null)
            {
                if (CandidateBuffer.Count > 1)
                    Debug.Log($"{LogPrefix} Multiple adjacent doors — face the one you want to enter.");
                return false;
            }

            matched.OpenInteractUI(actor);
            return true;
        }

        static TownBuildingDoorInteractable ResolveDoor(
            BaseActor actor,
            List<TownBuildingDoorInteractable> candidates)
        {
            if (candidates.Count == 1)
                return candidates[0];

            TownBuildingDoorInteractable matched = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                TownBuildingDoorInteractable candidate = candidates[i];
                if (!NpcTalkFacingUtility.IsFacingToward(actor, candidate.Cell))
                    continue;

                if (matched != null)
                    return null;

                matched = candidate;
            }

            return matched;
        }

        static void CollectAdjacentDoors(BaseActor actor, List<TownBuildingDoorInteractable> results)
        {
            results.Clear();
            AdjacentMapInteractableService mapInteract = AdjacentMapInteractableService.Instance;
            if (mapInteract == null)
                return;

            MapInteractOrthogonal.CopyNeighborCells(actor.GridPosition, NeighborBuffer);
            for (int i = 0; i < NeighborBuffer.Count; i++)
            {
                if (!mapInteract.TryGetAtCell(NeighborBuffer[i], out IAdjacentMapInteractable interactable))
                    continue;

                if (interactable is not TownBuildingDoorInteractable door || !door.CanInteract(actor))
                    continue;

                for (int j = 0; j < results.Count; j++)
                {
                    if (results[j].Cell == door.Cell)
                        goto nextNeighbor;
                }

                results.Add(door);

                nextNeighbor: ;
            }
        }
    }
}
