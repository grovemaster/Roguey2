using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(fileName = "InteractableTile", menuName = "JRogue/Interactables/Interactable Tile Definition")]
    public sealed class InteractableTileDefinition : ScriptableObject
    {
        public InteractableTileId interactableId = InteractableTileId.None;
        public string displayName = "Interactable";
        public InteractableTileKind kind = InteractableTileKind.Lever;

        public bool blocksOccupancy = true;
        public bool bumpEnabled = true;

        public InteractablePrecondition[] preconditions = System.Array.Empty<InteractablePrecondition>();
        public InteractableEffect[] onActivateEffects = System.Array.Empty<InteractableEffect>();

        public Sprite spriteOff;
        public Sprite spriteOn;
    }
}
