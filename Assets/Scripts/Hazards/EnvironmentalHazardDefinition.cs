using JRogue.Stats;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Hazards
{
    [CreateAssetMenu(fileName = "EnvironmentalHazard", menuName = "JRogue/Hazards/Environmental Hazard")]
    public class EnvironmentalHazardDefinition : ScriptableObject
    {
        public EnvironmentalHazardId hazardId = EnvironmentalHazardId.None;
        public string displayName = "Hazard";

        public EnvironmentalHazardKind kind = EnvironmentalHazardKind.Passage;
        public PassageCondition passageCondition = PassageCondition.None;

        [Min(0)]
        public int requiredStrength = 50;

        public Tile overlayTile;
        public Sprite overlaySprite;
        public bool underlyingFloorPreserves;
        [Tooltip("When true, enemies treat this hazard as undesirable and avoid it if another route exists.")]
        public bool avoidForEnemyPathing = true;

        [Min(0)]
        public int persistentDamagePerTrigger = 1;

        public DamageType persistentDamageType = DamageType.Poison;
    }
}
