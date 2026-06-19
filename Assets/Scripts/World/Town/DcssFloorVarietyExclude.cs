using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>
    /// Marks a tilemap (or floor root) to skip
    /// <c>JRogue → Town → Randomize Stone Floor Tiles (Current Scene)</c>.
    /// Place on building-interior grids you want to keep a uniform floor.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("JRogue/Town/DCSS Floor Variety Exclude")]
    public sealed class DcssFloorVarietyExclude : MonoBehaviour
    {
    }
}
