using UnityEngine;

namespace JRogue.World.Lighting
{
    [CreateAssetMenu(
        menuName = "JRogue/Lighting/Lighting Placement Set",
        fileName = "LightingPlacementSet_")]
    public sealed class LightingPlacementSet : ScriptableObject
    {
        public LightingPlacementEntry[] placements = System.Array.Empty<LightingPlacementEntry>();
    }
}
