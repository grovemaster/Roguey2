using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>
    /// Opt-in marker for floor tilemaps that are not named <c>Floor</c>
    /// but should receive DCSS rect_gray variety when randomizing the open scene.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("JRogue/Town/DCSS Floor Variety Include")]
    public sealed class DcssFloorVarietyInclude : MonoBehaviour
    {
    }
}
