using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.UI.Gameplay;
using JRogue.World.Town;
using UnityEngine;

namespace JRogue.World.Generation
{
    public static class TownTransitionService
    {
        public static bool IsTransitionInProgress =>
            TownTransitionCurtainUI.Instance != null && TownTransitionCurtainUI.Instance.IsBusy;

        public static bool TryTransitionBuilding(string portalLinkId, string targetFloorId, BaseActor triggeringMember)
        {
            if (triggeringMember == null
                || string.IsNullOrEmpty(portalLinkId)
                || string.IsNullOrEmpty(targetFloorId))
            {
                return false;
            }

            if (GameplayModalGate.BlocksFloorGameplay)
                return false;

            if (IsTransitionInProgress)
                return false;

            if (HolyLandTransitionIds.IsHolyLandBuildingPortal(portalLinkId))
                return HolyLandPortalTransitionService.TryTransitionHolyLandBuilding(
                    portalLinkId,
                    targetFloorId,
                    triggeringMember);

            TownTransitionCurtainUI curtain = TownTransitionCurtainUI.EnsureInstance();
            return curtain.RunTransition(() => ExecuteTransition(portalLinkId, targetFloorId));
        }

        static bool ExecuteTransition(string portalLinkId, string targetFloorId)
        {
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[TownTransition] DungeonFloorInstanceManager missing.");
                return false;
            }

            bool transitioned = manager.TryTransitionPortalForWholeParty(portalLinkId, targetFloorId);
            if (transitioned)
                PartyManager.Instance?.RefreshCameraFollow();

            return transitioned;
        }
    }
}
