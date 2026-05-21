using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Tracks active racial benefits/restrictions from progression payloads (implants, skill nodes).
    /// </summary>
    public class UndeadRacialEffectTracker : MonoBehaviour
    {
        readonly HashSet<RacialBenefitDefinition> _benefits = new HashSet<RacialBenefitDefinition>();
        readonly HashSet<RacialRestrictionDefinition> _restrictions = new HashSet<RacialRestrictionDefinition>();

        public bool HasBenefit(RacialBenefitDefinition benefit) =>
            benefit != null && _benefits.Contains(benefit);

        public bool HasNecroticSustenance => HasBenefitType<UndeadNecroticSustenanceBenefit>();

        public bool HasBenefitType<T>() where T : RacialBenefitDefinition
        {
            foreach (RacialBenefitDefinition b in _benefits)
            {
                if (b is T)
                    return true;
            }

            return false;
        }

        public void RegisterBenefit(RacialBenefitDefinition benefit)
        {
            if (benefit != null)
                _benefits.Add(benefit);
        }

        public void UnregisterBenefit(RacialBenefitDefinition benefit)
        {
            if (benefit != null)
                _benefits.Remove(benefit);
        }

        public void RegisterRestriction(RacialRestrictionDefinition restriction)
        {
            if (restriction != null)
                _restrictions.Add(restriction);
        }

        public void UnregisterRestriction(RacialRestrictionDefinition restriction)
        {
            if (restriction != null)
                _restrictions.Remove(restriction);
        }

        public static UndeadRacialEffectTracker GetOrCreate(GameObject target)
        {
            if (target == null)
                return null;
            if (!target.TryGetComponent(out UndeadRacialEffectTracker tracker))
                tracker = target.AddComponent<UndeadRacialEffectTracker>();
            return tracker;
        }
    }
}
