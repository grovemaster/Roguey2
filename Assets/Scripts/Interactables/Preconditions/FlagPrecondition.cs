using JRogue.Actors;
using UnityEngine;

namespace JRogue.Interactables
{
    /// <summary>Stub for future quest / map flags.</summary>
    [CreateAssetMenu(fileName = "Flag", menuName = "JRogue/Interactables/Preconditions/Flag (stub)")]
    public sealed class FlagPrecondition : InteractablePrecondition
    {
        public string flagId;

        public override bool Evaluate(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source,
            out string failureReason)
        {
            failureReason = $"Flag '{flagId}' is not implemented.";
            return false;
        }
    }
}
