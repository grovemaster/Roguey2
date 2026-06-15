using System.Collections.Generic;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    public static class HumanPriestCovenantService
    {
        public static HumanPriestCovenantRuntime EnsureCovenantRuntime(GameObject actor)
        {
            if (actor == null)
                return null;

            HumanPriestCovenantRuntime runtime = actor.GetComponent<HumanPriestCovenantRuntime>();
            if (runtime == null)
                runtime = actor.AddComponent<HumanPriestCovenantRuntime>();

            return runtime;
        }

        public static HumanPriestDevotionRuntime EnsureDevotionRuntime(GameObject actor)
        {
            if (actor == null)
                return null;

            HumanPriestDevotionRuntime runtime = actor.GetComponent<HumanPriestDevotionRuntime>();
            if (runtime == null)
                runtime = actor.AddComponent<HumanPriestDevotionRuntime>();

            return runtime;
        }

        public static bool InitializeOnCommit(GameObject actor, string patronGodId, out string failureReason)
        {
            failureReason = null;
            if (actor == null)
            {
                failureReason = "Actor is null.";
                return false;
            }

            if (!PatronGodCatalogService.TryGetGod(patronGodId, out PatronGodDefinition god))
            {
                failureReason = $"Unknown patron god '{patronGodId}'.";
                return false;
            }

            HumanPriestCovenantRuntime covenant = EnsureCovenantRuntime(actor);
            HumanPriestDevotionRuntime devotion = EnsureDevotionRuntime(actor);
            if (covenant == null || devotion == null)
            {
                failureReason = "Failed to attach priest runtimes.";
                return false;
            }

            int startingPiety = HumanPriestPietyService.ResolveStartingPiety();
            covenant.InitializeOnCommit(god.godId, startingPiety);
            HumanPriestPietyService.ApplyBandPassives(actor, covenant);

            devotion.SetEquippedIds(new List<string>
            {
                "priest_lay_on_hands",
                "priest_smites_undead",
            });

            HumanPriestHotbarSync.TryAutoPlaceEquipped(actor);
            return true;
        }
    }
}
