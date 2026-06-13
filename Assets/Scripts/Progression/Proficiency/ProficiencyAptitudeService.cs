using System.Collections.Generic;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Progression.Proficiency
{
    public static class ProficiencyAptitudeService
    {
        static readonly Dictionary<(Race race, HumanClass humanClass, ProficiencyKind kind), int> Overrides =
            new()
            {
                { (Race.Dwarf, HumanClass.None, ProficiencyKind.Weapon_Axe), 2 },
                { (Race.Dwarf, HumanClass.None, ProficiencyKind.Weapon_Mace), 2 },
                { (Race.Dwarf, HumanClass.None, ProficiencyKind.Armour), 2 },
                { (Race.Dwarf, HumanClass.None, ProficiencyKind.Weapon_Bow), -2 },
                { (Race.Elf, HumanClass.None, ProficiencyKind.Weapon_Bow), 2 },
                { (Race.Barbarian, HumanClass.None, ProficiencyKind.Fighting), 2 },
                { (Race.Dragonian, HumanClass.None, ProficiencyKind.DraconicSpellcraft), 3 },
                { (Race.Human, HumanClass.Mage, ProficiencyKind.Spellcasting), 2 },
                { (Race.Human, HumanClass.Mage, ProficiencyKind.FireMagic), 1 },
            };

        public static int GetAptitude(CharacterStats stats, ProficiencyKind kind)
        {
            if (stats == null || kind == ProficiencyKind.None)
                return 0;

            HumanClass cls = stats.humanClass;
            if (Overrides.TryGetValue((stats.race, cls, kind), out int exact))
                return exact;

            if (cls == HumanClass.None
                && Overrides.TryGetValue((stats.race, HumanClass.None, kind), out int folkOnly))
            {
                return folkOnly;
            }

            return 0;
        }

        public static float GetXpMultiplier(int aptitude) =>
            aptitude switch
            {
                4 => 0.20f,
                3 => 0.33f,
                2 => 0.50f,
                1 => 0.67f,
                0 => 1.00f,
                -1 => 1.50f,
                -2 => 2.00f,
                -3 => 3.00f,
                -4 => 5.00f,
                _ => 1.00f,
            };
    }
}
