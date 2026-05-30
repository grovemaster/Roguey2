using System;
using UnityEngine;

namespace JRogue.Data.Door
{
    [CreateAssetMenu(fileName = "DoorPlacementSet", menuName = "JRogue/Doors/Door Placement Set")]
    public sealed class DoorPlacementSet : ScriptableObject
    {
        public DoorPlacement[] placements = Array.Empty<DoorPlacement>();
    }
}
