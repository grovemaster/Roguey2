using JRogue.Actors;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(fileName = "ScriptOnly", menuName = "JRogue/Interactables/Preconditions/Script Only")]
    public sealed class ScriptOnlyPrecondition : InteractablePrecondition
    {
        public override bool Evaluate(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source,
            out string failureReason)
        {
            if (source == InteractableActivationSource.Scripted)
            {
                failureReason = null;
                return true;
            }

            failureReason = "This interactable can only be activated by script.";
            return false;
        }
    }
}
