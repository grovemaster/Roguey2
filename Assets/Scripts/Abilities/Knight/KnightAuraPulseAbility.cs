using JRogue.Actors;
using JRogue.Racial;
using JRogue.Racial.Knight;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Ability.Knight
{
    [CreateAssetMenu(fileName = "KnightAuraPulse", menuName = "JRogue/Abilities/Knight/Aura Pulse")]
    public sealed class KnightAuraPulseAbility : AbilityAction
    {
        [SerializeField] string nodeId;

        public string NodeId => string.IsNullOrEmpty(nodeId) ? knightSkillId : nodeId;

        public override bool CanExecute(GameObject user) =>
            TryResolveContext(user, out _, out HumanClassSkillTreeRuntime tree, out _)
            && tree.GetRank(NodeId) >= 1;

        protected override bool ExecuteCore(GameObject user)
        {
            if (!TryResolveContext(
                    user,
                    out BaseActor actor,
                    out HumanClassSkillTreeRuntime tree,
                    out HumanClassSkillTreeNodeData node))
            {
                return false;
            }

            int rank = tree.GetRank(NodeId);
            KnightSkillMasteryRuntime mastery = user.GetComponent<KnightSkillMasteryRuntime>();
            int masteryLevel = mastery != null ? mastery.GetMasteryLevel(node.ResolveMasteryId()) : 0;
            float potency = KnightSkillCombatResolver.GetPulsePotencyPercent(node, rank, masteryLevel);

            Debug.Log(
                $"[Knight Pulse] {node.displayName} on {user.name} — potency +{potency:0.#}% (rank {rank}, mastery {masteryLevel}).");

            return true;
        }

        bool TryResolveContext(
            GameObject user,
            out BaseActor actor,
            out HumanClassSkillTreeRuntime tree,
            out HumanClassSkillTreeNodeData node)
        {
            actor = null;
            tree = null;
            node = null;

            if (user == null || string.IsNullOrEmpty(NodeId))
                return false;

            actor = user.GetComponent<BaseActor>();
            CharacterStats stats = user.GetComponent<CharacterStats>();
            tree = user.GetComponent<HumanClassSkillTreeRuntime>();

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
