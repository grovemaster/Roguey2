using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Authorable imprint node (tree vertex). Root uses empty <see cref="parentNodeId"/> and must carry no gameplay payload (D2.0).
    /// </summary>
    [Serializable]
    public class SpiritImprintNodeData
    {
        [Tooltip("Stable id for saves and validation.")]
        public string nodeId;

        public string displayName;

        [TextArea] public string description;

        [Tooltip("Empty = root node.")]
        public string parentNodeId;

        [Tooltip("0 = none. Among direct children of the same parent sharing this id, at most one may appear on a valid path.")]
        public int siblingExclusivityGroup;

        [Tooltip("Price to append this node from its parent. Ignored on root.")]
        public SpiritImprintUnlockCost unlockCost;

        public List<AttributeModifier> statModifiers;
        public List<DamageResistanceModifier> resistanceModifiers;
        public List<PassiveEffect> passiveEffects;
        public List<AbilityAction> activeAbilities;

        public bool HasGameplayPayload()
        {
            if (statModifiers is { Count: > 0 }) return true;
            if (resistanceModifiers is { Count: > 0 }) return true;
            if (passiveEffects is { Count: > 0 }) return true;
            if (activeAbilities is { Count: > 0 }) return true;
            return false;
        }
    }
}
