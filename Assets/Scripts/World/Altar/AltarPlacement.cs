using System;
using UnityEngine;

namespace JRogue.World.Altar
{
    [Serializable]
    public struct AltarPlacement
    {
        public Vector3Int cell;
        public AltarDefinition definition;
    }
}
