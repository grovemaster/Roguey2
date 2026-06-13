using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.Stats;

namespace JRogue.UI.Racial
{
    public sealed class ElfElementalSpiritContractCard
    {
        public string Title { get; set; }
        public string ProgressLine { get; set; }
        public string CapLine { get; set; }
    }

    public static class ElfElementalSpiritViewModel
    {
        public const string EmptyRosterMessage =
            "No elemental spirit contracts yet.\n\nUse Fairy Stones from the Fairy Merchant to form contracts, then visit the Meditation Shrine in town to deepen your bond.";

        public const string BannerText =
            "Elemental spirit contracts — meditate in town to gain bond experience and raise contract level.";

        public static IReadOnlyList<ElfElementalSpiritContractCard> Build(BaseActor elf)
        {
            var cards = new List<ElfElementalSpiritContractCard>();
            if (elf == null)
                return cards;

            ElementalSpiritContractsRuntime runtime = elf.GetComponent<ElementalSpiritContractsRuntime>();
            if (runtime?.ContractedSpirits == null || runtime.ContractedSpirits.Count == 0)
                return cards;

            IReadOnlyList<ElementalSpiritContractPreset> roster = runtime.ContractedSpirits;
            for (int i = 0; i < roster.Count; i++)
            {
                ElementalSpiritContractPreset preset = roster[i];
                if (preset?.spirit == null)
                    continue;

                preset.EnsureInstanceId();
                ElementalSpiritXpProgress progress = ElementalSpiritProgressionLogic.GetXpProgress(elf, preset);
                string title = ElementalSpiritProgressionLogic.BuildInstanceDisplayName(preset, roster);
                string progressLine;
                if (progress.XpToNextLevel == int.MaxValue)
                {
                    progressLine = $"Contract level {progress.ContractLevel} — max spirit level reached.";
                }
                else if (progress.IsCappedForXpGain)
                {
                    progressLine = $"Contract level {progress.ContractLevel} — {progress.ContractExperience} bond XP banked.";
                }
                else
                {
                    progressLine =
                        $"Contract level {progress.ContractLevel} — {progress.ContractExperience}/{progress.XpToNextLevel} bond XP to next level.";
                }

                cards.Add(new ElfElementalSpiritContractCard
                {
                    Title = title,
                    ProgressLine = progressLine,
                    CapLine = $"Level cap: {progress.EffectiveCap} (your character level)",
                });
            }

            return cards;
        }
    }
}
