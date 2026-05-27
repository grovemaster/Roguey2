using System;
using UnityEngine;

namespace JRogue.Traps
{
    [CreateAssetMenu(fileName = "TrapPlacementSet", menuName = "JRogue/Traps/Trap Placement Set")]
    public sealed class TrapPlacementSet : ScriptableObject
    {
        public TrapPlacementEntry[] placements = Array.Empty<TrapPlacementEntry>();
    }
}
