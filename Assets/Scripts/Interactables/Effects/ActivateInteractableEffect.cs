using JRogue.Actors;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "ActivateInteractable",
        menuName = "JRogue/Interactables/Effects/Activate Interactable")]
    public sealed class ActivateInteractableEffect : InteractableEffect
    {
        public InteractableTileId targetInteractableId = InteractableTileId.None;

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            if (service == null)
                return;

            service.ActivateById(targetInteractableId, InteractableActivationSource.Scripted, bumper);
        }
    }
}
