using JRogue.Actors;
using JRogue.Racial;
using JRogue.Racial.Knight;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Ability.Knight
{
    [CreateAssetMenu(fileName = "KnightAuraToggle", menuName = "JRogue/Abilities/Knight/Aura Toggle")]
    public sealed class KnightAuraToggleAbility : AbilityAction
    {
        [SerializeField] string nodeId;

        public string NodeId => string.IsNullOrEmpty(nodeId) ? knightSkillId : nodeId;

        public override bool CanExecute(GameObject user) =>
            TryResolveContext(user, out _, out HumanClassSkillTreeRuntime tree, out _, out _)
            && tree.GetRank(NodeId) >= 1;

        protected override bool ExecuteCore(GameObject user)
        {
            if (!TryResolveContext(
                    user,
                    out BaseActor actor,
                    out HumanClassSkillTreeRuntime tree,
                    out KnightAuraStateRuntime auraState,
                    out HumanClassSkillTreeNodeData node))
            {
                return false;
            }

            string skillId = NodeId;
            if (auraState.IsActive(skillId))
            {
                auraState.TryDeactivate(skillId);
                Debug.Log($"[Knight Aura] Deactivated {node.displayName} on {user.name}.");
                return true;
            }

            if (!auraState.TryActivate(tree.SkillTree, skillId, out bool wasAlreadyActive, out string failureReason))
            {
                Debug.Log($"[Knight Aura] Failed to activate {skillId}: {failureReason}");
                return false;
            }

            if (wasAlreadyActive)
                return true;

            int rank = tree.GetRank(skillId);
            KnightSkillMasteryRuntime mastery = user.GetComponent<KnightSkillMasteryRuntime>();
            int masteryLevel = mastery != null ? mastery.GetMasteryLevel(node.ResolveMasteryId()) : 0;

            Debug.Log(
                $"[Knight Aura] Activated {node.displayName} on {user.name} "
                + $"(rank {rank}, effect +{KnightSkillCombatResolver.GetPartyDamageBonusPercent(node, rank, masteryLevel):0.#}%).");

            return true;
        }

        bool TryResolveContext(
            GameObject user,
            out BaseActor actor,
            out HumanClassSkillTreeRuntime tree,
            out KnightAuraStateRuntime auraState,
            out HumanClassSkillTreeNodeData node)
        {
            actor = null;
            tree = null;
            auraState = null;
            node = null;

            if (user == null || string.IsNullOrEmpty(NodeId))
                return false;

            actor = user.GetComponent<BaseActor>();
            CharacterStats stats = user.GetComponent<CharacterStats>();
            tree = user.GetComponent<HumanClassSkillTreeRuntime>();
            auraState = KnightAuraStateRuntime.EnsureOn(user);

            if (stats == null
                || tree == null
                || tree.SkillTree == null
                || stats.humanClass != HumanClass.Knight)
            {
                return false;
            }

            return tree.SkillTree.TryFindNode(NodeId, out node);
        }
    }
}
