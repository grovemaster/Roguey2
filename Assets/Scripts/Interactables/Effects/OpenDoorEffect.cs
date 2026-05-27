using JRogue.Actors;
using JRogue.Manager.Door;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(fileName = "OpenDoor", menuName = "JRogue/Interactables/Effects/Open Door")]
    public sealed class OpenDoorEffect : InteractableEffect
    {
        public string doorId;

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            DoorService.Instance?.Open(doorId);
        }
    }
}
