using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using JRogue.UI.Inventory;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Racial
{
    public static class MageSpellbookReadService
    {
        public const string MageOnlyMessage = "Only a Mage can study this spellbook.";
        public const string AllKnownMessage = "You already know every spell in this book.";

        public static bool CanRead(InventoryViewModel.Row row)
        {
            if (row.Owner == null || row.Item == null || row.IsEquipped)
                return false;

            if (row.Item is not SpellbookItemData spellbookItem || spellbookItem.spellbook == null)
                return false;

            return ValidateReader(row.Owner, spellbookItem.spellbook, out _);
        }

        public static bool ValidateReader(
            BaseActor owner,
            MageSpellbookDefinition book,
            out string denyReason)
        {
            denyReason = null;
            if (owner == null || book == null)
            {
                denyReason = "Invalid spellbook.";
                return false;
            }

            CharacterStats stats = owner.GetComponent<CharacterStats>();
            HumanMageSpellsRuntime runtime = HumanClassCommitment.ResolveMageSpellsRuntime(owner.gameObject);
            if (stats == null
                || stats.race != Race.Human
                || stats.humanClass != HumanClass.Mage
                || runtime == null)
            {
                denyReason = MageOnlyMessage;
                return false;
            }

            if (!HasUnknownSpell(runtime, book))
            {
                denyReason = AllKnownMessage;
                return false;
            }

            return true;
        }

        public static InventoryUseResult TryRead(InventoryViewModel.Row row)
        {
            if (row.Owner == null || row.Item == null || row.Instance == null)
                return InventoryUseResult.Fail("Invalid spellbook.");

            if (row.Item is not SpellbookItemData spellbookItem || spellbookItem.spellbook == null)
                return InventoryUseResult.Fail("Invalid spellbook.");

            MageSpellbookDefinition book = spellbookItem.spellbook;
            HumanMageSpellsRuntime runtime = HumanClassCommitment.ResolveMageSpellsRuntime(row.Owner.gameObject);
            if (!ValidateReader(row.Owner, book, out string denyReason))
                return InventoryUseResult.Fail(denyReason);

            bool canPrepareLoadout = SafeZonePolicyService.TryAllowHumanMageEquipChange(out _);

            var learnedNames = new List<string>();
            if (book.spellIds != null)
            {
                for (int i = 0; i < book.spellIds.Count; i++)
                {
                    string spellId = book.spellIds[i];
                    if (string.IsNullOrWhiteSpace(spellId))
                        continue;

                    string trimmed = spellId.Trim();
                    if (runtime.HasLearned(trimmed))
                        continue;

                    if (!runtime.TryLearnSpell(trimmed, out string learnReason))
                    {
                        Debug.LogWarning($"[MageSpellbook] Failed to learn '{spellId}': {learnReason}");
                        continue;
                    }

                    if (canPrepareLoadout)
                        runtime.TryEquip(trimmed, out _);

                    if (MageSpellCatalogService.TryGetSpell(trimmed, out MageSpellDefinition spell)
                        && spell != null
                        && !string.IsNullOrWhiteSpace(spell.displayName))
                    {
                        learnedNames.Add(spell.displayName);
                    }
                    else
                    {
                        learnedNames.Add(trimmed);
                    }
                }
            }

            if (learnedNames.Count == 0)
                return InventoryUseResult.Fail(AllKnownMessage);

            InventoryManager inventory = row.Owner.GetComponent<InventoryManager>();
            if (inventory == null || !inventory.TryConsumeCarriedQuantity(row.Instance, 1))
                return InventoryUseResult.Fail("Could not consume spellbook.");

            if (canPrepareLoadout)
            {
                HumanMageHotbarSync.TryAssignEquippedSpellsToEmptyMainSlots(row.Owner);
                AbilityHotbarUI.EnsureInstance().RefreshAll();
            }

            Debug.Log(BuildLearnFeedback(learnedNames));
            return InventoryUseResult.Consumed();
        }

        static bool HasUnknownSpell(HumanMageSpellsRuntime runtime, MageSpellbookDefinition book)
        {
            if (runtime == null || book?.spellIds == null)
                return false;

            for (int i = 0; i < book.spellIds.Count; i++)
            {
                string spellId = book.spellIds[i];
                if (string.IsNullOrWhiteSpace(spellId))
                    continue;

                if (!runtime.HasLearned(spellId.Trim()))
                    return true;
            }

            return false;
        }

        static string BuildLearnFeedback(IReadOnlyList<string> learnedNames)
        {
            var sb = new StringBuilder("You learn ");
            for (int i = 0; i < learnedNames.Count; i++)
            {
                if (i > 0)
                    sb.Append(i == learnedNames.Count - 1 ? ", and " : ", ");
                sb.Append(learnedNames[i]);
            }

            sb.Append('.');
            return sb.ToString();
        }
    }
}
