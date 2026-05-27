using JRogue.Actors;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(fileName = "AlwaysTrue", menuName = "JRogue/Interactables/Preconditions/Always True")]
    public sealed class AlwaysTruePrecondition : InteractablePrecondition
    {
        public override bool Evaluate(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source,
            out string failureReason)
        {
            failureReason = null;
            return true;
        }
    }
}
