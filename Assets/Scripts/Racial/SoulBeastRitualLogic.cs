using System;
using System.Collections.Generic;
using JRogue.Item;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Racial
{
    public readonly struct SoulBeastWeightedCandidate
    {
        public SoulBeastDefinition Beast { get; }
        public int Weight { get; }

        public SoulBeastWeightedCandidate(SoulBeastDefinition beast, int weight)
        {
            Beast = beast;
            Weight = weight;
        }
    }

    public static class SoulBeastRitualLogic
    {
        public static bool CanBeginRitual(out string rejectLine)
        {
            rejectLine = null;

            if (!SafeZonePolicyService.IsSafeZoneForActiveParty())
            {
                rejectLine = "You can only perform Soul Beast rituals in town.";
                return false;
            }

            if (SoulBeastPartyRules.GetEligibleBeastmen(requireUnbonded: true).Count == 0)
            {
                rejectLine = "No unbonded Beastman can perform a ritual.";
                return false;
            }

            return true;
        }

        public static List<SoulBeastWeightedCandidate> BuildWeightedPool(
            SoulBeastRegistry registry,
            SoulBeastRitualTypeDefinition ritualType,
            IReadOnlyList<ItemData> offerings)
        {
            var pool = new List<SoulBeastWeightedCandidate>();
            if (registry?.Beasts == null || ritualType == null)
                return pool;

            var allowedTypes = new HashSet<SoulBeastType>(ritualType.allowedSoulBeastTypes ?? new List<SoulBeastType>());
            var baseWeightById = BuildBaseWeightLookup(ritualType);

            foreach (SoulBeastDefinition beast in registry.Beasts)
            {
                if (beast == null || string.IsNullOrEmpty(beast.soulBeastId))
                    continue;

                if (allowedTypes.Count > 0 && !allowedTypes.Contains(beast.soulBeastType))
                    continue;

                if (!PassesOfferingFilters(beast, ritualType, offerings))
                    continue;

                int weight = ResolveBaseWeight(beast.soulBeastId, baseWeightById);
                weight += SumOfferingBonuses(beast, ritualType, offerings);
                if (weight <= 0)
                    continue;

                pool.Add(new SoulBeastWeightedCandidate(beast, weight));
            }

            return pool;
        }

        public static SoulBeastDefinition RollAppearance(
            IReadOnlyList<SoulBeastWeightedCandidate> pool,
            int noneOutcomeWeight,
            System.Random rng)
        {
            if (pool == null || pool.Count == 0)
                return null;

            int beastWeightTotal = 0;
            for (int i = 0; i < pool.Count; i++)
                beastWeightTotal += Mathf.Max(0, pool[i].Weight);

            int noneWeight = Mathf.Max(0, noneOutcomeWeight);
            int total = beastWeightTotal + noneWeight;
            if (total <= 0)
                return null;

            int roll = rng.Next(0, total);
            if (roll < noneWeight)
                return null;

            int cursor = noneWeight;
            for (int i = 0; i < pool.Count; i++)
            {
                int weight = Mathf.Max(0, pool[i].Weight);
                cursor += weight;
                if (roll < cursor)
                    return pool[i].Beast;
            }

            return null;
        }

        static Dictionary<string, int> BuildBaseWeightLookup(SoulBeastRitualTypeDefinition ritualType)
        {
            var lookup = new Dictionary<string, int>();
            if (ritualType.baseWeights == null)
                return lookup;

            foreach (SoulBeastWeightEntry entry in ritualType.baseWeights)
            {
                if (entry == null || string.IsNullOrEmpty(entry.soulBeastId))
                    continue;

                lookup[entry.soulBeastId] = Mathf.Max(0, entry.weight);
            }

            return lookup;
        }

        static int ResolveBaseWeight(string soulBeastId, Dictionary<string, int> baseWeightById)
        {
            if (baseWeightById.TryGetValue(soulBeastId, out int weight))
                return weight;

            return 1;
        }

        static bool PassesOfferingFilters(
            SoulBeastDefinition beast,
            SoulBeastRitualTypeDefinition ritualType,
            IReadOnlyList<ItemData> offerings)
        {
            if (offerings == null || offerings.Count == 0)
                return true;

            for (int i = 0; i < offerings.Count; i++)
            {
                ItemData item = offerings[i];
                if (item == null)
                    continue;

                SoulBeastRitualOfferingDefinition offering = ResolveOffering(item);
                if (offering == null)
                    continue;

                if (!IsOfferingCompatibleWithRitual(offering, ritualType))
                    continue;

                if (IsExcludedByOffering(beast, offering))
                    return false;

                if (offering.poolFilterTags == null || offering.poolFilterTags.Count == 0)
                    continue;

                bool tagMatch = false;
                foreach (string tag in offering.poolFilterTags)
                {
                    if (beast.HasTag(tag))
                    {
                        tagMatch = true;
                        break;
                    }
                }

                if (!tagMatch)
                    return false;
            }

            return true;
        }

        static int SumOfferingBonuses(
            SoulBeastDefinition beast,
            SoulBeastRitualTypeDefinition ritualType,
            IReadOnlyList<ItemData> offerings)
        {
            int bonus = 0;
            if (offerings == null)
                return bonus;

            for (int i = 0; i < offerings.Count; i++)
            {
                SoulBeastRitualOfferingDefinition offering = ResolveOffering(offerings[i]);
                if (offering == null || !IsOfferingCompatibleWithRitual(offering, ritualType))
                    continue;

                if (offering.soulBeastWeightBonuses != null)
                {
                    foreach (SoulBeastIdWeightBonus entry in offering.soulBeastWeightBonuses)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.soulBeastId))
                            continue;

                        if (entry.soulBeastId == beast.soulBeastId)
                            bonus += Mathf.Max(0, entry.bonusWeight);
                    }
                }

                if (offering.tagWeightBonuses != null)
                {
                    foreach (SoulBeastTagWeightBonus entry in offering.tagWeightBonuses)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.tag))
                            continue;

                        if (beast.HasTag(entry.tag))
                            bonus += Mathf.Max(0, entry.bonusWeight);
                    }
                }
            }

            return bonus;
        }

        static SoulBeastRitualOfferingDefinition ResolveOffering(ItemData item)
        {
            if (item is RitualOfferingItemData offeringItem)
                return offeringItem.ritualOffering;

            return null;
        }

        static bool IsOfferingCompatibleWithRitual(
            SoulBeastRitualOfferingDefinition offering,
            SoulBeastRitualTypeDefinition ritualType)
        {
            if (offering.requiredRitualTypeIds == null || offering.requiredRitualTypeIds.Count == 0)
                return true;

            if (ritualType == null || string.IsNullOrEmpty(ritualType.ritualTypeId))
                return false;

            foreach (string requiredId in offering.requiredRitualTypeIds)
            {
                if (requiredId == ritualType.ritualTypeId)
                    return true;
            }

            return false;
        }

        static bool IsExcludedByOffering(SoulBeastDefinition beast, SoulBeastRitualOfferingDefinition offering)
        {
            if (offering.poolExcludeSoulBeastIds == null)
                return false;

            foreach (string excludedId in offering.poolExcludeSoulBeastIds)
            {
                if (excludedId == beast.soulBeastId)
                    return true;
            }

            return false;
        }
    }
}
