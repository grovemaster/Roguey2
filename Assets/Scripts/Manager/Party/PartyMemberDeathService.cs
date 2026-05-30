using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Input;
using JRogue.Manager.Grid;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Manager.Party
{
    /// <summary>Central party-member death pipeline (dialog → destroy, queued for multi-death).</summary>
    public static class PartyMemberDeathService
    {
        const string LogPrefix = "[Party:Death]";

        static readonly HashSet<BaseActor> DyingMembers = new HashSet<BaseActor>();
        static readonly Queue<PendingDeath> PendingQueue = new Queue<PendingDeath>();
        static bool _dialogOpen;

        struct PendingDeath
        {
            public BaseActor Member;
            public string DisplayName;
        }

        public static void HandleDeath(BaseActor member)
        {
            if (member == null)
                return;

            if (DyingMembers.Contains(member))
            {
                Debug.Log($"{LogPrefix} Ignored duplicate death for {member.DisplayName}.");
                return;
            }

            PartyManager party = PartyManager.Instance;
            if (party == null || !party.partyMembers.Contains(member))
            {
                Debug.LogWarning($"{LogPrefix} {member.name} is not in partyMembers; skipping death pipeline.");
                return;
            }

            DyingMembers.Add(member);

            if (member.stats != null)
                member.stats.currentHP = Mathf.Max(0, member.stats.currentHP);

            string displayName = member.DisplayName;
            int maxHp = member.stats != null ? member.stats.MaxHP : 0;
            Debug.Log(
                $"{LogPrefix} {member.gameObject.name} ({displayName}) has died. HP 0/{maxHp}.");

            CancelTargetingIfNeeded(member);
            UnregisterFromGrid(member);

            int indexBeforeRemove = party.partyMembers.IndexOf(member);
            party.RemovePartyMember(member);
            int remaining = party.partyMembers.Count;
            Debug.Log($"{LogPrefix} Removed from party. Remaining: {remaining}.");

            PendingQueue.Enqueue(new PendingDeath
            {
                Member = member,
                DisplayName = displayName,
            });

            TryShowNextDialog();
        }

        static void CancelTargetingIfNeeded(BaseActor member)
        {
            InputHandler input = Object.FindAnyObjectByType<InputHandler>();
            if (input == null)
                return;

            if (PartyManager.Instance?.GetActiveMember() == member
                || input.CommandProcessor.CurrentState == InputState.Targeting)
            {
                input.CommandProcessor.ForceExitTargeting();
            }
        }

        static void UnregisterFromGrid(BaseActor member)
        {
            if (GridManager.Instance == null)
                return;

            Vector3Int pos = member.GridPosition;
            IBattleTarget at = GridManager.Instance.GetActorAt(pos);
            if (at != null && at.Owner == member.gameObject)
                GridManager.Instance.UnregisterActor(pos);
        }

        static void TryShowNextDialog()
        {
            if (_dialogOpen || PendingQueue.Count == 0)
                return;

            PendingDeath pending = PendingQueue.Peek();
            _dialogOpen = true;
            PartyMemberDeathDialogUI.EnsureInstance().Show(pending.DisplayName, OnDialogOk);
        }

        static void OnDialogOk()
        {
            _dialogOpen = false;

            if (PendingQueue.Count == 0)
                return;

            PendingDeath pending = PendingQueue.Dequeue();
            DyingMembers.Remove(pending.Member);

            if (pending.Member != null)
                Object.Destroy(pending.Member.gameObject);

            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers.Count == 0)
                Debug.Log($"{LogPrefix} No living party members remain.");

            TryShowNextDialog();
        }

#if UNITY_EDITOR
        public static void ResetForTests()
        {
            DyingMembers.Clear();
            PendingQueue.Clear();
            _dialogOpen = false;
        }
#endif
    }
}
