using JRogue.Ability;
using JRogue.Actors;
using JRogue.Combat;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.UI.Inventory;
using UnityEngine;

namespace JRogue.Manager.Inventory
{
    /// <summary>Consumes / activates items from a party member's carried inventory.</summary>
    public static class InventoryItemUse
    {
        public static InventoryUseResult TryUseCarriedItem(InventoryViewModel.Row row, bool inCombat)
        {
            if (row.Owner == null || row.Item == null)
                return InventoryUseResult.Fail("Invalid item or owner.");

            if (row.Instance != null && row.Instance.StorageLocation == ItemStorageLocation.OnGround)
                return InventoryUseResult.Fail("Pick up the item before using it.");

            if (row.Item.IsBowAmmo)
                return TryUseBowArrow(row, inCombat);

            if (row.Item is EvocableItemData evocableDef)
                return TryUseEvocable(row, inCombat, evocableDef);

            if (HealingPotionRules.IsHealingPotionItem(row.Item)
                && inCombat
                && !HealingPotionRules.IsExemptFromPainStun(row.Owner.gameObject))
                return InventoryUseResult.Fail(HealingPotionRules.CombatBanMessage);

            if (!InventoryUsability.AppearsUsableNow(row, inCombat))
            {
                if (!InventoryConsumePolicy.CanConsume(row, out string reason))
                    return InventoryUseResult.Fail(reason);
                return InventoryUseResult.Fail("Cannot use this item right now.");
            }

            if (!InventoryConsumePolicy.CanConsume(row, out string consumeReason))
                return InventoryUseResult.Fail(consumeReason);

            TurnManager turnManager = TurnManager.Instance;
            if (turnManager == null || turnManager.currentState != GameState.PLAYER_TURN)
                return InventoryUseResult.Fail("Not your turn.");

            PartyManager party = PartyManager.Instance;
            BaseActor activeMember = party != null ? party.GetActiveMember() : null;
            if (activeMember == null || !turnManager.CanActorTakeAction(activeMember.gameObject))
                return InventoryUseResult.Fail("Already acted this turn.");

            if (row.Item.activeAbilities == null || row.Item.activeAbilities.Count == 0)
                return InventoryUseResult.Fail("Item has no active ability.");

            AbilityAction ability = row.Item.activeAbilities[0];
            if (ability == null)
                return InventoryUseResult.Fail("Item ability is missing.");

            if (!ability.CanExecute(row.Owner.gameObject))
                return InventoryUseResult.Fail("Cannot use this item right now.");

            string logTag = row.Item.inventoryTargetedUseLogTag;

            if (ability.requiresTarget)
            {
                var pending = new InventoryTargetedUsePending(
                    ability,
                    row.Instance,
                    row.Owner,
                    resumeSelectionIndex: 0,
                    logTag);
                return InventoryUseResult.StartTargeting(pending);
            }

            if (!ability.Execute(row.Owner.gameObject))
                return InventoryUseResult.Fail("Item use failed.");

            InventoryManager inventory = row.Owner.GetComponent<InventoryManager>();
            if (inventory != null && row.Instance != null)
                inventory.TryConsumeCarriedQuantity(row.Instance, 1);

            PartyPlayerActionCompletion.CompleteActiveMemberAction(activeMember);
            return InventoryUseResult.Consumed();
        }

        static InventoryUseResult TryUseEvocable(InventoryViewModel.Row row, bool inCombat, EvocableItemData definition)
        {
            if (row.Instance == null)
                return InventoryUseResult.Fail("Invalid evocable instance.");

            EvocableChargeRules.ClampCharges(row.Instance);

            if (!EvocableChargeRules.HasChargeToInvoke(row.Instance))
            {
                Debug.Log($"{EvocableChargeRules.LogPrefix} Invoke blocked — no charges id={row.Instance.Id}.");
                return InventoryUseResult.Fail("No charges remaining.");
            }

            AbilityAction ability = definition.invokeAbility;
            if (ability == null)
                return InventoryUseResult.Fail("Evocable has no invoke ability.");

            if (!InventoryUsability.AppearsUsableNow(row, inCombat))
                return InventoryUseResult.Fail("Cannot use this item right now.");

            if (!InventoryConsumePolicy.CanConsume(row, out string consumeReason))
                return InventoryUseResult.Fail(consumeReason);

            TurnManager turnManager = TurnManager.Instance;
            if (turnManager == null || turnManager.currentState != GameState.PLAYER_TURN)
                return InventoryUseResult.Fail("Not your turn.");

            PartyManager party = PartyManager.Instance;
            BaseActor activeMember = party != null ? party.GetActiveMember() : null;
            if (activeMember == null || !turnManager.CanActorTakeAction(activeMember.gameObject))
                return InventoryUseResult.Fail("Already acted this turn.");

            if (!ability.CanExecute(row.Owner.gameObject))
                return InventoryUseResult.Fail("Cannot use this item right now.");

            string logTag = string.IsNullOrEmpty(definition.inventoryTargetedUseLogTag)
                ? EvocableChargeRules.LogPrefix
                : definition.inventoryTargetedUseLogTag;

            int chargesBefore = row.Instance.CurrentCharges;
            Debug.Log(
                $"{EvocableChargeRules.LogPrefix} Invoke start {definition.itemName} id={row.Instance.Id} " +
                $"charges={chargesBefore}/{row.Instance.MaxCharges} ability={ability.abilityName}.");

            if (ability.requiresTarget)
            {
                var pending = new InventoryTargetedUsePending(
                    ability,
                    row.Instance,
                    row.Owner,
                    resumeSelectionIndex: 0,
                    logTag);
                return InventoryUseResult.StartTargeting(pending);
            }

            if (!ability.Execute(row.Owner.gameObject))
                return InventoryUseResult.Fail("Item use failed.");

            InventoryManager inventory = row.Owner.GetComponent<InventoryManager>();
            if (inventory != null)
                EvocableChargeRules.SpendChargeAfterSuccessfulInvoke(inventory, row.Instance);

            PartyPlayerActionCompletion.CompleteActiveMemberAction(activeMember);
            return InventoryUseResult.Consumed();
        }

        static InventoryUseResult TryUseBowArrow(InventoryViewModel.Row row, bool inCombat)
        {
            if (row.Item is { isThrowable: false, requiresBow: true })
            {
                if (!BowRangedCombatService.HasBowEquipped(row.Owner))
                {
                    BowRangedCombatService.LogArrowsRequireBow();
                    return InventoryUseResult.Fail("Arrows require a bow.");
                }
            }

            if (!InventoryUsability.AppearsUsableNow(row, inCombat))
                return InventoryUseResult.Fail("Cannot use this item right now.");

            TurnManager turnManager = TurnManager.Instance;
            if (turnManager == null || turnManager.currentState != GameState.PLAYER_TURN)
                return InventoryUseResult.Fail("Not your turn.");

            PartyManager party = PartyManager.Instance;
            BaseActor activeMember = party != null ? party.GetActiveMember() : null;
            if (activeMember == null || row.Owner == null)
                return InventoryUseResult.Fail("Invalid owner.");

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
                return InventoryUseResult.Fail("Already acted this turn.");

            if (!BowRangedCombatService.HasBowEquipped(row.Owner))
            {
                BowRangedCombatService.LogArrowsRequireBow();
                return InventoryUseResult.Fail("Arrows require a bow.");
            }

            if (row.Instance == null || row.CarriedListIndex < 0)
                return InventoryUseResult.Fail("Arrow must be carried.");

            EquipmentManager equip = row.Owner.GetComponent<EquipmentManager>();
            ItemInstance restoreOffHand = null;
            if (equip != null)
            {
                ItemInstance currentOff = equip.GetEquippedInstance(EquipmentSlot.OffHand);
                if (currentOff != null && currentOff.Id != row.Instance.Id)
                    restoreOffHand = currentOff;

                equip.EquipItem(EquipmentSlot.OffHand, row.Instance);
            }

            if (!BowRangedCombatService.HasAnyArrowAvailable(row.Owner))
            {
                BowRangedCombatService.LogArrowsRequireBow();
                return InventoryUseResult.Fail("No arrows available.");
            }

            var bowPending = new InventoryBowAimPending(
                row.Owner,
                row.Instance,
                restoreOffHand,
                resumeSelectionIndex: 0);
            return InventoryUseResult.StartBowAim(bowPending);
        }
    }
}
