using JRogue.Ability;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "MageSpell", menuName = "JRogue/Racial/Mage Spell")]
    public class MageSpellDefinition : ScriptableObject
    {
        public string spellId;
        public string displayName;
        [TextArea] public string description;

        [Range(1, 9)]
        [Tooltip("1 = highest tier (equip cost 9); 9 = lowest (equip cost 1).")]
        public int tier = 1;

        public AbilityAction ability;
        public int magicPowerCost = 1;

        public int EquipCost => HumanClassRules.GetSpellEquipCost(tier);
    }
}
