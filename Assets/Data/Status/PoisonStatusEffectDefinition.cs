using JRogue.Stats;
using UnityEngine;

namespace JRogue.Status
{
    [CreateAssetMenu(
        menuName = "JRogue/Status/Poison Definition",
        fileName = "Status_Poisoned")]
    public sealed class PoisonStatusEffectDefinition : StatusEffectDefinition
    {
        [Min(1)] public int damagePerTick = 1;
        public DamageType damageType = DamageType.Poison;
        public int escapeDifficulty = 12;

        void OnValidate()
        {
            statusId = StatusEffectId.Poisoned;
            if (maxDurationTurns <= 0)
                maxDurationTurns = 10;
            if (damagePerTick <= 0)
                damagePerTick = 1;
        }
    }
}
