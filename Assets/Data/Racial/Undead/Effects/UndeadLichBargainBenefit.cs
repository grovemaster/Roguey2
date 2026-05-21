using JRogue.Item.Essence;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "LichBargainBenefit", menuName = "JRogue/Racial/Undead/Lich's Bargain Benefit")]
    public class UndeadLichBargainBenefit : RacialBenefitDefinition
    {
        public int intelligenceBonus = 1;

        public override void OnApply(GameObject target)
        {
            UndeadRacialEffectTracker.GetOrCreate(target)?.RegisterBenefit(this);
            CharacterStats stats = target.GetComponent<CharacterStats>();
            Stat intel = stats?.GetStatByType(StatType.Intelligence);
            intel?.AddModifier(intelligenceBonus, this);
        }

        public override void OnRemove(GameObject target)
        {
            if (target != null && target.TryGetComponent(out UndeadRacialEffectTracker tracker))
                tracker.UnregisterBenefit(this);

            CharacterStats stats = target != null ? target.GetComponent<CharacterStats>() : null;
            Stat intel = stats?.GetStatByType(StatType.Intelligence);
            intel?.RemoveModifiersFromSource(this);
        }
    }
}
