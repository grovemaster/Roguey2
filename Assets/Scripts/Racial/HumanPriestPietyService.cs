using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>Scripts-layer facade over <see cref="PriestPietyLogic"/>.</summary>
    public static class HumanPriestPietyService
    {
        public static PriestPietyProgressionDefinition Progression => PriestPietyLogic.Progression;

        public static int ResolveMaxPiety() => PriestPietyLogic.ResolveMaxPiety();

        public static int ResolveStartingPiety() => PriestPietyLogic.ResolveStartingPiety();

        public static int ResolveDevotionSlotCap(HumanPriestCovenantRuntime covenant) =>
            PriestPietyLogic.ResolveDevotionSlotCap(covenant);

        public static int ResolveDevotionSlotCap(int piety) =>
            PriestPietyLogic.ResolveDevotionSlotCap(piety);

        public static PriestPietyBandData ResolveCurrentBand(HumanPriestCovenantRuntime covenant) =>
            PriestPietyLogic.ResolveCurrentBand(covenant);

        public static PriestPietyBandData ResolveCurrentBand(int piety) =>
            PriestPietyLogic.ResolveCurrentBand(piety);

        public static bool IsInvocationUnlocked(
            CharacterStats stats,
            HumanPriestCovenantRuntime covenant,
            PriestInvocationDefinition invocation) =>
            PriestPietyLogic.IsInvocationUnlocked(stats, covenant, invocation);

        public static string BuildLockedReason(
            CharacterStats stats,
            HumanPriestCovenantRuntime covenant,
            PriestInvocationDefinition invocation) =>
            PriestPietyLogic.BuildLockedReason(stats, covenant, invocation);

        public static void ApplyBandPassives(GameObject actor, HumanPriestCovenantRuntime covenant) =>
            PriestPietyLogic.ApplyBandPassives(actor, covenant);

        public static void ResetCacheForTests() => PriestPietyLogic.ResetCacheForTests();
    }
}
