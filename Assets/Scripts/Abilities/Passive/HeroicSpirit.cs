using UnityEngine;
using JRogue.Item.Essence;
using JRogue.Stats;

namespace JRogue.Ability.Passive
{
    [CreateAssetMenu(fileName = "HeroicSpirit", menuName = "JRogue/Passives/Heroic Spirit")]
    public class HeroicSpirit : PassiveEffect
    {
        public int resistanceBonus = 15;
        private bool _isActive = false;

        public override void OnApply(GameObject user) => Refresh(user);
        public override void OnRemove(GameObject user) => Cleanup(user);

        public override void Refresh(GameObject user)
        {
            var stats = user.GetComponent<CharacterStats>();
            bool conditionMet = stats.currentHP <= (stats.MaxHP * 0.5f);
            Stat agilityStat = stats.GetStatByType(StatType.Agility);

            // Pass 'this' (the ScriptableObject) as the source identifier
            bool alreadyApplied = agilityStat.HasModifierFromSource(this);

            if (conditionMet && !alreadyApplied)
            {
                // Add the modifier and mark THIS passive as the source
                agilityStat.AddModifier(resistanceBonus, this);
                Debug.Log($"{user.name}'s Heroic Spirit activates!");
            }
            else if (!conditionMet && alreadyApplied)
            {
                // Only remove the modifiers that THIS passive added
                agilityStat.RemoveModifiersFromSource(this);
                Debug.Log($"{user.name}'s Heroic Spirit fades.");
            }
        }

        private void Cleanup(GameObject user)
        {
            if (!_isActive) return;

            var stats = user.GetComponent<CharacterStats>();

            Stat agilityStat = stats.GetStatByType(StatType.Agility);
            agilityStat.RemoveModifiersFromSource(this);
            // foreach (DamageType type in System.Enum.GetValues(typeof(DamageType)))
            //     stats.RemoveResistanceModifier(type, resistanceBonus);

            _isActive = false;
            Debug.Log("Heroic Spirit Dormant.");
        }
    }
}