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

        public List<HumanPerRankStatModifier> perRankStatModifiers = new List<HumanPerRankStatModifier>();
        public List<AbilityAction> activeAbilities = new List<AbilityAction>();
    }
}
