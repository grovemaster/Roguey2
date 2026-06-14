using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Places unlocked Human Knight actives onto empty main hotbar slots.
    /// </summary>
    public static class HumanKnightHotbarSync
    {
        public static bool TryAssignActiveToHotbar(
            BaseActor actor,
            string nodeId,
            int abilityIndex,
            out string failureReason)
        {
            failureReason = null;
            if (actor == null)
            {
                failureReason = "No actor.";
                return false;
            }

            HumanClassSkillTreeRuntime tree = actor.GetComponent<HumanClassSkillTreeRuntime>();
            if (tree == null || tree.SkillTree == null)
            {
                failureReason = "No Knight skill tree runtime.";
                return false;
            }

            if (!tree.SkillTree.TryFindNode(nodeId, out HumanClassSkillTreeNodeData node)
                || !node.HasActiveAbilities)
            {
                failureReason = "Skill is not an active node.";
                return false;
            }

            if (tree.GetRank(nodeId) < 1)
            {
                failureReason = "Skill is not unlocked.";
                return false;
            }

            if (abilityIndex < 0 || abilityIndex >= node.activeAbilities.Count
                || node.activeAbilities[abilityIndex] == null)
            {
                failureReason = "Ability index is invalid.";
                return false;
            }

            HotbarLayout layout = HotbarLayout.EnsureOn(actor);
            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                HotbarEntry entry = layout.GetSlot(slot);
                if (entry.Kind == HotbarEntryKind.HumanKnightSkill
                    && string.Equals(entry.knightNodeId, nodeId, StringComparison.Ordinal)
                    && entry.abilityIndex == abilityIndex)
                {
                    failureReason = "Already on hotbar.";
                    return false;
                }
            }

            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                if (!layout.GetSlot(slot).IsEmpty())
                    continue;

                layout.SetSlot(slot, new HotbarEntry
                {
                    Kind = HotbarEntryKind.HumanKnightSkill,
                    knightNodeId = nodeId,
                    abilityIndex = abilityIndex,
                    abilityAssetName = node.activeAbilities[abilityIndex].name,
                });
                return true;
            }

            failureReason = "No empty hotbar slot.";
            return false;
        }

        public static void TryAssignUnlockedActivesToEmptyMainSlots(BaseActor actor)
        {
            if (actor == null)
                return;

            HumanClassSkillTreeRuntime tree = actor.GetComponent<HumanClassSkillTreeRuntime>();
            if (tree?.SkillTree?.nodes == null)
                return;

            var assigned = new HashSet<string>(StringComparer.Ordinal);
            HotbarLayout layout = HotbarLayout.EnsureOn(actor);

            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                HotbarEntry entry = layout.GetSlot(slot);
                if (entry.Kind != HotbarEntryKind.HumanKnightSkill)
                    continue;

                assigned.Add($"{entry.knightNodeId}:{entry.abilityIndex}");
            }

            foreach (HumanClassSkillTreeNodeData node in tree.SkillTree.nodes)
            {
                if (node == null || !node.HasActiveAbilities || tree.GetRank(node.nodeId) < 1)
                    continue;

                int index = Mathf.Clamp(node.activeAbilityIndex, 0, node.activeAbilities.Count - 1);
                string key = $"{node.nodeId}:{index}";
                if (assigned.Contains(key))
                    continue;

                if (TryAssignActiveToHotbar(actor, node.nodeId, index, out _))
                    assigned.Add(key);
            }
        }
    }
}
