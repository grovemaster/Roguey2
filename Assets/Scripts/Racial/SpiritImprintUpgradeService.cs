using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Racial
{
    public static class SpiritImprintUpgradeService
    {
        public static IReadOnlyList<SpiritImprintNodeData> GetNextOffers(BaseActor speaker)
        {
            if (!SpiritImprintUpgradeLogic.IsSpeakerEligible(speaker, out SpiritImprintRuntime runtime, out _))
                return System.Array.Empty<SpiritImprintNodeData>();

            return SpiritImprintUpgradeLogic.GetNextNodeOffers(runtime);
        }

        public static bool CanAffordNode(BaseActor speaker, SpiritImprintNodeData node)
        {
            if (speaker == null || node == null)
                return false;

            GameStoryFlagService.EnsureInstance();
            PartyManager party = PartyManager.Instance;
            IReadOnlyList<BaseActor> members = party != null ? party.partyMembers : null;
            List<BaseActor> ordered = SpiritImprintUpgradeLogic.OrderPartyMembersForPayment(members, speaker);
            return SpiritImprintUpgradeLogic.CanAfford(
                node.unlockCost,
                ordered,
                GameStoryFlagService.Instance,
                out _);
        }

        public static bool TryExecuteUpgrade(BaseActor speaker, string childNodeId, out string failureReason)
        {
            failureReason = null;
            if (!SpiritImprintUpgradeLogic.IsSpeakerEligible(speaker, out SpiritImprintRuntime runtime, out string rejectLine))
            {
                failureReason = rejectLine;
                return false;
            }

            GameStoryFlagService.EnsureInstance();
            PartyManager party = PartyManager.Instance;
            IReadOnlyList<BaseActor> members = party != null ? party.partyMembers : null;
            return SpiritImprintUpgradeLogic.TryExecuteUpgrade(
                speaker,
                runtime,
                childNodeId,
                members,
                GameStoryFlagService.Instance,
                out failureReason);
        }
    }
}
