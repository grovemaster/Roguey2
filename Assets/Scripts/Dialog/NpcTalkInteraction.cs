using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using JRogue.UI.Gameplay;
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

        /// <summary>
        /// Customer on <paramref name="customerRowY"/> faces the clerk across a counter tile directly north.
        /// </summary>
        public static bool IsFacingAcrossShopCounter(
            BaseActor actor,
            Vector3Int npcCell,
            int customerRowY,
            int counterRowY)
        {
            if (actor == null)
                return false;

            Vector3Int actorCell = actor.GridPosition;
            if (actorCell.y != customerRowY)
                return false;

            Vector3Int counterCell = new Vector3Int(actorCell.x, counterRowY, 0);
            if (!JRogue.World.Generation.ShopCounterService.IsCounterCell(counterCell))
                return false;

            if (npcCell.y <= counterRowY)
                return false;

            Vector3Int delta = npcCell - actorCell;
            if (delta == Vector3Int.zero)
                return false;

            return actor.currentFacing == DirectionToward(delta);
        }

        /// <summary>Customer row + counter tile north + clerk behind counter (facing not required).</summary>
        public static bool IsEligibleForCounterTalk(
            BaseActor actor,
            Vector3Int npcCell,
            int customerRowY,
            int counterRowY)
        {
            if (actor == null)
                return false;

            Vector3Int actorCell = actor.GridPosition;
            if (actorCell.y != customerRowY)
                return false;

            Vector3Int counterCell = new Vector3Int(actorCell.x, counterRowY, 0);
            if (!JRogue.World.Generation.ShopCounterService.IsCounterCell(counterCell))
                return false;

            return npcCell.y > counterRowY;
        }

        /// <summary>
        /// Customer on <paramref name="customerCell"/> faces the NPC two steps away with the counter in between.
        /// </summary>
        public static bool IsFacingAcrossCounter(
            BaseActor actor,
            Vector3Int npcCell,
            Vector3Int counterCell,
            Vector3Int customerCell)
        {
            if (actor == null)
                return false;

            Vector3Int actorCell = actor.GridPosition;
            if (actorCell != customerCell)
                return false;

            Vector3Int delta = npcCell - actorCell;
            if (delta.x != 0 && delta.y != 0)
                return false;

            int manhattan = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
            if (manhattan != 2)
                return false;

            Vector3Int step = new Vector3Int(
                delta.x != 0 ? delta.x / Mathf.Abs(delta.x) : 0,
                delta.y != 0 ? delta.y / Mathf.Abs(delta.y) : 0,
                0);
            if (actorCell + step != counterCell)
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
            CollectCounterTalkTargets(actor, CandidateBuffer);
            if (CandidateBuffer.Count == 0)
            {
                Debug.Log($"{LogPrefix} No NPC in talk range.");
                return false;
            }

            if (CandidateBuffer.Count == 1)
            {
                INpcTalkTarget only = CandidateBuffer[0];
                NpcTalkFacingUtility.FaceToward(actor, only.Cell);
                only.BeginDialog(actor);
                return true;
            }

            INpcTalkTarget matched = ResolveTalkTarget(actor, CandidateBuffer);
            if (matched != null)
            {
                NpcTalkFacingUtility.FaceToward(actor, matched.Cell);
                matched.BeginDialog(actor);
                return true;
            }

            NpcTalkPickerModalUI.EnsureInstance().Show(actor, CandidateBuffer, selected =>
            {
                if (selected == null)
                    return;

                NpcTalkFacingUtility.FaceToward(actor, selected.Cell);
                selected.BeginDialog(actor);
            });
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
                if (!IsFacingTalkTarget(actor, candidate))
                    continue;

                if (matched != null)
                    return null;

                matched = candidate;
            }

            return matched;
        }

        static bool IsFacingTalkTarget(BaseActor actor, INpcTalkTarget candidate)
        {
            if (NpcTalkFacingUtility.IsFacingToward(actor, candidate.Cell))
                return true;

            NpcCounterTalkBinding binding = candidate.Actor?.GetComponent<NpcCounterTalkBinding>();
            return binding != null && binding.TryGetCounterTalk(actor, out _);
        }

        static void CollectCounterTalkTargets(BaseActor actor, List<INpcTalkTarget> results)
        {
            NpcCounterTalkBinding[] bindings = Object.FindObjectsByType<NpcCounterTalkBinding>();
            for (int i = 0; i < bindings.Length; i++)
            {
                NpcCounterTalkBinding binding = bindings[i];
                if (binding == null || !binding.IsEligibleForCounterTalk(actor))
                    continue;

                TryAddTalkTarget(results, binding.Npc.gameObject);
            }
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
                ?? owner.GetComponent<InnkeeperNpcController>() as INpcTalkTarget
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
