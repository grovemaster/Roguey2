using System;
using UnityEngine;

namespace JRogue.Data.Door
{
    [Serializable]
    public struct DoorPlacement
    {
        public DoorDefinition definition;
        public Vector3Int cell;

        [Tooltip("When true, overrides definition.startsLocked.")]
        public bool overrideLocked;

        public bool startsLocked;

        [Tooltip("When true, overrides open/closed from definition.")]
        public bool overrideOpenState;

        public DoorState initialState;
    }
}
