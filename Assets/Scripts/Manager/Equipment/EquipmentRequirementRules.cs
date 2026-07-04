using System.Text;
using JRogue.Item;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Manager.Equipment
{
    /// <summary>Class, level, and stat equip gates. See Docs/Equipment/Stat-And-Class-Equip-Requirements.md.</summary>
    public static class EquipmentRequirementRules
    {
        public static bool PassesMartialCalling(CharacterStats stats, ItemData item)
        {
            if (item == null || !item.requiresMartialCalling)
                return true;

            if (stats == null)
                return false;

            if (stats.race != Race.Human)
                return true;

            return stats.humanClass is HumanClass.None or HumanClass.Knight;
        }

        public static bool PassesCharacterLevel(CharacterStats stats, ItemData item)
        {
            if (item == null || item.minimumCharacterLevel <= 0)
                return true;

            return stats != null && stats.level >= item.minimumCharacterLevel;
        }

        public static bool PassesStatMinimums(CharacterStats stats, ItemData item)
        {
            if (item?.statMinimums == null || item.statMinimums.Length == 0)
                return true;

            if (stats == null)
                return false;

            for (int i = 0; i < item.statMinimums.Length; i++)
            {
                StatMinimumRequirement req = item.statMinimums[i];
                Stat stat = stats.GetStatByType(req.stat);
                int value = stat?.GetValue() ?? 0;
                if (value < req.minimumEffectiveValue)
                    return false;
            }

            return true;
        }

        public static bool TryGetFirstFailure(CharacterStats stats, ItemData item, out string reason)
        {
            reason = null;

            if (TryGetMartialCallingFailure(stats, item, out reason))
                return true;

            if (TryGetCharacterLevelFailure(stats, item, out reason))
                return true;

            if (TryGetStatMinimumFailure(stats, item, out reason))
                return true;

            return false;
        }

        public static bool TryGetMartialCallingFailure(CharacterStats stats, ItemData item, out string reason)
        {
            reason = null;
            if (item == null || !item.requiresMartialCalling || PassesMartialCalling(stats, item))
                return false;

            string classLabel = GetHumanClassPlural(stats?.humanClass ?? HumanClass.Mage);
            string verb = GetMartialFailureVerb(item);
            reason = $"{classLabel} cannot {verb}.";
            return true;
        }

        public static bool TryGetCharacterLevelFailure(CharacterStats stats, ItemData item, out string reason)
        {
            reason = null;
            if (item == null || item.minimumCharacterLevel <= 0 || PassesCharacterLevel(stats, item))
                return false;

            int current = stats?.level ?? 0;
            reason = $"Requires character level {item.minimumCharacterLevel} (you are level {current}).";
            return true;
        }

        public static bool TryGetStatMinimumFailure(CharacterStats stats, ItemData item, out string reason)
        {
            reason = null;
            if (item?.statMinimums == null || item.statMinimums.Length == 0)
                return false;

            for (int i = 0; i < item.statMinimums.Length; i++)
            {
                StatMinimumRequirement req = item.statMinimums[i];
                Stat stat = stats?.GetStatByType(req.stat);
                int value = stat?.GetValue() ?? 0;
                if (value >= req.minimumEffectiveValue)
                    continue;

                reason =
                    $"Requires {FormatStatName(req.stat)} {req.minimumEffectiveValue} (yours: {value}).";
                return true;
            }

            return false;
        }

        public static string FormatRequirementsBlock(ItemData item, CharacterStats stats = null)
        {
            if (item == null || !item.HasEquipRequirements)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("<color=#cfd6dd><b>Requirements</b></color>");

            if (item.requiresMartialCalling)
                AppendRequirementLine(sb, stats, PassesMartialCalling(stats, item),
                    "Martial class (not Mage or Priest)");

            if (item.minimumCharacterLevel > 0)
            {
                bool pass = PassesCharacterLevel(stats, item);
                string label = $"Character level {item.minimumCharacterLevel}";
                if (stats != null && !pass)
                    label += $" (yours: {stats.level})";
                AppendRequirementLine(sb, stats, pass, label);
            }

            if (item.statMinimums != null)
            {
                for (int i = 0; i < item.statMinimums.Length; i++)
                {
                    StatMinimumRequirement req = item.statMinimums[i];
                    Stat stat = stats?.GetStatByType(req.stat);
                    int value = stat?.GetValue() ?? 0;
                    bool pass = stats != null && value >= req.minimumEffectiveValue;
                    string label = $"{FormatStatName(req.stat)} {req.minimumEffectiveValue}";
                    if (stats != null && !pass)
                        label += $" (yours: {value})";
                    AppendRequirementLine(sb, stats, pass, label);
                }
            }

            return sb.ToString().TrimEnd();
        }

        static void AppendRequirementLine(StringBuilder sb, CharacterStats stats, bool pass, string label)
        {
            if (stats == null)
            {
                sb.AppendLine($" - {label}");
                return;
            }

            // ASCII markers only — LiberationSans SDF lacks ✓/✗ and TMP logs per missing glyph.
            string mark = pass ? "<color=#82e0b8>+</color>" : "<color=#c45a7a>x</color>";
            sb.AppendLine($" {mark} {label}");
        }

        static string GetMartialFailureVerb(ItemData item)
        {
            if (item.category == ItemCategory.Weapon || item.category == ItemCategory.Staff)
                return "wield this weapon";

            if (item.category == ItemCategory.Armor)
                return "wear this armor";

            return "equip this item";
        }

        static string GetHumanClassPlural(HumanClass humanClass) =>
            humanClass switch
            {
                HumanClass.Mage => "Mages",
                HumanClass.Priest => "Priests",
                HumanClass.Knight => "Knights",
                _ => "Your class"
            };

        public static string GetHumanClassSingular(HumanClass humanClass) =>
            humanClass switch
            {
                HumanClass.Mage => "Mage",
                HumanClass.Priest => "Priest",
                HumanClass.Knight => "Knight",
                HumanClass.None => "Unclassed Human",
                _ => humanClass.ToString()
            };

        static string FormatStatName(StatType stat) =>
            stat switch
            {
                StatType.Strength => "Strength",
                StatType.Dexterity => "Dexterity",
                StatType.Agility => "Agility",
                StatType.Constitution => "Constitution",
                StatType.Intelligence => "Intelligence",
                StatType.Wisdom => "Wisdom",
                StatType.Charisma => "Charisma",
                StatType.Luck => "Luck",
                _ => stat.ToString()
            };
    }
}
