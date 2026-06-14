using System;
using System.Collections.Generic;
using JRogue.Ability;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public class HumanClassSkillTreeNodeData
    {
        public string nodeId;
        public string displayName;
        [TextArea] public string description;

        public int maxRanks = 5;
        public int requiredCharacterLevel = 1;

        [Tooltip("Parent must have at least this many ranks before spending here.")]
        public string requiredParentNodeId;

        public int requiredParentMinRank = 1;

        [Header("Knight")]
        public string branch;
        public List<KnightSkillTag> tags = new List<KnightSkillTag>();
        public string masteryId;
        public int activeAbilityIndex;
        [Tooltip("When > 0, replaces default pxp (12) for combat awards on this node.")]
        public int proficiencyXpOverride;
        [Tooltip("Authored effect % added per tree rank (auras).")]
        public float effectPercentPerRank = 2f;
        [Tooltip("Soul Power upkeep per turn while aura active, scaled by rank.")]
        public int soulPowerUpkeepPerRank = 1;

        public List<HumanPerRankStatModifier> perRankStatModifiers = new List<HumanPerRankStatModifier>();
        public List<AbilityAction> activeAbilities = new List<AbilityAction>();

        public bool HasActiveAbilities =>
            activeAbilities != null && activeAbilities.Count > 0;

        public bool IsAuraStance =>
            tags != null && tags.Contains(KnightSkillTag.AuraStance);

        public string ResolveMasteryId() =>
            string.IsNullOrEmpty(masteryId) ? nodeId : masteryId;

        public AbilityAction ResolveActiveAbility()
        {
            if (!HasActiveAbilities)
                return null;

            int index = Mathf.Clamp(activeAbilityIndex, 0, activeAbilities.Count - 1);
            return activeAbilities[index];
        }
    }
}
