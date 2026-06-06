using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.Dialog
{
    public static class NpcTalkFacingUtility
    {
        public static bool IsFacingToward(BaseActor actor, Vector3Int targetCell)
        {
            if (actor == null)
                return false;

            Vector3Int actorCell = actor.GridPosition;
            Vector3Int delta = targetCell - actorCell;
            if (!MapInteractOrthogonal.IsOrthogonallyAdjacent(actorCell, targetCell))
                return false;

            return actor.currentFacing == DirectionToward(delta);
        }

        public static void FaceToward(BaseActor actor, Vector3Int targetCell)
        {
            if (actor == null)
                return;

            Vector3Int delta = targetCell - actor.GridPosition;
            if (delta == Vector3Int.zero)
                return;

            actor.currentFacing = DirectionToward(delta);
        }

        public static FacingDirection DirectionToward(Vector3Int delta)
        {
            if (delta.x > 0)
                return FacingDirection.East;
            if (delta.x < 0)
                return FacingDirection.West;
            if (delta.y > 0)
                return FacingDirection.North;
            if (delta.y < 0)
                return FacingDirection.South;

            return FacingDirection.North;
        }
    }

    public static class NpcTalkInteraction
    {
        public const string LogPrefix = "[NpcTalk]";

        static readonly List<Vector3Int> NeighborBuffer = new List<Vector3Int>(4);
        static readonly List<INpcTalkTarget> CandidateBuffer = new List<INpcTalkTarget>(4);

        public static bool TryTalkFacing(BaseActor actor)
        {
            if (actor == null)
                return false;

            CollectAdjacentTalkTargets(actor, CandidateBuffer);
            if (CandidateBuffer.Count == 0)
            {
                Debug.Log($"{LogPrefix} No NPC on an adjacent cell.");
                return false;
            }

            INpcTalkTarget matched = ResolveTalkTarget(actor, CandidateBuffer);
            if (matched == null)
            {
                if (CandidateBuffer.Count > 1)
                    Debug.Log($"{LogPrefix} Multiple adjacent NPCs — face the one you want to talk to.");
                else
                    Debug.Log($"{LogPrefix} Could not resolve talk target.");
                return false;
            }

            NpcTalkFacingUtility.FaceToward(actor, matched.Cell);
            matched.BeginDialog(actor);
            return true;
        }

        static INpcTalkTarget ResolveTalkTarget(BaseActor actor, List<INpcTalkTarget> candidates)
        {
            if (candidates.Count == 1)
                return candidates[0];

            INpcTalkTarget matched = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                INpcTalkTarget candidate = candidates[i];
                if (!NpcTalkFacingUtility.IsFacingToward(actor, candidate.Cell))
                    continue;

                if (matched != null)
                    return null;

                matched = candidate;
            }

            return matched;
        }

        static void CollectAdjacentTalkTargets(BaseActor actor, List<INpcTalkTarget> results)
        {
            results.Clear();
            Vector3Int actorCell = actor.GridPosition;

            GridManager grid = GridManager.Instance;
            if (grid != null)
            {
                MapInteractOrthogonal.CopyNeighborCells(actorCell, NeighborBuffer);
                for (int i = 0; i < NeighborBuffer.Count; i++)
                {
                    IBattleTarget occupant = grid.GetActorAt(NeighborBuffer[i]);
                    TryAddTalkTarget(results, occupant?.Owner);
                }
            }

            if (results.Count > 0)
                return;

            NpcController[] npcs = Object.FindObjectsByType<NpcController>();
            for (int i = 0; i < npcs.Length; i++)
            {
                NpcController npc = npcs[i];
                if (npc == null)
                    continue;

                if (!MapInteractOrthogonal.IsOrthogonallyAdjacent(actorCell, npc.GridPosition))
                    continue;

                TryAddTalkTarget(results, npc.gameObject);
            }
        }

        static void TryAddTalkTarget(List<INpcTalkTarget> results, GameObject owner)
        {
            if (owner == null)
                return;

            INpcTalkTarget talkTarget = owner.GetComponent<SpiritImprintShamanNpcController>() as INpcTalkTarget
                ?? owner.GetComponent<ShopNpcController>() as INpcTalkTarget
                ?? owner.GetComponent<NpcController>() as INpcTalkTarget;
            if (talkTarget == null)
                return;

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Actor == talkTarget.Actor)
                    return;
            }

            results.Add(talkTarget);
        }
    }
}
