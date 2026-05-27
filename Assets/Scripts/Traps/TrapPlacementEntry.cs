using System;
using UnityEngine;

namespace JRogue.Traps
{
    [Serializable]
    public struct TrapPlacementEntry
    {
        public Vector3Int cell;
        public TrapDefinition definition;
    }
}
