using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Racial;
using JRogue.Stats;

namespace JRogue.Progression.Proficiency
{
    public static class ProficiencyMageSpellTags
    {
        public static IReadOnlyList<ProficiencyKind> Resolve(MageSpellDefinition spell, AbilityAction ability)
        {
            var tags = new List<ProficiencyKind>();

            if (spell?.proficiencyTags != null && spell.proficiencyTags.Count > 0)
            {
                for (int i = 0; i < spell.proficiencyTags.Count; i++)
                {
                    ProficiencyKind tag = spell.proficiencyTags[i];
                    if (tag != ProficiencyKind.None && !tags.Contains(tag))
                        tags.Add(tag);
                }
            }
            else if (ability?.proficiencyTags != null && ability.proficiencyTags.Count > 0)
            {
                for (int i = 0; i < ability.proficiencyTags.Count; i++)
                {
                    ProficiencyKind tag = ability.proficiencyTags[i];
                    if (tag != ProficiencyKind.None && !tags.Contains(tag))
                        tags.Add(tag);
                }
            }

            if (!tags.Contains(ProficiencyKind.Spellcasting))
                tags.Insert(0, ProficiencyKind.Spellcasting);

            if (tags.Count == 1)
                InferSchoolFromAbilityName(ability, tags);

            return tags;
        }

        static void InferSchoolFromAbilityName(AbilityAction ability, List<ProficiencyKind> tags)
        {
            if (ability == null)
                return;

            string name = ability.abilityName ?? ability.name;
            if (string.IsNullOrWhiteSpace(name))
                return;

            string lower = name.ToLowerInvariant();
            if (lower.Contains("fire") && !tags.Contains(ProficiencyKind.FireMagic))
                tags.Add(ProficiencyKind.FireMagic);
            else if ((lower.Contains("cold") || lower.Contains("ice")) && !tags.Contains(ProficiencyKind.IceMagic))
                tags.Add(ProficiencyKind.IceMagic);
            else if (lower.Contains("lightning") && !tags.Contains(ProficiencyKind.AirMagic))
                tags.Add(ProficiencyKind.AirMagic);
            else if (lower.Contains("teleport") && !tags.Contains(ProficiencyKind.Translocations))
                tags.Add(ProficiencyKind.Translocations);
        }

        public static DamageType? DamageTypeForSchoolTag(ProficiencyKind kind) =>
            kind switch
            {
                ProficiencyKind.FireMagic => DamageType.Fire,
                ProficiencyKind.IceMagic => DamageType.Cold,
                ProficiencyKind.AirMagic => DamageType.Lightning,
                ProficiencyKind.EarthMagic => DamageType.Blunt,
                ProficiencyKind.Alchemy => DamageType.Poison,
                _ => null,
            };
    }
}
