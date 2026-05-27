using JRogue.Actors;
using UnityEngine;

namespace JRogue.Interactables
{
    public abstract class InteractablePrecondition : ScriptableObject
    {
        public abstract bool Evaluate(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source,
            out string failureReason);
    }
}
