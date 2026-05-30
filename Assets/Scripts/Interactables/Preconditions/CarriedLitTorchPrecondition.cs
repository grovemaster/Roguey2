using JRogue.Actors;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "Precondition_CarriedLitTorch",
        menuName = "JRogue/Interactables/Preconditions/Carried Lit Torch")]
    public sealed class CarriedLitTorchPrecondition : InteractablePrecondition
    {
        public override bool Evaluate(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source,
            out string failureReason)
        {
            if (PartyCarriedLightSource.AnyMemberHasLitAccessoryEmitter())
            {
                failureReason = null;
                return true;
            }

            failureReason = "Requires a lit torch (accessory) in the party.";
            return false;
        }
    }
}
