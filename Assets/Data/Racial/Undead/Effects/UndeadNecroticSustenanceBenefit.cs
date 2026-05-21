using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "NecroticSustenanceBenefit", menuName = "JRogue/Racial/Undead/Necrotic Sustenance Benefit")]
    public class UndeadNecroticSustenanceBenefit : RacialBenefitDefinition
    {
        public override void OnApply(GameObject target)
        {
            UndeadRacialEffectTracker.GetOrCreate(target)?.RegisterBenefit(this);
        }

        public override void OnRemove(GameObject target)
        {
            if (target != null && target.TryGetComponent(out UndeadRacialEffectTracker tracker))
                tracker.UnregisterBenefit(this);
        }
    }
}
