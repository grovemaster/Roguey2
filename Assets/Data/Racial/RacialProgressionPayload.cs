using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Serializable progression payload for authoring on Undead skill-tree nodes (embedded lists).
    /// Tiefling <see cref="CyborgImplantDefinition"/> implements <see cref="IRacialProgressionPayload"/> directly.
    /// </summary>
    [Serializable]
    public class RacialProgressionPayload : IRacialProgressionPayload
    {
        public List<RacialRestrictionDefinition> racialRestrictions = new List<RacialRestrictionDefinition>();
        public List<RacialBenefitDefinition> racialBenefits = new List<RacialBenefitDefinition>();
        public List<AttributeModifier> statModifiers = new List<AttributeModifier>();
        public List<DamageResistanceModifier> resistanceModifiers = new List<DamageResistanceModifier>();
        public List<PassiveEffect> passiveEffects = new List<PassiveEffect>();
        public List<AbilityAction> activeAbilities = new List<AbilityAction>();

        public IReadOnlyList<RacialRestrictionDefinition> RacialRestrictions => racialRestrictions;
        public IReadOnlyList<RacialBenefitDefinition> RacialBenefits => racialBenefits;
        public IReadOnlyList<AttributeModifier> StatModifiers => statModifiers;
        public IReadOnlyList<DamageResistanceModifier> ResistanceModifiers => resistanceModifiers;
        public IReadOnlyList<PassiveEffect> PassiveEffects => passiveEffects;
        public IReadOnlyList<AbilityAction> ActiveAbilities => activeAbilities;
    }
}
