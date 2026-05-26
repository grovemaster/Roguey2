using System;

namespace JRogue.Racial
{
    /// <summary>Stable modifier source for a human class skill tree node (Pattern B).</summary>
    public sealed class HumanClassSkillNodeModifierSource
    {
        public HumanClassSkillNodeModifierSource(HumanClassSkillTreeDefinition tree, string nodeId)
        {
            Tree = tree;
            NodeId = nodeId;
        }

        public HumanClassSkillTreeDefinition Tree { get; }
        public string NodeId { get; }
    }
}
