using JRogue.Actors;
using JRogue.Manager.Door;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(fileName = "UnlockDoor", menuName = "JRogue/Interactables/Effects/Unlock Door")]
    public sealed class UnlockDoorEffect : InteractableEffect
    {
        public string doorId;

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            DoorService.Instance?.Unlock(doorId, $"lever:{instance?.Definition?.displayName}");
        }
    }
}
