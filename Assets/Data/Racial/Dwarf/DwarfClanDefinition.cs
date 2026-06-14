using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "DwarfClan", menuName = "JRogue/Racial/Dwarf Clan")]
    public class DwarfClanDefinition : ScriptableObject
    {
        public string clanId;
        public string displayName;
        public string shortName;
        [TextArea] public string description;
        public AncestorDefinition patronAncestor;
        public int startingPrestige = 5;
        [TextArea] public string altarFlavorTitle = "The ancestors await your offering.";
    }
}
