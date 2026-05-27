using UnityEngine;

namespace JRogue.Interactables
{
    public sealed class InteractableTileInstance
    {
        public Vector3Int Cell { get; }
        public InteractableTileDefinition Definition { get; }
        public bool IsOn { get; private set; }
        public bool HasActivated => IsOn;

        public InteractableTileInstance(Vector3Int cell, InteractableTileDefinition definition)
        {
            Cell = cell;
            Definition = definition;
        }

        public void SetOn()
        {
            IsOn = true;
        }
    }
}
