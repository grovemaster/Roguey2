using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Generation
{
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

        public bool CanInteract(BaseActor actor) =>
            actor != null && DungeonFloorInstanceManager.Instance != null;

        public void OpenInteractUI(BaseActor actor)
        {
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            if (manager == null)
                return;

            bool transitioned = manager.TryTransitionPortal(PortalLinkId, TargetFloorId);
            if (transitioned)
                PartyPlayerActionCompletion.CompleteActiveMemberAction(actor);
        }
    }
}
