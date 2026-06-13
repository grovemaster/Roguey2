using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "MageSpellbook", menuName = "JRogue/Racial/Mage Spellbook")]
    public sealed class MageSpellbookDefinition : ScriptableObject
    {
        public string spellbookId;
        public string displayName;
        [TextArea] public string description;
        public List<string> spellIds = new List<string>();
    }
}
