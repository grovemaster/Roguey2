using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Essence;
using JRogue.Manager.Party;
using JRogue.Stats.Racial;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Racial
{
    public static class HumanPriestVowService
    {
        public static bool TrySelectVows(
            BaseActor priest,
            IReadOnlyList<string> vowIds,
            out string failureReason)
        {
            failureReason = null;
            if (priest == null)
            {
                failureReason = "No priest.";
                return false;
            }

            if (!SafeZonePolicyService.TryAllowHumanPriestShrineQuestChange(out failureReason))
                return false;

            HumanPriestCovenantRuntime covenant = priest.GetComponent<HumanPriestCovenantRuntime>();
            if (covenant == null || !covenant.IsCommittedPriest)
            {
                failureReason = "Speaker is not a covenant priest.";
                return false;
            }

            if (covenant.PenanceDebt > 0)
            {
                failureReason = "Repent at the shrine before taking new vows.";
                return false;
            }

            if (vowIds != null && vowIds.Count > 3)
            {
                failureReason = "You may take at most three vows.";
                return false;
            }

            covenant.SetActiveVows(vowIds);
            return true;
        }

        public static bool TryReportVowsAtShrine(BaseActor priest, out string summary, out string failureReason)
        {
            summary = null;
            failureReason = null;
            if (!SafeZonePolicyService.TryAllowHumanPriestShrineQuestChange(out failureReason))
                return false;

            HumanPriestCovenantRuntime covenant = priest?.GetComponent<HumanPriestCovenantRuntime>();
            if (covenant == null || !covenant.IsCommittedPriest)
            {
                failureReason = "Speaker is not a covenant priest.";
                return false;
            }

            int rewarded = 0;
            for (int i = 0; i < covenant.ActiveVows.Count; i++)
            {
                PriestActiveVowState state = covenant.ActiveVows[i];
                if (state == null || state.failed || state.completed)
                    continue;

                if (!PriestVowCatalogService.TryGetVow(state.vowId, out PriestVowDefinition vow))
                    continue;

                if (!HumanPriestVowLogic.MeetsCompletionGates(vow, out failureReason))
                    return false;

                state.completed = true;
                covenant.AddPiety(vow.pietyRewardOnSuccess, state.vowId, $"Vow fulfilled: {vow.displayName}");

                if (!string.IsNullOrWhiteSpace(vow.grantSealId))
                    covenant.GrantSeal(vow.grantSealId);

                rewarded++;
            }

            summary = rewarded > 0
                ? $"The shrine records {rewarded} fulfilled vow(s). Piety is now {covenant.Piety}."
                : "No vows were ready to report.";

            covenant.ClearActiveVows();
            return true;
        }

        public static void NotifyPersonalTaboo(GameObject priestActor, string triggerId) =>
            NotifyPersonalAction(priestActor, triggerId);

        public static void NotifyPersonalAction(GameObject priestActor, string triggerId)
        {
            if (priestActor == null)
                return;

            HumanPriestCovenantRuntime covenant = priestActor.GetComponent<HumanPriestCovenantRuntime>();
            if (covenant == null)
                return;

            for (int i = 0; i < covenant.ActiveVows.Count; i++)
            {
                PriestActiveVowState state = covenant.ActiveVows[i];
                if (state == null || state.failed || state.completed)
                    continue;

                if (!PriestVowCatalogService.TryGetVow(state.vowId, out PriestVowDefinition vow))
                    continue;

                if (vow.scope != PriestVowScope.Personal)
                    continue;

                if (!HumanPriestVowLogic.IsVowBroken(vow, priestActor, triggerId))
                    continue;

                state.failed = true;
                covenant.AddPenance(10, $"Vow broken: {vow.displayName}");
                Debug.Log($"[Priest] Vow broken: {vow.displayName}");
            }
        }

        public static void NotifyPartyAction(BaseActor actor, string triggerId)
        {
            if (actor == null)
                return;

            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                HumanPriestCovenantRuntime covenant = member?.GetComponent<HumanPriestCovenantRuntime>();
                if (covenant == null || !covenant.IsCommittedPriest)
                    continue;

                for (int v = 0; v < covenant.ActiveVows.Count; v++)
                {
                    PriestActiveVowState state = covenant.ActiveVows[v];
                    if (state == null || state.failed || state.completed)
                        continue;

                    if (!PriestVowCatalogService.TryGetVow(state.vowId, out PriestVowDefinition vow))
                        continue;

                    if (vow.scope != PriestVowScope.Party)
                        continue;

                    if (!HumanPriestVowLogic.IsPartyVowBroken(vow, actor, triggerId))
                        continue;

                    state.failed = true;
                    covenant.AddPenance(10, $"Party vow broken: {vow.displayName}");
                    Debug.Log($"[Priest] {member.DisplayName}'s party vow broken: {vow.displayName}");
                }
            }
        }
    }
}
