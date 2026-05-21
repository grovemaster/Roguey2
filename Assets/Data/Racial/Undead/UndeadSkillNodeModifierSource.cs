using System;

namespace JRogue.Racial
{
    sealed class UndeadSkillNodeModifierSource : IEquatable<UndeadSkillNodeModifierSource>
    {
        public UndeadSkillTreeDefinition Tree { get; }
        public string NodeId { get; }

        public UndeadSkillNodeModifierSource(UndeadSkillTreeDefinition tree, string nodeId)
        {
            Tree = tree;
            NodeId = nodeId;
        }

        public bool Equals(UndeadSkillNodeModifierSource other) =>
            other != null && Tree == other.Tree && NodeId == other.NodeId;

        public override bool Equals(object obj) => obj is UndeadSkillNodeModifierSource o && Equals(o);

        public override int GetHashCode() =>
            HashCode.Combine(Tree != null ? Tree.GetEntityId().GetHashCode() : 0, NodeId);
    }
}
