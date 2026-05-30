using JRogue.Ability;
using JRogue.Ability.Heal;
using JRogue.Manager.Combat;
using JRogue.Item;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Manager.Inventory
{
    public static class HealingPotionRules
    {
        public const string CombatBanMessage = "Cannot drink this potion during combat.";
        public const int HealAmount = 50;

        public static bool IsHealingPotionItem(ItemData item) =>
            item != null
            && item.activeAbilities != null
            && item.activeAbilities.Count > 0
            && item.activeAbilities[0] is HealingPotionAbility;

        public static bool IsHealingPotionAbility(AbilityAction ability) => ability is HealingPotionAbility;

        public static bool CanUseWithoutCombatBan(GameObject user)
        {
            if (user == null)
                return false;

            CombatThreatCoordinator combat = CombatThreatCoordinator.Instance;
            if (combat == null || !combat.IsInCombat)
                return true;

            return IsExemptFromPainStun(user);
        }

        public static bool IsExemptFromPainStun(GameObject user)
        {
            if (user == null || !user.TryGetComponent(out CharacterStats stats))
                return false;

            return RacialTraitQueries.HasTrait(stats, RacialTraitFlags.WarriorWillpower)
                   && stats.painTolerance.GetValue() >= 100;
        }

        public static int ComputeStunTurns(CharacterStats stats)
        {
            if (stats == null)
                return 3;

            int painToleranceValue = Mathf.Max(1, stats.painTolerance.GetValue());
            return Mathf.Max(3, 100 / painToleranceValue);
        }
    }
}
