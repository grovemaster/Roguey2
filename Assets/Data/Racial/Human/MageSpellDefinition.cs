using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "MageSpell", menuName = "JRogue/Racial/Mage Spell")]
    public class MageSpellDefinition : ScriptableObject
    {
        public string spellId;
        public string displayName;
        [TextArea] public string description;

        [Range(1, 9)]
        [Tooltip("1 = highest tier (equip cost 9); 9 = lowest (equip cost 1).")]
        public int tier = 1;

        public AbilityAction ability;
        public int magicPowerCost = 1;

        [Header("Proficiency")]
        public List<ProficiencyKind> proficiencyTags = new();

        [Min(0)]
        public int extraEquipCost;

        public int EquipCost => HumanClassRules.GetSpellEquipCost(tier, extraEquipCost);
    }

    [CreateAssetMenu(fileName = "MageSpellCatalog", menuName = "JRogue/Racial/Mage Spell Catalog")]
    public sealed class MageSpellCatalog : ScriptableObject
    {
        public List<MageSpellDefinition> spells = new List<MageSpellDefinition>();

        public bool TryGetSpell(string spellId, out MageSpellDefinition spell)
        {
            spell = null;
            if (string.IsNullOrWhiteSpace(spellId) || spells == null)
                return false;

            string trimmed = spellId.Trim();
            for (int i = 0; i < spells.Count; i++)
            {
                MageSpellDefinition candidate = spells[i];
                if (candidate != null && candidate.spellId == trimmed)
                {
                    spell = candidate;
                    return true;
                }
            }

            return false;
        }
    }

    public static class MageSpellCatalogService
    {
        const string DefaultResourcePath = "Racial/Human/MageSpellCatalog";
        const string SpellResourceFolder = "Racial/Human";

        static MageSpellCatalog _cached;
        static Dictionary<string, MageSpellDefinition> _spellLookup;

        public static MageSpellCatalog Instance
        {
            get
            {
                if (_cached == null)
                    _cached = Resources.Load<MageSpellCatalog>(DefaultResourcePath);

                return _cached;
            }
        }

        public static bool TryGetSpell(string spellId, out MageSpellDefinition spell)
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

            _spellLookup = new Dictionary<string, MageSpellDefinition>(StringComparer.OrdinalIgnoreCase);

            MageSpellCatalog catalog = Instance;
            if (catalog?.spells != null)
            {
                for (int i = 0; i < catalog.spells.Count; i++)
                    RegisterSpell(catalog.spells[i]);
            }

            if (_spellLookup.Count == 0)
            {
                MageSpellDefinition[] spells = Resources.LoadAll<MageSpellDefinition>(SpellResourceFolder);
                for (int i = 0; i < spells.Length; i++)
                    RegisterSpell(spells[i]);
            }
        }

        static void RegisterSpell(MageSpellDefinition spell)
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
