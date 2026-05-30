using JRogue.Data.Door;
using UnityEngine;

namespace JRogue.Manager.Door
{
    public sealed class DoorInstance
    {
        public DoorInstance(DoorDefinition definition, Vector3Int cell, DoorState state, bool isUnlocked)
        {
            Definition = definition;
            Cell = cell;
            State = state;
            IsUnlocked = isUnlocked;
        }

        public DoorDefinition Definition { get; }
        public Vector3Int Cell { get; }
        public DoorState State { get; private set; }
        public bool IsUnlocked { get; private set; }

        public string DoorId => Definition != null ? Definition.doorId : string.Empty;

        public DoorOrientation Orientation =>
            Definition != null ? Definition.orientation : DoorOrientation.Horizontal;

        public bool BlocksMovement => State == DoorState.Closed;

        public void SetState(DoorState state) => State = state;

        public void SetUnlocked(bool unlocked) => IsUnlocked = unlocked;
    }
}
