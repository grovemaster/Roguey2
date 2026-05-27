using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Hazards
{
    [CreateAssetMenu(fileName = "EnvironmentalHazardTile", menuName = "JRogue/Hazards/Environmental Hazard Tile")]
    public class EnvironmentalHazardTile : Tile
    {
        public EnvironmentalHazardDefinition hazardDefinition;
        [Tooltip("When true, overlay is hidden until detected by sight or entered.")]
        public bool startHidden;
    }
}
