using System.Collections.Generic;
using JRogue.Manager.Party;

namespace JRogue.World.Altar
{
    public readonly struct AltarPlaceableStack
    {
        public readonly int Tier;
        public readonly string SourceSpeciesId;
        public readonly int Count;

        public AltarPlaceableStack(int tier, string sourceSpeciesId, int count)
        {
            Tier = tier;
            SourceSpeciesId = sourceSpeciesId ?? string.Empty;
            Count = count;
        }
    }

    public static class AltarPlaceListBuilder
    {
        public static void BuildPlaceableStacks(
            AltarInstance instance,
            PartyManaStoneLedger ledger,
            List<AltarPlaceableStack> dest)
        {
            dest.Clear();
            if (instance == null || ledger == null)
                return;

            var neededTiers = new HashSet<int>();
            CollectNeededTiers(instance, neededTiers);
            if (neededTiers.Count == 0)
                return;

            foreach (KeyValuePair<PartyManaStoneLedger.ManaStoneStackKey, int> kv in ledger.Snapshot)
            {
                if (kv.Value <= 0)
                    continue;

                if (!neededTiers.Contains(kv.Key.Tier))
                    continue;

                dest.Add(new AltarPlaceableStack(kv.Key.Tier, kv.Key.SourceSpeciesId, kv.Value));
            }

            dest.Sort((a, b) =>
            {
                int tier = b.Tier.CompareTo(a.Tier);
                if (tier != 0)
                    return tier;

                return string.Compare(
                    a.SourceSpeciesId,
                    b.SourceSpeciesId,
                    System.StringComparison.OrdinalIgnoreCase);
            });
        }

        public static void CollectNeededTiers(AltarInstance instance, HashSet<int> dest)
        {
            dest.Clear();
            if (instance?.Definition?.slots == null)
                return;

            for (int i = 0; i < instance.Definition.slots.Length; i++)
            {
                if (i >= instance.Slots.Count || !instance.Slots[i].IsEmpty)
                    continue;

                AltarSlotDefinition slotDef = instance.Definition.slots[i];
                if (slotDef?.acceptFilter == null)
                    continue;

                if (AltarSlotFilters.TryGetManaStoneTier(slotDef.acceptFilter, out int tier))
                    dest.Add(tier);
            }
        }

        public static string BuildPlaceListHeader(AltarInstance instance)
        {
            var tiers = new HashSet<int>();
            CollectNeededTiers(instance, tiers);
            if (tiers.Count == 0)
                return "YOUR MANA STONES";

            var sorted = new List<int>(tiers);
            sorted.Sort((a, b) => b.CompareTo(a));

            if (sorted.Count == 1)
                return $"YOUR MANA STONES (tier {sorted[0]} only)";

            if (sorted.Count == 2)
                return $"YOUR MANA STONES (tier {sorted[0]} and tier {sorted[1]})";

            return "YOUR MANA STONES";
        }
    }
}
