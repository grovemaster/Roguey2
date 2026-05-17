using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.Passive
{
    [CreateAssetMenu(fileName = "ElementalSpiritStatPassive", menuName = "JRogue/Passives/Elemental Spirit Stat")]
    public class ElementalSpiritStatPassive : PassiveEffect
    {
        public StatType stat = StatType.Dexterity;
        public int amount = 1;

        public override void OnApply(GameObject user)
        {
            var target = user.GetComponent<CharacterStats>()?.GetStatByType(stat);
            target?.AddModifier(amount, this);
        }

        public override void OnRemove(GameObject user)
        {
            user.GetComponent<CharacterStats>()?.GetStatByType(stat)?.RemoveModifiersFromSource(this);
        }
    }
}
