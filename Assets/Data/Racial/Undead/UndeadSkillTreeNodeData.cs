using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public class UndeadSkillTreeNodeData : IRacialProgressionPayload
    {
        public string nodeId;
        public string displayName;
        [TextArea] public string description;

        public string clusterId;
        public UndeadSkillNodeKind nodeKind = UndeadSkillNodeKind.Skill;
        public int maxRanks = 1;

        [Tooltip("Optional. Parent must be at least requiredParentMinRank (default 1).")]
        public string requiredParentNodeId;

        public int requiredParentMinRank = 1;

        [Tooltip("0 = none. At most one node per group may have rank > 0.")]
        public int mutualExclusivityGroup;

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

        public bool HasAnyPayload()
        {
            if (racialRestrictions is { Count: > 0 }) return true;
            if (racialBenefits is { Count: > 0 }) return true;
            if (statModifiers is { Count: > 0 }) return true;
            if (resistanceModifiers is { Count: > 0 }) return true;
            if (passiveEffects is { Count: > 0 }) return true;
            if (activeAbilities is { Count: > 0 }) return true;
            return false;
        }
    }
}
