using JRogue.Actors;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "OtherInteractableOn",
        menuName = "JRogue/Interactables/Preconditions/Other Interactable On")]
    public sealed class OtherInteractableOnPrecondition : InteractablePrecondition
    {
        public InteractableTileId requiredInteractableId = InteractableTileId.None;

        public override bool Evaluate(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source,
            out string failureReason)
        {
            failureReason = null;
            InteractableTileService service = InteractableTileService.Instance;
            if (service == null)
            {
                failureReason = "No interactable service.";
                return false;
            }

            if (!service.TryGetInstanceById(requiredInteractableId, out InteractableTileInstance other))
            {
                failureReason = $"Required interactable {requiredInteractableId} is not registered.";
                return false;
            }

            if (!other.IsOn)
            {
                failureReason = $"{requiredInteractableId} is not activated.";
                return false;
            }

            return true;
        }
    }
}
