using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "Ancestor", menuName = "JRogue/Racial/Dwarf Ancestor")]
    public class AncestorDefinition : ScriptableObject
    {
        public string ancestorId;
        public string displayName;
        [TextArea] public string description;

        [Tooltip("Forward-only ability tree for this patron (Spirit Imprint graph shape).")]
        public SpiritImprintGraph abilityTree;
    }
}
