using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.UI.Racial
{
    public sealed class ElfElementalSpiritPassiveLine
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public sealed class ElfElementalSpiritActiveLine
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Meta { get; set; }
    }

    public sealed class ElfElementalSpiritContractCard
    {
        public string ContractInstanceId { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Nickname { get; set; }
        public string ProgressLine { get; set; }
        public string CapLine { get; set; }
        public string ElementLine { get; set; }
        public string CostsLine { get; set; }
        public bool IsSummoned { get; set; }
        public int ContractLevel { get; set; }
        public List<ElfElementalSpiritPassiveLine> Passives { get; } = new List<ElfElementalSpiritPassiveLine>();
        public List<ElfElementalSpiritActiveLine> Actives { get; } = new List<ElfElementalSpiritActiveLine>();
    }

    public static class ElfElementalSpiritViewModel
    {
        public const string EmptyRosterMessage =
            "No elemental spirit contracts yet.\n\nBuy Fairy Stones from the Fairy Merchant and use them on an Elf in your party.";

        public const string BannerText =
            "View only — form new contracts at the Fairy Merchant; deepen bonds at the Meditation Shrine in town.";

        public static IReadOnlyList<ElfElementalSpiritContractCard> Build(BaseActor elf)
        {
            var cards = new List<ElfElementalSpiritContractCard>();
            if (elf == null)
                return cards;

            ElementalSpiritContractsRuntime runtime = elf.GetComponent<ElementalSpiritContractsRuntime>();
            if (runtime?.ContractedSpirits == null || runtime.ContractedSpirits.Count == 0)
                return cards;

            IReadOnlyList<ElementalSpiritContractPreset> roster = runtime.ContractedSpirits;
            List<ElementalSpiritContractPreset> sorted = ElementalSpiritRosterSort.Apply(roster);
            for (int i = 0; i < sorted.Count; i++)
            {
                ElementalSpiritContractPreset preset = sorted[i];
                preset.EnsureInstanceId();
                cards.Add(BuildCard(elf, runtime, preset, roster));
            }

            return cards;
        }

        static ElfElementalSpiritContractCard BuildCard(
            BaseActor elf,
            ElementalSpiritContractsRuntime runtime,
            ElementalSpiritContractPreset preset,
            IReadOnlyList<ElementalSpiritContractPreset> roster)
        {
            ElementalSpiritXpProgress progress = ElementalSpiritProgressionLogic.GetXpProgress(elf, preset);
            string canonicalName = ElementalSpiritDisplayNames.GetCanonicalInstanceName(preset, roster);
            string nickname = ElementalSpiritDisplayNames.NormalizeNickname(preset.nickname);
            bool hasNickname = !string.IsNullOrEmpty(nickname);
            string title = hasNickname ? nickname : canonicalName;
            string subtitle = hasNickname
                ? canonicalName
                : $"{FormatElement(preset.spirit.element)} · {preset.spirit.spiritId}";

            var card = new ElfElementalSpiritContractCard
            {
                ContractInstanceId = preset.contractInstanceId,
                Title = title,
                Subtitle = subtitle,
                Nickname = nickname,
                ProgressLine = BuildProgressLine(progress),
                CapLine = $"Level cap: {progress.EffectiveCap} (your character level)",
                ElementLine = FormatElement(preset.spirit.element),
                CostsLine =
                    $"Summon {preset.spirit.summonSoulPowerCost} SP · Upkeep {preset.spirit.upkeepSoulPowerPerTurn} SP/turn",
                IsSummoned = runtime.IsInstanceSummoned(preset.contractInstanceId),
                ContractLevel = progress.ContractLevel,
            };

            AppendPayloadLines(card, preset);
            return card;
        }

        static string BuildProgressLine(ElementalSpiritXpProgress progress)
        {
            if (progress.XpToNextLevel == int.MaxValue)
                return $"Bond XP — contract level {progress.ContractLevel} (max spirit level reached)";

            if (progress.IsCappedForXpGain)
                return $"Bond XP {progress.ContractExperience} banked — contract level {progress.ContractLevel} (at cap)";

            return
                $"Bond XP {progress.ContractExperience}/{progress.XpToNextLevel} — contract level {progress.ContractLevel}";
        }

        static string FormatElement(ElementalElement element) =>
            element switch
            {
                ElementalElement.Fire => "Fire",
                ElementalElement.Water => "Water",
                ElementalElement.Earth => "Earth",
                ElementalElement.Wind => "Wind",
                _ => element.ToString(),
            };

        static void AppendPayloadLines(ElfElementalSpiritContractCard card, ElementalSpiritContractPreset preset)
        {
            ElementalSpiritDefinition spirit = preset.spirit;
            if (spirit?.levels == null)
                return;

            int contractLevel = Mathf.Max(1, preset.contractLevel);
            for (int level = 1; level <= contractLevel; level++)
            {
                if (!spirit.TryGetLevelRow(level, out ElementalSpiritLevelData row))
                    continue;

                if (row.passiveEffects != null)
                {
                    foreach (PassiveEffect passive in row.passiveEffects)
                    {
                        if (passive == null)
                            continue;

                        card.Passives.Add(new ElfElementalSpiritPassiveLine
                        {
                            Name = passive.name,
                            Description = passive.effectDescription,
                        });
                    }
                }

                if (row.activeEntries == null)
                    continue;

                foreach (ElementalSpiritActiveEntry activeEntry in row.activeEntries)
                {
                    AbilityAction ability = activeEntry?.ability;
                    if (ability == null)
                        continue;

                    card.Actives.Add(new ElfElementalSpiritActiveLine
                    {
                        Name = ResolveAbilityName(ability),
                        Description = ability.description,
                        Meta = FormatAbilityMeta(ability),
                    });
                }
            }
        }

        static string ResolveAbilityName(AbilityAction ability)
        {
            if (!string.IsNullOrWhiteSpace(ability.abilityName))
                return ability.abilityName.Trim();

            return ability.name;
        }

        static string FormatAbilityMeta(AbilityAction ability)
        {
            var parts = new List<string>();
            if (ability.soulPowerCost != 0)
                parts.Add($"Soul {ability.soulPowerCost}");
            if (ability.magicPowerCost != 0)
                parts.Add($"Magic {ability.magicPowerCost}");
            if (ability.divinePowerCost != 0)
                parts.Add($"Divine {ability.divinePowerCost}");
            if (ability.cooldownTurns != 0)
                parts.Add($"CD {ability.cooldownTurns}");

            return parts.Count == 0 ? string.Empty : string.Join(" · ", parts);
        }
    }
}
