using JRogue.Ability;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "DragonianSpell", menuName = "JRogue/Racial/Dragonian Spell")]
    public class DragonianSpellDefinition : ScriptableObject
    {
        public string spellId;
        public string displayName;
        [TextArea] public string description;

        [Min(0)]
        public int memorizeCost;

        [Min(0)]
        public int soulPowerCastCost;

        public AbilityAction ability;
    }
}
