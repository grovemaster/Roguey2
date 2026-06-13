using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;

namespace JRogue.Racial
{
    [Serializable]
    public sealed class SoulBeastLevelData : IRacialProgressionPayload
    {
        public List<AttributeModifier> statModifiers = new List<AttributeModifier>();
        public List<DamageResistanceModifier> resistanceModifiers = new List<DamageResistanceModifier>();
        public List<PassiveEffect> passiveEffects = new List<PassiveEffect>();
        public List<AbilityAction> activeAbilities = new List<AbilityAction>();

        public IReadOnlyList<RacialRestrictionDefinition> RacialRestrictions => null;
        public IReadOnlyList<RacialBenefitDefinition> RacialBenefits => null;
        public IReadOnlyList<AttributeModifier> StatModifiers => statModifiers;
        public IReadOnlyList<DamageResistanceModifier> ResistanceModifiers => resistanceModifiers;
        public IReadOnlyList<PassiveEffect> PassiveEffects => passiveEffects;
        public IReadOnlyList<AbilityAction> ActiveAbilities => activeAbilities;
    }
}
