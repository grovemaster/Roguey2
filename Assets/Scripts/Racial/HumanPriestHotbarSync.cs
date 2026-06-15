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

        public static bool TryAssignEquippedInvocationToHotbar(
            BaseActor actor,
            string invocationId,
            out string failureReason)
        {
            failureReason = null;
            if (actor == null)
            {
                failureReason = "No actor.";
                return false;
            }

            HumanPriestDevotionRuntime runtime = actor.GetComponent<HumanPriestDevotionRuntime>();
            if (runtime == null)
            {
                failureReason = "No priest devotion runtime.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(invocationId))
            {
                failureReason = "Invocation id is empty.";
                return false;
            }

            string trimmed = invocationId.Trim();
            int equippedIndex = -1;
            IReadOnlyList<PriestInvocationDefinition> equipped = runtime.EquippedInvocations;
            for (int i = 0; i < equipped.Count; i++)
            {
                if (equipped[i] != null
                    && string.Equals(equipped[i].invocationId, trimmed, System.StringComparison.OrdinalIgnoreCase))
                {
                    equippedIndex = i;
                    break;
                }
            }

            if (equippedIndex < 0)
            {
                failureReason = "Invocation is not prepared.";
                return false;
            }

            HotbarLayout layout = HotbarLayout.EnsureOn(actor);
            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                HotbarEntry entry = layout.GetSlot(slot);
                if (entry.Kind == HotbarEntryKind.HumanPriestInvocation
                    && entry.abilityIndex == equippedIndex)
                {
                    failureReason = "Already on hotbar.";
                    return false;
                }
            }

            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                if (!layout.GetSlot(slot).IsEmpty())
                    continue;

                PriestInvocationDefinition invocation = equipped[equippedIndex];
                layout.SetSlot(slot, new HotbarEntry
                {
                    Kind = HotbarEntryKind.HumanPriestInvocation,
                    abilityIndex = equippedIndex,
                    abilityAssetName = invocation?.ability != null ? invocation.ability.name : string.Empty,
                });
                return true;
            }

            failureReason = "Hotbar full.";
            return false;
        }
    }
}
