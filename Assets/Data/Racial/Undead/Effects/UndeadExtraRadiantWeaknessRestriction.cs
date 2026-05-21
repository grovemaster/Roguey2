using JRogue.Item.Essence;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "ExtraRadiantWeakness", menuName = "JRogue/Racial/Undead/Extra Radiant Weakness Restriction")]
    public class UndeadExtraRadiantWeaknessRestriction : RacialRestrictionDefinition
    {
        public int additionalRadiantPenalty = -25;

        public override void OnApply(GameObject target)
        {
            UndeadRacialEffectTracker.GetOrCreate(target)?.RegisterRestriction(this);
            CharacterStats stats = target.GetComponent<CharacterStats>();
            if (stats != null)
                stats.AddResistanceModifier(DamageType.Radiant, additionalRadiantPenalty, this);
        }

        public override void OnRemove(GameObject target)
        {
            if (target != null && target.TryGetComponent(out UndeadRacialEffectTracker tracker))
                tracker.UnregisterRestriction(this);

            CharacterStats stats = target != null ? target.GetComponent<CharacterStats>() : null;
            if (stats != null)
                stats.RemoveResistanceModifier(DamageType.Radiant, this);
        }
    }
}
