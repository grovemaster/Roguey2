using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.UI.Gameplay;
using JRogue.World.Town;
using UnityEngine;

namespace JRogue.World.Generation
{
    public static class HolyLandPortalTransitionService
    {
        public static bool TryEnterHolyLand(
            string portalLinkId,
            string targetFloorId,
            BaseActor triggeringMember)
        {
            if (!HolyLandTransitionIds.IsHolyLandAdmission(portalLinkId))
                return false;

            PartyManager party = PartyManager.Instance;
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            PartyFloorPresenceService presence = PartyFloorPresenceService.Instance;
            if (party == null || manager == null || presence == null)
                return false;

            List<BaseActor> barbarians = PartyFloorPresenceService.CollectLivingBarbarians(party);
            if (barbarians.Count == 0)
            {
                GameLogService.EnsureInstance().Session.Append("Only Barbarians may enter the Holy Land.");
                return false;
            }

            if (!HolyLandControlFilter.TryForceActiveBarbarianForAdmission(party))
                return false;

            presence.ParkAllExcept(
                barbarians,
                HolyLandFloorIds.Nexus,
                HolyLandNexusLayout.HolyLandReturnAnchor);

            bool transitioned = manager.TryActivateFloorForMembers(
                targetFloorId,
                portalLinkId,
                isFirstVisitSpawn: false,
                barbarians);

            if (transitioned)
            {
                presence.ParkAllExcept(
                    barbarians,
                    HolyLandFloorIds.Nexus,
                    HolyLandNexusLayout.HolyLandReturnAnchor);

                BaseActor active = party.GetActiveMember();
                if (active != null)
                    PartyPlayerActionCompletion.CompleteActiveMemberAction(active);

                party.RefreshCameraFollow();
                TurnManager.Instance?.RefreshPartyPresentation();
                DungeonGenerationLog.Info(
                    $"Holy Land admission — {barbarians.Count} Barbarian(s) entered '{targetFloorId}'.");
            }
            else
            {
                presence.UnparkAll();
            }

            return transitioned;
        }

        public static bool TryExitHolyLand(
            string portalLinkId,
            string targetFloorId,
            BaseActor triggeringMember)
        {
            if (!HolyLandTransitionIds.IsHolyLandExit(portalLinkId))
                return false;

            PartyManager party = PartyManager.Instance;
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            PartyFloorPresenceService presence = PartyFloorPresenceService.Instance;
            if (party == null || manager == null || presence == null)
                return false;

            List<BaseActor> barbarians = PartyFloorPresenceService.CollectLivingBarbarians(party);
            barbarians = FilterPresent(barbarians, presence);
            if (barbarians.Count == 0)
                return false;

            bool transitioned = manager.TryActivateFloorForMembers(
                targetFloorId,
                portalLinkId,
                isFirstVisitSpawn: false,
                barbarians);

            if (!transitioned)
                return false;

            if (presence.HasParkedMembers)
            {
                presence.UnparkAll();
                Vector3Int anchor = HolyLandNexusLayout.HolyLandReturnAnchor;
                PartyFormationSpawnProfile profile =
                    manager.TryFindDefinition(targetFloorId)?.FormationProfile;
                PartySpawnService.TrySpawnFormationAtAnchor(anchor, profile, CollectAllLiving(party), out _);
                party.SnapHistoryToCurrentPositions();
            }

            PartyPlayerActionCompletion.CompleteActiveMemberAction(triggeringMember);
            party.RefreshCameraFollow();
            DungeonGenerationLog.Info($"Holy Land exit — returned to '{targetFloorId}'.");
            return true;
        }

        static List<BaseActor> FilterPresent(List<BaseActor> members, PartyFloorPresenceService presence)
        {
            var present = new List<BaseActor>(members.Count);
            for (int i = 0; i < members.Count; i++)
            {
                BaseActor member = members[i];
                if (member != null && !presence.IsParked(member))
                    present.Add(member);
            }

            return present;
        }

        static List<BaseActor> CollectAllLiving(PartyManager party)
        {
            var living = new List<BaseActor>();
            if (party?.partyMembers == null)
                return living;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member != null && member.stats != null && member.stats.currentHP > 0)
                    living.Add(member);
            }

            return living;
        }
    }
}
