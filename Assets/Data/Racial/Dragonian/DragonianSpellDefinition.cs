using JRogue.Ability;
using System;
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
        const string SpellResourceFolder = "Racial/Dragonian";

        static DragonianSpellCatalog _cached;
        static Dictionary<string, DragonianSpellDefinition> _spellLookup;

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
            if (string.IsNullOrWhiteSpace(spellId))
                return false;

            EnsureSpellLookup();
            return _spellLookup.TryGetValue(spellId.Trim(), out spell);
        }

        static void EnsureSpellLookup()
        {
            if (_spellLookup != null)
                return;

            _spellLookup = new Dictionary<string, DragonianSpellDefinition>(StringComparer.OrdinalIgnoreCase);

            DragonianSpellCatalog catalog = Instance;
            if (catalog?.spells != null)
            {
                for (int i = 0; i < catalog.spells.Count; i++)
                    RegisterSpell(catalog.spells[i]);
            }

            if (_spellLookup.Count == 0)
            {
                DragonianSpellDefinition[] spells =
                    Resources.LoadAll<DragonianSpellDefinition>(SpellResourceFolder);
                for (int i = 0; i < spells.Length; i++)
                    RegisterSpell(spells[i]);
            }

            if (_spellLookup.Count == 0)
            {
                Debug.LogWarning(
                    "[Dragonian] No Dragonian spells found under "
                    + $"Resources/{SpellResourceFolder}. "
                    + "Run JRogue/Racial/Create Dragonian Spell Pack.");
            }
        }

        static void RegisterSpell(DragonianSpellDefinition spell)
        {
            if (spell == null || string.IsNullOrWhiteSpace(spell.spellId))
                return;

            _spellLookup[spell.spellId.Trim()] = spell;
        }

        public static void ResetCacheForTests()
        {
            _cached = null;
            _spellLookup = null;
        }
    }
}
