using JRogue.Actors;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Exterior building door: opened with Confirm while orthogonally adjacent (and facing when multiple doors).
    /// </summary>
    public sealed class TownBuildingDoorInteractable : IAdjacentMapInteractable
    {
        public Vector3Int Cell { get; }
        public string PortalLinkId { get; }
        public string TargetFloorId { get; }
        public string ListLabel { get; }
        public int SortOrder => 5;

        public TownBuildingDoorInteractable(
            Vector3Int cell,
            string portalLinkId,
            string targetFloorId,
            string listLabel)
        {
            Cell = cell;
            PortalLinkId = portalLinkId ?? string.Empty;
            TargetFloorId = targetFloorId ?? string.Empty;
            ListLabel = string.IsNullOrEmpty(listLabel) ? "Building door" : listLabel;
        }

        public bool CanInteract(BaseActor actor) =>
            actor != null && MapInteractOrthogonal.IsOrthogonallyAdjacent(actor.GridPosition, Cell);

        public void OpenInteractUI(BaseActor actor)
        {
            if (actor == null)
                return;

            Dialog.NpcTalkFacingUtility.FaceToward(actor, Cell);
            TownTransitionService.TryTransitionBuilding(PortalLinkId, TargetFloorId, actor);
        }
    }
}
