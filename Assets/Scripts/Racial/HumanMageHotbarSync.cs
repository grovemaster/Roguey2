using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Places newly equipped Human Mage spells onto empty main hotbar slots (v0 until K-menu loadout UI).
    /// </summary>
    public static class HumanMageHotbarSync
    {
        public static void TryAssignEquippedSpellsToEmptyMainSlots(BaseActor actor)
        {
            if (actor == null)
                return;

            HumanMageSpellsRuntime runtime = actor.GetComponent<HumanMageSpellsRuntime>();
            if (runtime == null)
                return;

            IReadOnlyList<MageSpellDefinition> equipped = runtime.EquippedSpells;
            if (equipped == null || equipped.Count == 0)
                return;

            HotbarLayout layout = HotbarLayout.EnsureOn(actor);
            var assignedIndices = new HashSet<int>();

            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                HotbarEntry entry = layout.GetSlot(slot);
                if (entry.Kind == HotbarEntryKind.HumanMageSpell)
                    assignedIndices.Add(entry.abilityIndex);
            }

            int nextEmptySlot = 0;
            for (int spellIndex = 0; spellIndex < equipped.Count; spellIndex++)
            {
                if (assignedIndices.Contains(spellIndex))
                    continue;

                MageSpellDefinition spell = equipped[spellIndex];
                if (spell?.ability == null)
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
                    Kind = HotbarEntryKind.HumanMageSpell,
                    abilityIndex = spellIndex,
                    abilityAssetName = spell.ability.name,
                });
                assignedIndices.Add(spellIndex);
                nextEmptySlot++;
            }
        }
    }
}
