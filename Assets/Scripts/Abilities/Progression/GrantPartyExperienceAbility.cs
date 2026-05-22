using JRogue.Manager.Progression;
using UnityEngine;

namespace JRogue.Ability.Progression
{
    [CreateAssetMenu(fileName = "GrantPartyExperience", menuName = "JRogue/Abilities/Grant Party Experience")]
    public class GrantPartyExperienceAbility : JRogue.Ability.AbilityAction
    {
        [Min(0)]
        public int experienceAmount = 50;

        public override bool CanExecute(GameObject user) => user != null && experienceAmount > 0;

        protected override bool ExecuteCore(GameObject user)
        {
            PartyExperienceService svc = PartyExperienceService.Instance;
            if (svc == null)
            {
                Debug.LogWarning("[XP] No PartyExperienceService in scene.");
                return false;
            }

            svc.AwardPartyExperience(experienceAmount, $"Item:{abilityName}");
            return true;
        }
    }
}
