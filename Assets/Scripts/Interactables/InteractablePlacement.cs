using System;
using UnityEngine;

namespace JRogue.Interactables
{
    [Serializable]
    public struct InteractablePlacement
    {
        public Vector3Int cell;
        public InteractableTileDefinition definition;
    }
}
