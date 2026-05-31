using UnityEngine;

namespace JRogue.World.Altar
{
    [CreateAssetMenu(
        fileName = "AltarPlacementSet",
        menuName = "JRogue/World/Altar Placement Set")]
    public sealed class AltarPlacementSet : ScriptableObject
    {
        public AltarPlacement[] placements = System.Array.Empty<AltarPlacement>();
    }
}
