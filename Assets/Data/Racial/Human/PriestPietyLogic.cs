using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>Piety band and invocation unlock rules (Data-layer; no Scripts assembly dependency).</summary>
    public static class PriestPietyLogic
    {
        const string ProgressionResourcePath = "Racial/Human/PriestPietyProgression_Default";
        public const int PenanceBlockedInvocationPietyThreshold = 20;

        static PriestPietyProgressionDefinition _progression;

        public static PriestPietyProgressionDefinition Progression
        {
            get
            {
                if (_progression == null)
                    _progression = Resources.Load<PriestPietyProgressionDefinition>(ProgressionResourcePath);

                return _progression;
            }
        }

        public static int ResolveMaxPiety() =>
            Progression != null ? Mathf.Max(1, Progression.maxPiety) : 100;

        public static int ResolveStartingPiety() =>
            Progression != null ? Mathf.Max(0, Progression.startingPietyOnCommit) : 10;

        public static int ResolveDevotionSlotCap(HumanPriestCovenantRuntime covenant) =>
            ResolveDevotionSlotCap(covenant != null ? covenant.Piety : 0);

        public static int ResolveDevotionSlotCap(int piety)
        {
            PriestPietyProgressionDefinition progression = Progression;
            if (progression?.bands == null || progression.bands.Count == 0)
                return piety >= 20 ? 3 : 2;

            int cap = 2;
            for (int i = 0; i < progression.bands.Count; i++)
            {
                PriestPietyBandData band = progression.bands[i];
                if (band == null)
                    continue;

                if (piety >= band.minPietyInclusive)
                    cap = Mathf.Max(cap, band.devotionSlots);
            }

            return cap;
        }

        public static PriestPietyBandData ResolveCurrentBand(HumanPriestCovenantRuntime covenant) =>
            ResolveCurrentBand(covenant != null ? covenant.Piety : 0);

        public static PriestPietyBandData ResolveCurrentBand(int piety)
        {
            PriestPietyProgressionDefinition progression = Progression;
            if (progression?.bands == null || progression.bands.Count == 0)
                return null;

            PriestPietyBandData current = null;
            for (int i = 0; i < progression.bands.Count; i++)
            {
                PriestPietyBandData band = progression.bands[i];
                if (band == null || piety < band.minPietyInclusive)
                    continue;

                if (current == null || band.minPietyInclusive >= current.minPietyInclusive)
                    current = band;
            }

            return current;
        }

        public static bool IsInvocationUnlocked(
            CharacterStats stats,
            HumanPriestCovenantRuntime covenant,
            PriestInvocationDefinition invocation)
        {
            if (invocation == null || stats == null || covenant == null)
                return false;

            if (stats.level < invocation.requiredCharacterLevel)
                return false;

            if (covenant.Piety < invocation.requiredPiety)
                return false;

            if (!string.IsNullOrWhiteSpace(invocation.requiredSealId)
                && !covenant.HasSeal(invocation.requiredSealId))
            {
                return false;
            }

            if (IsInvocationBlockedByPenance(covenant, invocation))
                return false;

            return true;
        }

        public static bool IsInvocationBlockedByPenance(
            HumanPriestCovenantRuntime covenant,
            PriestInvocationDefinition invocation)
        {
            if (covenant == null || invocation == null)
                return false;

            return covenant.PenanceDebt > 0
                && invocation.requiredPiety >= PenanceBlockedInvocationPietyThreshold;
        }

        public static string BuildLockedReason(
            CharacterStats stats,
            HumanPriestCovenantRuntime covenant,
            PriestInvocationDefinition invocation)
        {
            if (invocation == null)
                return "Unknown invocation.";

            if (stats != null && stats.level < invocation.requiredCharacterLevel)
                return $"Requires character level {invocation.requiredCharacterLevel}.";

            if (covenant != null && covenant.Piety < invocation.requiredPiety)
                return $"Requires piety {invocation.requiredPiety} (have {covenant.Piety}).";

            if (!string.IsNullOrWhiteSpace(invocation.requiredSealId)
                && (covenant == null || !covenant.HasSeal(invocation.requiredSealId)))
            {
                return $"Requires Covenant Seal '{invocation.requiredSealId}'.";
            }

            if (covenant != null && IsInvocationBlockedByPenance(covenant, invocation))
                return "Repent at the shrine — penance blocks high-tier invocations.";

            return "Invocation locked.";
        }

        public static void ApplyBandPassives(GameObject actor, HumanPriestCovenantRuntime covenant)
        {
            if (covenant == null)
                return;

            covenant.ApplyBandPassives(ResolveCurrentBand(covenant));
        }

        public static void ResetCacheForTests() => _progression = null;
    }
}
