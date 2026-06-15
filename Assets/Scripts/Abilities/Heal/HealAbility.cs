using UnityEngine;

namespace JRogue.Ability.Heal
{
    [CreateAssetMenu(fileName = "HealAbility", menuName = "JRogue/Abilities/Heal")]
    public class HealAbility : AbilityAction
    {
        public int healAmount = 20;

        public override bool CanExecute(GameObject user)
        {
            if (user == null || !user.TryGetComponent(out JRogue.Stats.CharacterStats stats))
                return false;

            return stats.currentHP < stats.MaxHP;
        }

        protected override bool ExecuteCore(GameObject user)
        {
            var stats = user.GetComponent<JRogue.Stats.CharacterStats>();

            if (stats.currentHP >= stats.MaxHP)
            {
                Debug.Log("Health already full!");
                return false; // Action failed, don't waste Soul Power or a turn
            }

            stats.currentHP = Mathf.Min(stats.currentHP + healAmount, stats.MaxHP);
            Debug.Log($"{user.name} healed for {healAmount}!");
            return true;
        }
    }
}