using JRogue.Ability;
using System.Collections.Generic;
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

    [CreateAssetMenu(fileName = "DragonianSpellCatalog", menuName = "JRogue/Racial/Dragonian Spell Catalog")]
    public sealed class DragonianSpellCatalog : ScriptableObject
    {
        public List<DragonianSpellDefinition> spells = new List<DragonianSpellDefinition>();

        public bool TryGetSpell(string spellId, out DragonianSpellDefinition spell)
        {
            spell = null;
            if (string.IsNullOrWhiteSpace(spellId) || spells == null)
                return false;

            string trimmed = spellId.Trim();
            for (int i = 0; i < spells.Count; i++)
            {
                DragonianSpellDefinition candidate = spells[i];
                if (candidate != null && candidate.spellId == trimmed)
                {
                    spell = candidate;
                    return true;
                }
            }

            return false;
        }
    }

    public static class DragonianSpellCatalogService
    {
        const string DefaultResourcePath = "Racial/Dragonian/DragonianSpellCatalog";

        static DragonianSpellCatalog _cached;

        public static DragonianSpellCatalog Instance
        {
            get
            {
                if (_cached == null)
                    _cached = Resources.Load<DragonianSpellCatalog>(DefaultResourcePath);

                return _cached;
            }
        }

        public static bool TryGetSpell(string spellId, out DragonianSpellDefinition spell)
        {
            spell = null;
            DragonianSpellCatalog catalog = Instance;
            return catalog != null && catalog.TryGetSpell(spellId, out spell);
        }

        public static void ResetCacheForTests() => _cached = null;
    }
}
