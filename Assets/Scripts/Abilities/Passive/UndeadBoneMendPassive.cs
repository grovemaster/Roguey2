using JRogue.Item.Essence;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.Passive
{
    [CreateAssetMenu(fileName = "UndeadBoneMend", menuName = "JRogue/Passives/Undead Bone Mend")]
    public class UndeadBoneMendPassive : PassiveEffect
    {
        public int wisdomBonus = 1;

        public override void OnApply(GameObject user)
        {
            Stat wis = user.GetComponent<CharacterStats>()?.GetStatByType(StatType.Wisdom);
            wis?.AddModifier(wisdomBonus, this);
        }

        public override void OnRemove(GameObject user)
        {
            Stat wis = user != null ? user.GetComponent<CharacterStats>()?.GetStatByType(StatType.Wisdom) : null;
            wis?.RemoveModifiersFromSource(this);
        }
    }
}
