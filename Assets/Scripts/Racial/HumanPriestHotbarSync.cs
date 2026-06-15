using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using UnityEngine;

namespace JRogue.Racial
{
    public static class HumanPriestHotbarSync
    {
        public static void TryAutoPlaceEquipped(GameObject actor)
        {
            BaseActor baseActor = actor?.GetComponent<BaseActor>();
            if (baseActor != null)
                TryAssignEquippedToEmptyMainSlots(baseActor);
        }

        public static void TryAssignEquippedToEmptyMainSlots(BaseActor actor)
        {
            if (actor == null)
                return;

            HumanPriestDevotionRuntime runtime = actor.GetComponent<HumanPriestDevotionRuntime>();
            if (runtime == null)
                return;

            IReadOnlyList<PriestInvocationDefinition> equipped = runtime.EquippedInvocations;
            if (equipped == null || equipped.Count == 0)
                return;

            HotbarLayout layout = HotbarLayout.EnsureOn(actor);
            var assignedIndices = new HashSet<int>();

            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                HotbarEntry entry = layout.GetSlot(slot);
                if (entry.Kind == HotbarEntryKind.HumanPriestInvocation)
                    assignedIndices.Add(entry.abilityIndex);
            }

            int nextEmptySlot = 0;
            for (int invocationIndex = 0; invocationIndex < equipped.Count; invocationIndex++)
            {
                if (assignedIndices.Contains(invocationIndex))
                    continue;

                PriestInvocationDefinition invocation = equipped[invocationIndex];
                if (invocation?.ability == null)
                    continue;

                while (nextEmptySlot < HotbarLayout.HotbarMainSlotCount
                       && !layout.GetSlot(nextEmptySlot).IsEmpty())
                {
                    nextEmptySlot++;
                }

                if (nextEmptySlot >= HotbarLayout.HotbarMainSlotCount)
                    break;

                layout.SetSlot(nextEmptySlot, new HotbarEntry
                {
                    Kind = HotbarEntryKind.HumanPriestInvocation,
                    abilityIndex = invocationIndex,
                    abilityAssetName = invocation.ability.name,
                });
                assignedIndices.Add(invocationIndex);
                nextEmptySlot++;
            }
        }
    }
}
