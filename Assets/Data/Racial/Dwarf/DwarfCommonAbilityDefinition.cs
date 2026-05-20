using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "DwarfCommonAbility", menuName = "JRogue/Racial/Dwarf Common Ability")]
    public class DwarfCommonAbilityDefinition : ScriptableObject
    {
        public string abilityId;
        public string displayName;
        [TextArea] public string description;

        public List<AttributeModifier> statModifiers;
        public List<DamageResistanceModifier> resistanceModifiers;
        public List<PassiveEffect> passiveEffects;
        public List<AbilityAction> activeAbilities;
    }
}
