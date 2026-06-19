using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Floor portal: party member steps onto the portal cell to activate.
    /// Whole party teleports immediately (see <see cref="PortalEntryService"/>).
    /// </summary>
    public sealed class PortalInteractable : IAdjacentMapInteractable
    {
        public Vector3Int Cell { get; }
        public string PortalLinkId { get; }
        public string TargetFloorId { get; }
        public string ListLabel { get; }
        public int SortOrder => 10;

        public PortalInteractable(Vector3Int cell, string portalLinkId, string targetFloorId, string listLabel)
        {
            Cell = cell;
            PortalLinkId = portalLinkId;
            TargetFloorId = targetFloorId;
            ListLabel = listLabel;
        }

        /// <summary>Portals use step-on activation, not adjacent Interact.</summary>
        public bool CanInteract(BaseActor actor) => false;

        public void OpenInteractUI(BaseActor actor) { }

        public bool TryActivatePartyTeleport(BaseActor triggeringMember)
        {
            if (triggeringMember == null)
                return false;

            if (IsBuildingPortal(PortalLinkId) || IsDistrictPortal(PortalLinkId))
            {
                return TownTransitionService.TryTransitionBuilding(
                    PortalLinkId,
                    TargetFloorId,
                    triggeringMember);
            }

            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            if (manager == null)
                return false;

            bool transitioned = manager.TryTransitionPortalForWholeParty(PortalLinkId, TargetFloorId);
            if (transitioned)
            {
                PartyPlayerActionCompletion.CompleteActiveMemberAction(triggeringMember);
                DungeonGenerationLog.Info(
                    $"Portal '{PortalLinkId}' activated at {Cell} — party moved to '{TargetFloorId}'.");
            }

            return transitioned;
        }

        static bool IsBuildingPortal(string portalLinkId) =>
            !string.IsNullOrEmpty(portalLinkId) && portalLinkId.StartsWith("building_");

        static bool IsDistrictPortal(string portalLinkId) =>
            !string.IsNullOrEmpty(portalLinkId) && portalLinkId.StartsWith("district_");
    }
}
