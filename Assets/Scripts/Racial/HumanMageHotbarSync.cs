using System;
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

        public static bool TryAssignEquippedSpellToHotbar(
            BaseActor actor,
            string spellId,
            out string failureReason)
        {
            failureReason = null;
            if (actor == null)
            {
                failureReason = "No actor.";
                return false;
            }

            HumanMageSpellsRuntime runtime = actor.GetComponent<HumanMageSpellsRuntime>();
            if (runtime == null)
            {
                failureReason = "No Human Mage spell runtime.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(spellId))
            {
                failureReason = "Spell id is empty.";
                return false;
            }

            string trimmed = spellId.Trim();
            if (!TryResolveEquippedIndex(runtime, trimmed, out int equippedIndex, out MageSpellDefinition spell))
            {
                failureReason = "Spell is not prepared.";
                return false;
            }

            if (spell?.ability == null)
            {
                failureReason = "Spell has no ability.";
                return false;
            }

            HotbarLayout layout = HotbarLayout.EnsureOn(actor);
            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                HotbarEntry entry = layout.GetSlot(slot);
                if (entry.Kind == HotbarEntryKind.HumanMageSpell && entry.abilityIndex == equippedIndex)
                {
                    failureReason = "Already on hotbar.";
                    return false;
                }
            }

            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                if (!layout.GetSlot(slot).IsEmpty())
                    continue;

                layout.SetSlot(slot, new HotbarEntry
                {
                    Kind = HotbarEntryKind.HumanMageSpell,
                    abilityIndex = equippedIndex,
                    abilityAssetName = spell.ability.name,
                });
                return true;
            }

            failureReason = "Hotbar full — open ability hotbar to rearrange.";
            return false;
        }

        static bool TryResolveEquippedIndex(
            HumanMageSpellsRuntime runtime,
            string spellId,
            out int equippedIndex,
            out MageSpellDefinition spell)
        {
            equippedIndex = -1;
            spell = null;
            if (runtime?.EquippedSpells == null)
                return false;

            for (int i = 0; i < runtime.EquippedSpells.Count; i++)
            {
                MageSpellDefinition candidate = runtime.EquippedSpells[i];
                if (candidate != null
                    && string.Equals(candidate.spellId, spellId, StringComparison.OrdinalIgnoreCase))
                {
                    equippedIndex = i;
                    spell = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
