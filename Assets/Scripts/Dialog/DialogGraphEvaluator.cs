using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Dialog
{
    public static class DialogGraphEvaluator
    {
        public static bool EvaluateCondition(DialogNodeData node, DialogContext context)
        {
            if (node == null || context == null)
                return false;

            switch (node.conditionKind)
            {
                case DialogConditionKind.StoryFlag:
                    return context.Flags != null
                           && context.Flags.IsSet(node.flagId) == node.expectedFlagValue;

                case DialogConditionKind.NpcTalkCount:
                {
                    int count = context.Counters != null
                        ? context.Counters.GetCount(node.npcIdForTalkCount)
                        : 0;
                    return count >= node.talkCountMin && count <= node.talkCountMax;
                }

                case DialogConditionKind.AnyNpcTalked:
                    return context.Flags != null
                           && context.Flags.IsAnySet(node.anyTalkedNpcIds);

                default:
                    return true;
            }
        }

        public static DialogNodeData GetNode(NpcDialogProfile profile, int nodeIndex)
        {
            if (profile?.nodes == null || nodeIndex < 0 || nodeIndex >= profile.nodes.Length)
                return null;

            return profile.nodes[nodeIndex];
        }

        public static int ResolveEntryNodeIndex(NpcDialogProfile profile, DialogContext context)
        {
            if (profile == null || profile.rootNodeIndex < 0)
                return DialogGraph.NoNode;

            return ResolveNodeIndex(profile, profile.rootNodeIndex, context);
        }

        public static int ResolveNodeIndex(NpcDialogProfile profile, int nodeIndex, DialogContext context)
        {
            if (profile == null || nodeIndex < 0)
                return DialogGraph.NoNode;

            return WalkConditionals(profile, nodeIndex, context);
        }

        static int WalkConditionals(NpcDialogProfile profile, int nodeIndex, DialogContext context)
        {
            DialogNodeData node = GetNode(profile, nodeIndex);
            if (node == null)
                return DialogGraph.NoNode;

            if (node.kind != DialogNodeKind.Conditional)
                return nodeIndex;

            bool result = EvaluateCondition(node, context);
            int branchIndex = result ? node.trueNodeIndex : node.falseNodeIndex;
            if (branchIndex < 0)
                return DialogGraph.NoNode;

            DialogNodeData branch = GetNode(profile, branchIndex);
            if (branch != null && branch.kind == DialogNodeKind.Conditional)
                return WalkConditionals(profile, branchIndex, context);

            return branchIndex;
        }

        public static DialogLineStep BuildLineStep(
            NpcDialogProfile profile,
            int nodeIndex,
            DialogContext context,
            PortraitDefinition portrait)
        {
            DialogNodeData node = GetNode(profile, nodeIndex);
            if (node == null || node.kind != DialogNodeKind.Line)
                return null;

            string text = DialogParameterResolver.Resolve(node.line?.textTemplate, context);
            int nextIndex = node.nextNodeIndex;
            if (nextIndex >= 0)
            {
                DialogNodeData nextNode = GetNode(profile, nextIndex);
                if (nextNode != null && nextNode.kind == DialogNodeKind.Conditional)
                    nextIndex = WalkConditionals(profile, nextIndex, context);
            }

            return new DialogLineStep
            {
                SpeakerName = context.Npc != null ? context.Npc.DisplayName : string.Empty,
                ResolvedText = text,
                Portrait = portrait,
                NextNodeIndex = nextIndex,
            };
        }

        public static DialogChoiceStep BuildChoiceStep(
            NpcDialogProfile profile,
            int nodeIndex,
            DialogContext context,
            PortraitDefinition portrait)
        {
            DialogNodeData node = GetNode(profile, nodeIndex);
            if (node == null || node.kind != DialogNodeKind.Choice)
                return null;

            string prompt = DialogParameterResolver.Resolve(node.line?.textTemplate, context);
            return new DialogChoiceStep
            {
                SpeakerName = context.Npc != null ? context.Npc.DisplayName : string.Empty,
                PromptText = prompt,
                Portrait = portrait,
                Options = node.choices ?? System.Array.Empty<DialogChoiceOptionData>(),
            };
        }
    }
}
