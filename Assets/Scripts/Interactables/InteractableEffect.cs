using JRogue.Actors;
using UnityEngine;

namespace JRogue.Interactables
{
    public abstract class InteractableEffect : ScriptableObject
    {
        public abstract void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source);
    }
}
