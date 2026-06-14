using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    public static class DwarfClanJoinService
    {
        public static bool TryJoinClan(BaseActor speaker, DwarfClanDefinition clan, out string failureReason)
        {
            failureReason = null;
            if (!DwarfClanJoinLogic.CanBeginJoinCeremony(out failureReason))
                return false;

            return ApplyJoinClan(speaker, clan, out failureReason);
        }

        public static bool ApplyJoinClan(BaseActor speaker, DwarfClanDefinition clan, out string failureReason)
        {
            failureReason = null;
            if (clan == null || string.IsNullOrWhiteSpace(clan.clanId))
            {
                failureReason = "Clan data is missing.";
                return false;
            }

            if (!DwarfClanJoinLogic.IsSpeakerDwarf(speaker, out _, out failureReason))
                return false;

            if (clan.patronAncestor == null || clan.patronAncestor.abilityTree == null)
            {
                failureReason = "Clan patron tree is missing.";
                return false;
            }

            DwarfClanMembershipRuntime membership = DwarfClanMembershipRuntime.EnsureOn(speaker.gameObject);
            if (!DwarfClanJoinLogic.CanJoin(membership, out failureReason))
                return false;

            DwarfClanWorldState world = DwarfClanWorldState.EnsureInstance();
            world.EnsurePrestige(clan.clanId, clan.startingPrestige);

            SpiritImprintGraph graph = clan.patronAncestor.abilityTree;
            var rootPath = new List<string> { graph.rootNodeId };

            DwarfAncestorPathRuntime pathRuntime = speaker.GetComponent<DwarfAncestorPathRuntime>();
            if (pathRuntime == null)
                pathRuntime = speaker.gameObject.AddComponent<DwarfAncestorPathRuntime>();

            pathRuntime.SetPatronAndPath(clan.patronAncestor, rootPath);
            membership.SetMembership(clan.clanId, 0);
            pathRuntime.TryApplyFromSerializedState();

            Debug.Log(
                $"[DwarfClan] {speaker.DisplayName} joined clan '{clan.clanId}' with patron "
                + $"'{clan.patronAncestor.ancestorId}'.");
            return true;
        }
    }
}
