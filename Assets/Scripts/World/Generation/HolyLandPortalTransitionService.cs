using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Stats;
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
            if (!HolyLandTransitionIds.TryGetHolyLandAdmissionRace(portalLinkId, out Race requiredRace))
                return false;

            PartyManager party = PartyManager.Instance;
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            PartyFloorPresenceService presence = PartyFloorPresenceService.Instance;
            if (party == null || manager == null || presence == null)
                return false;

            List<BaseActor> admitted = PartyFloorPresenceService.CollectLivingByRace(party, requiredRace);
            if (admitted.Count == 0)
            {
                GameLogService.EnsureInstance().Session.Append(
                    $"Only {GetRaceLabel(requiredRace)}s may enter the Holy Land.");
                return false;
            }

            if (!HolyLandControlFilter.TryForceActiveRaceForAdmission(party, requiredRace))
                return false;

            Vector3Int parkAnchor = HolyLandTransitionIds.GetNexusParkAnchorForAdmission(portalLinkId);
            presence.ParkAllExcept(admitted, HolyLandFloorIds.Nexus, parkAnchor);

            bool transitioned = manager.TryActivateFloorForMembers(
                targetFloorId,
                portalLinkId,
                isFirstVisitSpawn: false,
                admitted);

            if (transitioned)
            {
                presence.ParkAllExcept(admitted, HolyLandFloorIds.Nexus, parkAnchor);

                BaseActor active = party.GetActiveMember();
                if (active != null)
                    PartyPlayerActionCompletion.CompleteActiveMemberAction(active);

                party.RefreshCameraFollow();
                TurnManager.Instance?.RefreshPartyPresentation();
                DungeonGenerationLog.Info(
                    $"Holy Land admission — {admitted.Count} {GetRaceLabel(requiredRace)}(s) entered '{targetFloorId}'.");
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
            Debug.Log(
                $"{PortalEntryService.DebugTag} TryExitHolyLand begin link={portalLinkId} target={targetFloorId} " +
                $"actor={triggeringMember?.name ?? "(null)"}");

            if (!HolyLandTransitionIds.TryGetHolyLandExitRace(portalLinkId, out Race requiredRace))
            {
                Debug.LogWarning(
                    $"{PortalEntryService.DebugTag} TryExitHolyLand — '{portalLinkId}' is not a holy land exit portal id.");
                return false;
            }

            PartyManager party = PartyManager.Instance;
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            if (party == null || manager == null)
            {
                Debug.LogWarning(
                    $"{PortalEntryService.DebugTag} TryExitHolyLand — party or floor manager is null.");
                return false;
            }

            PartyFloorPresenceService presence = PartyFloorPresenceService.Instance;
            List<BaseActor> members = PartyFloorPresenceService.CollectLivingByRace(party, requiredRace);
            int livingRaceCount = members.Count;
            if (presence != null)
                members = FilterPresent(members, presence);

            Debug.Log(
                $"{PortalEntryService.DebugTag} TryExitHolyLand race={requiredRace} " +
                $"living={livingRaceCount} present={members.Count}");

            if (members.Count == 0)
            {
                GameLogService.EnsureInstance().Session.Append(
                    $"Only {GetRaceLabel(requiredRace)}s may leave from here.");
                Debug.LogWarning(
                    $"{PortalEntryService.DebugTag} TryExitHolyLand — no present living {requiredRace} members.");
                return false;
            }

            if (!HolyLandControlFilter.TryForceActiveRaceForAdmission(party, requiredRace))
            {
                GameLogService.EnsureInstance().Session.Append(
                    $"Only {GetRaceLabel(requiredRace)}s may leave from here.");
                Debug.LogWarning(
                    $"{PortalEntryService.DebugTag} TryExitHolyLand — could not activate a {requiredRace} party member.");
                return false;
            }

            bool transitioned = manager.TryActivateFloorForMembers(
                targetFloorId,
                portalLinkId,
                isFirstVisitSpawn: false,
                members);

            if (!transitioned)
            {
                Debug.LogWarning(
                    $"{PortalEntryService.DebugTag} TryExitHolyLand — TryActivateFloorForMembers failed for '{targetFloorId}'.");
                return false;
            }

            if (presence != null)
            {
                Vector3Int reunifyAnchor = HolyLandTransitionIds.GetNexusReturnAnchorForExit(portalLinkId);
                if (targetFloorId == HolyLandFloorIds.Nexus && presence.HasParkedMembers)
                {
                    presence.UnparkAll();
                    PartyFormationSpawnProfile profile =
                        manager.TryFindDefinition(targetFloorId)?.FormationProfile;
                    PartySpawnService.TrySpawnFormationAtAnchor(
                        reunifyAnchor,
                        profile,
                        CollectAllLiving(party),
                        out _);
                    party.SnapHistoryToCurrentPositions();
                }
                else
                {
                    presence.ParkAllExcept(members, HolyLandFloorIds.Nexus, reunifyAnchor);
                }
            }

            PartyPlayerActionCompletion.CompleteActiveMemberAction(triggeringMember);
            party.RefreshCameraFollow();
            TurnManager.Instance?.RefreshPartyPresentation();
            Debug.Log($"{PortalEntryService.DebugTag} TryExitHolyLand SUCCESS — returned to '{targetFloorId}'.");
            DungeonGenerationLog.Info($"Holy Land exit — returned to '{targetFloorId}'.");
            return true;
        }

        public static bool TryTransitionHolyLandBuilding(
            string portalLinkId,
            string targetFloorId,
            BaseActor triggeringMember)
        {
            if (!HolyLandTransitionIds.TryGetHolyLandBuildingRace(portalLinkId, out Race requiredRace))
                return false;

            PartyManager party = PartyManager.Instance;
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            PartyFloorPresenceService presence = PartyFloorPresenceService.Instance;
            if (party == null || manager == null || presence == null)
                return false;

            List<BaseActor> travelers = PartyFloorPresenceService.CollectLivingByRace(party, requiredRace);
            travelers = FilterPresent(travelers, presence);
            if (travelers.Count == 0)
            {
                GameLogService.EnsureInstance().Session.Append(
                    $"Only {GetRaceLabel(requiredRace)}s may enter.");
                return false;
            }

            if (!HolyLandControlFilter.TryForceActiveRaceForAdmission(party, requiredRace))
                return false;

            Vector3Int parkAnchor = HolyLandTransitionIds.GetNexusParkAnchorForRace(requiredRace);
            presence.ParkAllExcept(travelers, HolyLandFloorIds.Nexus, parkAnchor);

            bool transitioned = manager.TryActivateFloorForMembers(
                targetFloorId,
                portalLinkId,
                isFirstVisitSpawn: false,
                travelers);

            if (!transitioned)
                return false;

            presence.ParkAllExcept(travelers, HolyLandFloorIds.Nexus, parkAnchor);
            PartyPlayerActionCompletion.CompleteActiveMemberAction(triggeringMember);
            party.RefreshCameraFollow();
            TurnManager.Instance?.RefreshPartyPresentation();
            DungeonGenerationLog.Info(
                $"Holy Land building — {travelers.Count} {GetRaceLabel(requiredRace)}(s) moved to '{targetFloorId}'.");
            return true;
        }

        static string GetRaceLabel(Race race) =>
            race switch
            {
                Race.Barbarian => "Barbarian",
                Race.Elf => "Elf",
                _ => race.ToString()
            };

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
