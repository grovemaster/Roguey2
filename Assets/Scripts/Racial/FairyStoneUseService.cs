using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Gameplay;
using JRogue.UI.Inventory;
using UnityEngine;

namespace JRogue.Racial
{
    public static class FairyStonePartyRules
    {
        public static bool PartyHasElf()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return false;

            foreach (BaseActor member in party.partyMembers)
            {
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                CharacterStats stats = member.stats;
                if (stats != null
                    && stats.race == Race.Elf
                    && stats.racialSubsystem == RacialSubsystemKind.ElfElementalContracts)
                {
                    return true;
                }
            }

            return false;
        }

        public static List<BaseActor> GetEligibleElves()
        {
            var elves = new List<BaseActor>();
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return elves;

            foreach (BaseActor member in party.partyMembers)
            {
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                CharacterStats stats = member.stats;
                if (stats == null
                    || stats.race != Race.Elf
                    || stats.racialSubsystem != RacialSubsystemKind.ElfElementalContracts)
                {
                    continue;
                }

                if (member.GetComponent<ElementalSpiritContractsRuntime>() == null)
                    continue;

                elves.Add(member);
            }

            return elves;
        }
    }

    public static class FairyStoneUseService
    {
        const string CancelPayload = "__cancel__";
        const float SuccessChance = 0.5f;

        static ElementalSpiritRegistry _registry;
        static Action _inventoryRefreshCallback;

        public static void SetInventoryRefreshCallback(Action callback) =>
            _inventoryRefreshCallback = callback;

        static void NotifyInventoryChanged()
        {
            Action callback = _inventoryRefreshCallback;
            _inventoryRefreshCallback = null;
            callback?.Invoke();
        }

        static void ClearInventoryRefreshCallback() => _inventoryRefreshCallback = null;

        public static InventoryUseResult TryBeginUse(InventoryViewModel.Row row)
        {
            if (row.Owner == null || row.Instance == null || row.Item == null)
                return InventoryUseResult.Fail("Invalid item or owner.");

            if (row.Item is not FairyStoneItemData)
                return InventoryUseResult.Fail("Not a Fairy Stone.");

            if (!FairyStonePartyRules.PartyHasElf())
                return InventoryUseResult.Fail("Requires an Elf in the party.");

            List<BaseActor> elves = FairyStonePartyRules.GetEligibleElves();
            if (elves.Count == 0)
                return InventoryUseResult.Fail("No Elf can form elemental contracts.");

            ShowElfPicker(row, elves);
            return InventoryUseResult.StartChoiceDialog();
        }

        static void ShowElfPicker(InventoryViewModel.Row row, List<BaseActor> elves)
        {
            var options = new List<DialogChoiceOptionData>(elves.Count + 1);
            for (int i = 0; i < elves.Count; i++)
            {
                BaseActor elf = elves[i];
                options.Add(new DialogChoiceOptionData
                {
                    label = elf.DisplayName,
                    payload = i.ToString(),
                    enabled = true,
                });
            }

            options.Add(new DialogChoiceOptionData
            {
                label = "Cancel",
                payload = CancelPayload,
                enabled = true,
            });

            var step = new DialogChoiceStep
            {
                SpeakerName = "Fairy Stone",
                PromptText = "Use Fairy Stone on which Elf?",
                Portrait = null,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, option => OnElfSelected(row, elves, option));
        }

        static void OnElfSelected(
            InventoryViewModel.Row row,
            List<BaseActor> elves,
            DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null
                || option.payload == CancelPayload
                || string.IsNullOrWhiteSpace(option.payload))
            {
                ClearInventoryRefreshCallback();
                return;
            }

            if (!int.TryParse(option.payload, out int elfIndex)
                || elfIndex < 0
                || elfIndex >= elves.Count)
            {
                ClearInventoryRefreshCallback();
                Debug.Log("[FairyStone] Invalid Elf selection.");
                return;
            }

            BaseActor target = elves[elfIndex];

            if (!TryConsumeStone(row))
            {
                ClearInventoryRefreshCallback();
                ShowFeedback("The Fairy Stone could not be consumed.");
                return;
            }

            NotifyInventoryChanged();
            AttemptContract(target);
        }

        static bool TryConsumeStone(InventoryViewModel.Row row)
        {
            InventoryManager inventory = row.Owner.GetComponent<InventoryManager>();
            if (inventory == null || row.Instance == null)
                return false;

            return inventory.TryConsumeCarriedQuantity(row.Instance, 1);
        }

        static void AttemptContract(BaseActor elf)
        {
            ElementalSpiritRegistry registry = LoadRegistry();
            if (registry == null || registry.Spirits == null || registry.Spirits.Count == 0)
            {
                ShowFeedback("The stone has nothing left to offer.");
                return;
            }

            if (UnityEngine.Random.value >= SuccessChance)
            {
                ShowFeedback("The stone crumbles to dust. No spirit answers.");
                return;
            }

            if (!registry.TryPickRandom(out ElementalSpiritDefinition spirit) || spirit == null)
            {
                ShowFeedback("The stone has nothing left to offer.");
                return;
            }

            if (!ElementalSpiritContractService.TryFormContract(
                    elf,
                    spirit,
                    initialLevel: 1,
                    out string instanceId,
                    out string failureReason))
            {
                ShowFeedback(string.IsNullOrEmpty(failureReason)
                    ? "No spirit answers."
                    : failureReason);
                return;
            }

            string spiritName = string.IsNullOrWhiteSpace(spirit.displayName)
                ? spirit.spiritId
                : spirit.displayName.Trim();
            ShowFeedback($"{elf.DisplayName} forms a contract with {spiritName}!");
            Debug.Log($"[FairyStone] {elf.name} contracted {spirit.spiritId} instance {instanceId}.");
        }

        static void ShowFeedback(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            Debug.Log($"[FairyStone] {line}");
            var feedback = new DialogLineStep
            {
                SpeakerName = "Fairy Stone",
                ResolvedText = line,
                Portrait = null,
            };
            NpcDialogBoxUI.EnsureInstance().ShowLine(feedback, () => NpcDialogBoxUI.EnsureInstance().Close());
        }

        static ElementalSpiritRegistry LoadRegistry()
        {
            if (_registry != null)
                return _registry;

            _registry = Resources.Load<ElementalSpiritRegistry>("Racial/Elf/ElementalSpiritRegistry");
            return _registry;
        }

#if UNITY_EDITOR
        public static void SetRegistryForTests(ElementalSpiritRegistry registry) => _registry = registry;

        public static void ResetRegistryForTests()
        {
            _registry = null;
            ClearInventoryRefreshCallback();
        }
#endif
    }
}
