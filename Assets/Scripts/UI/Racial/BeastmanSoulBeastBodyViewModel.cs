using System.Collections.Generic;
using System.Text;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.UI.Racial
{
    public enum BeastmanSoulBeastAbilityKind
    {
        Passive = 0,
        Active = 1
    }

    public sealed class BeastmanSoulBeastAbilityRowModel
    {
        public BeastmanSoulBeastAbilityKind Kind;
        public int SourceLevel;
        public string Title = string.Empty;
        public string Description = string.Empty;
        public string LevelTag = string.Empty;
        public string Meta = string.Empty;
        public bool ShowHotbarFootnote;
        public Sprite Icon;
    }

    public sealed class BeastmanSoulBeastBondSummaryModel
    {
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public string Description = string.Empty;
        public string StatsLine = string.Empty;
        public string ResistancesLine = string.Empty;
        public string ProgressHint = string.Empty;
    }

    public sealed class BeastmanSoulBeastBodyViewModel
    {
        public const string BannerText =
            "View only — form a contract at the Soul Beast Ritual Circle; deepen the bond with Beast Blood from the merchant in town.";

        public const string CannotHostBondMessage =
            "This character cannot form a Soul Beast contract.";

        public const string UnbondedTitle = "No Soul Beast contract";

        public const string UnbondedBody =
            "Perform a ritual at the Soul Beast Ritual Circle in town\n" +
            "to attract a permanent Soul Beast companion.";

        public const string EmptyAbilitiesHint =
            "No special abilities yet — stat bonuses only.";

        public const string UnknownBeastTitle = "Unknown Soul Beast";

        public bool IsBonded;
        public bool ShowEmptyAbilitiesHint;
        public string EmptyStateTitle = string.Empty;
        public string EmptyStateBody = string.Empty;
        public BeastmanSoulBeastBondSummaryModel Summary = new BeastmanSoulBeastBondSummaryModel();
        public List<BeastmanSoulBeastAbilityRowModel> AbilityRows = new List<BeastmanSoulBeastAbilityRowModel>();

        public static BeastmanSoulBeastBodyViewModel Build(BaseActor beastman)
        {
            var vm = new BeastmanSoulBeastBodyViewModel();
            if (beastman == null)
                return vm;

            BeastmanSoulBeastRuntime runtime = beastman.GetComponent<BeastmanSoulBeastRuntime>();
            if (runtime == null || !runtime.IsBonded)
            {
                vm.IsBonded = false;
                vm.EmptyStateTitle = UnbondedTitle;
                vm.EmptyStateBody = UnbondedBody;
                return vm;
            }

            vm.IsBonded = true;
            if (!runtime.TryResolveBondedDefinition(out SoulBeastDefinition beast) || beast == null)
            {
                vm.Summary.Title = UnknownBeastTitle;
                vm.Summary.Subtitle = runtime.SoulBeastId ?? string.Empty;
                vm.ShowEmptyAbilitiesHint = true;
                return vm;
            }

            CharacterStats stats = beastman.stats;
            int level = Mathf.Max(1, runtime.SoulBeastLevel);
            int cap = SoulBeastProgressionLogic.GetEffectiveLevelCap(stats, beast);
            vm.Summary = BuildBondSummary(beast, level, cap);
            vm.AbilityRows = FlattenAbilities(beast, level);
            vm.ShowEmptyAbilitiesHint = vm.AbilityRows.Count == 0;
            return vm;
        }

        static BeastmanSoulBeastBondSummaryModel BuildBondSummary(SoulBeastDefinition beast, int level, int cap)
        {
            var summary = new BeastmanSoulBeastBondSummaryModel
            {
                Title = ResolveBeastTitle(beast),
                Subtitle = $"{FormatSoulBeastType(beast.soulBeastType)} · Level {level} / Cap {cap}",
                Description = string.IsNullOrWhiteSpace(beast.description) ? string.Empty : beast.description.Trim(),
                StatsLine = BuildStatsLine(beast, level),
                ResistancesLine = BuildResistancesLine(beast, level),
                ProgressHint = level >= cap
                    ? "Bond at maximum for your level."
                    : "Use Beast Blood to deepen the bond.",
            };

            return summary;
        }

        static List<BeastmanSoulBeastAbilityRowModel> FlattenAbilities(SoulBeastDefinition beast, int level)
        {
            var rows = new List<BeastmanSoulBeastAbilityRowModel>();
            if (beast?.levels == null)
                return rows;

            int clampedLevel = Mathf.Clamp(level, 1, beast.maxLevel);
            BeastmanSoulBeastAbilityRowModel lastActive = null;

            for (int rowLevel = 1; rowLevel <= clampedLevel; rowLevel++)
            {
                if (!beast.TryGetLevelRow(rowLevel, out SoulBeastLevelData row) || row == null)
                    continue;

                if (row.passiveEffects != null)
                {
                    foreach (PassiveEffect passive in row.passiveEffects)
                    {
                        if (passive == null)
                            continue;

                        rows.Add(new BeastmanSoulBeastAbilityRowModel
                        {
                            Kind = BeastmanSoulBeastAbilityKind.Passive,
                            SourceLevel = rowLevel,
                            Title = string.IsNullOrWhiteSpace(passive.name) ? "Passive" : passive.name.Trim(),
                            Description = passive.effectDescription?.Trim() ?? string.Empty,
                            LevelTag = $"Level {rowLevel} · Passive",
                        });
                    }
                }

                if (row.activeAbilities == null)
                    continue;

                foreach (AbilityAction active in row.activeAbilities)
                {
                    if (active == null)
                        continue;

                    var abilityRow = new BeastmanSoulBeastAbilityRowModel
                    {
                        Kind = BeastmanSoulBeastAbilityKind.Active,
                        SourceLevel = rowLevel,
                        Title = ResolveAbilityName(active),
                        Description = active.description?.Trim() ?? string.Empty,
                        Meta = FormatAbilityMeta(active),
                        LevelTag = $"Level {rowLevel} · Active",
                        Icon = active.hotbarIcon,
                    };
                    rows.Add(abilityRow);
                    lastActive = abilityRow;
                }
            }

            if (lastActive != null)
                lastActive.ShowHotbarFootnote = true;

            return rows;
        }

        static string ResolveBeastTitle(SoulBeastDefinition beast)
        {
            if (!string.IsNullOrWhiteSpace(beast.displayName))
                return beast.displayName.Trim();

            return string.IsNullOrWhiteSpace(beast.soulBeastId) ? "Soul Beast" : beast.soulBeastId.Trim();
        }

        static string FormatSoulBeastType(SoulBeastType type) =>
            type switch
            {
                SoulBeastType.Summoning => "Summoning",
                SoulBeastType.Enhancement => "Enhancement",
                SoulBeastType.SpecialAbility => "Special Ability",
                SoulBeastType.Specialist => "Specialist",
                _ => type.ToString(),
            };

        static string BuildStatsLine(SoulBeastDefinition beast, int level)
        {
            var totals = new Dictionary<StatType, int>();
            AccumulateStats(beast, level, totals);
            if (totals.Count == 0)
                return string.Empty;

            var sb = new StringBuilder("STATS · ");
            bool first = true;
            foreach (KeyValuePair<StatType, int> pair in totals)
            {
                if (!first)
                    sb.Append(" · ");

                first = false;
                string sign = pair.Value >= 0 ? "+" : string.Empty;
                sb.Append($"{sign}{pair.Value} {pair.Key}");
            }

            return sb.ToString();
        }

        static string BuildResistancesLine(SoulBeastDefinition beast, int level)
        {
            var totals = new Dictionary<DamageType, int>();
            AccumulateResistances(beast, level, totals);
            if (totals.Count == 0)
                return string.Empty;

            var sb = new StringBuilder("RESISTANCES · ");
            bool first = true;
            foreach (KeyValuePair<DamageType, int> pair in totals)
            {
                if (!first)
                    sb.Append(" · ");

                first = false;
                string sign = pair.Value >= 0 ? "+" : string.Empty;
                sb.Append($"{sign}{pair.Value}% {pair.Key}");
            }

            return sb.ToString();
        }

        static void AccumulateStats(SoulBeastDefinition beast, int level, Dictionary<StatType, int> totals)
        {
            for (int rowLevel = 1; rowLevel <= level; rowLevel++)
            {
                if (!beast.TryGetLevelRow(rowLevel, out SoulBeastLevelData row) || row?.statModifiers == null)
                    continue;

                foreach (AttributeModifier mod in row.statModifiers)
                {
                    if (totals.ContainsKey(mod.attribute))
                        totals[mod.attribute] += mod.value;
                    else
                        totals[mod.attribute] = mod.value;
                }
            }
        }

        static void AccumulateResistances(SoulBeastDefinition beast, int level, Dictionary<DamageType, int> totals)
        {
            for (int rowLevel = 1; rowLevel <= level; rowLevel++)
            {
                if (!beast.TryGetLevelRow(rowLevel, out SoulBeastLevelData row) || row?.resistanceModifiers == null)
                    continue;

                foreach (DamageResistanceModifier mod in row.resistanceModifiers)
                {
                    if (totals.ContainsKey(mod.type))
                        totals[mod.type] += mod.value;
                    else
                        totals[mod.type] = mod.value;
                }
            }
        }

        static string ResolveAbilityName(AbilityAction ability)
        {
            if (!string.IsNullOrWhiteSpace(ability.abilityName))
                return ability.abilityName.Trim();

            return ability.name;
        }

        static string FormatAbilityMeta(AbilityAction active)
        {
            var parts = new List<string>();
            if (active.soulPowerCost != 0)
                parts.Add($"Soul {active.soulPowerCost}");
            if (active.magicPowerCost != 0)
                parts.Add($"Magic {active.magicPowerCost}");
            if (active.divinePowerCost != 0)
                parts.Add($"Divine {active.divinePowerCost}");
            if (active.cooldownTurns != 0)
                parts.Add($"CD {active.cooldownTurns}");

            return parts.Count == 0 ? string.Empty : string.Join(" · ", parts);
        }
    }
}
