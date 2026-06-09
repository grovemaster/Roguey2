using JRogue.Ability;
using JRogue.Actors;
using JRogue.Combat;
using JRogue.Input;
using JRogue.Item;
using JRogue.Manager.Combat;
using JRogue.Manager.Equipment;
using JRogue.Manager.Essence;
using JRogue.Manager.Inventory;
using JRogue.Manager.Turn;
using JRogue.World.Generation;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Inventory;
using UnityEngine;

namespace JRogue.UI.Hotbar
{
    public static class HotbarUsabilityService
    {
        public static (bool usable, bool stale, string denyReason) Evaluate(
            BaseActor actor,
            HotbarResolvedAction resolved)
        {
            if (actor == null)
                return (false, false, "No actor.");

            if (resolved.IsStale)
                return (false, true, resolved.DenyReason ?? "Stale hotbar entry.");

            if (!resolved.IsValid)
                return (false, false, resolved.DenyReason ?? "Invalid hotbar entry.");

            TurnManager turnManager = TurnManager.Instance;
            bool inSafeZone = SafeZonePolicyService.IsSafeZoneForActiveParty();

            if (resolved.Kind == HotbarEntryKind.ElementalSpiritSummon)
                return EvaluateElementalSpiritSummon(actor, resolved, turnManager, inSafeZone);

            if (resolved.Source == PlayerAbilitySource.RacialActive
                && HotbarResolver.IsElementalSpiritActiveBinding(resolved.RacialBindingKey))
            {
                return EvaluateElementalSpiritActive(actor, resolved, turnManager, inSafeZone);
            }

            if (turnManager == null || turnManager.currentState != GameState.PLAYER_TURN)
                return (false, false, "Not your turn.");

            if (resolved.Kind == HotbarEntryKind.InventoryUse
                || resolved.Kind == HotbarEntryKind.InventoryActive)
            {
                return EvaluateInventory(actor, resolved);
            }

            AbilityAction ability = resolved.Ability;
            if (ability == null)
                return (false, resolved.IsStale, "Ability unavailable.");

            if (RequiresTurnSlotBeforeUse(actor, resolved)
                && !turnManager.CanActorTakeAction(actor.gameObject))
            {
                return (false, false, "Already acted this turn.");
            }

            if (!ability.IsReadyForUse(actor.gameObject))
                return (false, false, "Cannot use this ability right now.");

            if (!TryAllowSource(resolved.Source, logDeny: false, out string safeDeny))
                return (false, false, safeDeny);

            if (!CanAfford(actor, resolved, ability))
                return (false, false, InsufficientResourceMessage(actor));

            if (resolved.Source == PlayerAbilitySource.RacialActive
                && !CanExecuteRacial(actor, resolved, ability))
            {
                return (false, false, "Racial ability unavailable.");
            }

            return (true, false, null);
        }

        static (bool usable, bool stale, string denyReason) EvaluateElementalSpiritActive(
            BaseActor actor,
            HotbarResolvedAction resolved,
            TurnManager turnManager,
            bool inSafeZone)
        {
            AbilityAction ability = resolved.Ability;
            if (ability == null)
                return (false, true, "Ability unavailable.");

            ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (contracts == null)
                return (false, true, "No elemental spirit runtime.");

            if (!HotbarResolver.TryParseElementalSpiritActiveBinding(
                    resolved.RacialBindingKey,
                    out string abilityAssetName)
                || !HotbarResolver.TryFindRosterSpiritActiveByAssetName(contracts, abilityAssetName, out _))
            {
                return (false, true, "Elemental spirit ability unavailable.");
            }

            if (!HotbarResolver.HasSummonedSpiritActiveByAssetName(contracts, abilityAssetName))
                return (false, false, "Summon the spirit to use this ability.");

            if (turnManager == null)
                return (false, false, "Not your turn.");

            if (RequiresTurnSlotBeforeUse(actor, resolved))
            {
                if (turnManager.currentState != GameState.PLAYER_TURN)
                    return (false, false, "Not your turn.");

                if (!turnManager.CanActorTakeAction(actor.gameObject))
                    return (false, false, "Already acted this turn.");
            }

            if (!CanAfford(actor, resolved, ability))
                return (false, false, InsufficientResourceMessage(actor));

            if (!ability.IsReadyForUse(actor.gameObject))
                return (false, false, "Cannot use this ability right now.");

            return (true, false, null);
        }

        static (bool usable, bool stale, string denyReason) EvaluateElementalSpiritSummon(
            BaseActor actor,
            HotbarResolvedAction resolved,
            TurnManager turnManager,
            bool inSafeZone)
        {
            if (turnManager == null)
                return (false, false, "Not your turn.");

            if (!inSafeZone && turnManager.currentState != GameState.PLAYER_TURN)
                return (false, false, "Not your turn.");

            ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (contracts == null)
                return (false, true, "No elemental spirit runtime.");

            string instanceId = resolved.ContractInstanceId;
            if (string.IsNullOrEmpty(instanceId)
                || !contracts.TryGetPreset(instanceId, out ElementalSpiritContractPreset preset)
                || preset.spirit == null)
            {
                return (false, true, "Spirit instance is not contracted.");
            }

            if (contracts.IsInstanceSummoned(instanceId))
                return (true, false, null);

            CharacterStats stats = actor.stats;
            if (stats == null)
                return (false, false, "No character stats.");

            if (stats.currentSoulPower < preset.spirit.summonSoulPowerCost)
                return (false, false, "Not enough Soul Power to summon.");

            return (true, false, null);
        }

        static (bool usable, bool stale, string denyReason) EvaluateInventory(
            BaseActor actor,
            HotbarResolvedAction resolved)
        {
            if (resolved.ItemInstance == null || resolved.ItemOwner == null)
                return (false, true, "Item not found.");

            if (!string.IsNullOrEmpty(resolved.DenyReason))
                return (false, false, resolved.DenyReason);

            ItemData definition = resolved.ItemInstance.Definition;
            if (definition == null)
                return (false, true, "Item definition missing.");

            if (!SafeZonePolicyService.TryAllowInventoryUse(definition, out string safeDeny, logDeny: false))
                return (false, false, safeDeny);

            InventoryViewModel.Row row = BuildInventoryRow(resolved);
            bool inCombat = CombatThreatCoordinator.Instance != null && CombatThreatCoordinator.Instance.IsInCombat;

            if (!InventoryUsability.AppearsUsableNow(row, inCombat))
                return (false, false, "Cannot use this item right now.");

            if (!InventoryConsumePolicy.CanConsume(row, out string consumeDeny))
                return (false, false, consumeDeny);

            if (resolved.Kind == HotbarEntryKind.InventoryActive
                && resolved.Ability != null
                && !resolved.Ability.CanExecute(resolved.ItemOwner.gameObject))
            {
                return (false, false, "Cannot use this item right now.");
            }

            return (true, false, null);
        }

        static InventoryViewModel.Row BuildInventoryRow(HotbarResolvedAction resolved)
        {
            BaseActor owner = resolved.ItemOwner;
            ItemInstance instance = resolved.ItemInstance;

            EquipmentManager equipment = owner?.GetComponent<EquipmentManager>();
            bool isEquipped = false;
            EquipmentSlot? equippedSlot = null;
            int carriedIndex = -1;

            if (equipment != null && equipment.TryGetEquippedSlot(instance, out EquipmentSlot slot))
            {
                isEquipped = true;
                equippedSlot = slot;
            }
            else
            {
                InventoryManager inventory = owner?.GetComponent<InventoryManager>();
                if (inventory != null)
                {
                    for (int i = 0; i < inventory.CarriedItems.Count; i++)
                    {
                        if (inventory.CarriedItems[i]?.Id == instance.Id)
                        {
                            carriedIndex = i;
                            break;
                        }
                    }
                }
            }

            return new InventoryViewModel.Row(
                letter: ' ',
                instance: instance,
                owner: owner,
                ownerDisplayName: owner != null ? owner.name : string.Empty,
                isEquipped: isEquipped,
                equippedSlot: equippedSlot,
                carriedListIndex: carriedIndex,
                stackedWeight: instance?.TotalWeight ?? 0f);
        }

        static bool TryAllowSource(PlayerAbilitySource source, bool logDeny, out string denyReason)
        {
            denyReason = null;
            if (source == PlayerAbilitySource.InventoryItem)
                return true;

            if (source == PlayerAbilitySource.Essence)
                return SafeZonePolicyService.TryAllowEssenceAbility(out denyReason, logDeny);

            return SafeZonePolicyService.TryAllowHostileAction(out denyReason, logDeny);
        }

        static bool CanAfford(BaseActor actor, HotbarResolvedAction resolved, AbilityAction ability)
        {
            CharacterStats stats = actor.stats;
            if (stats == null)
                return false;

            if (resolved.Source == PlayerAbilitySource.HumanMageSpell)
            {
                HumanMageSpellsRuntime mageSpells = actor.GetComponent<HumanMageSpellsRuntime>();
                return mageSpells != null && mageSpells.CanAffordCast(resolved.AbilityIndex);
            }

            if (resolved.Source == PlayerAbilitySource.Essence)
            {
                EssenceSlotManager essence = actor.GetComponent<EssenceSlotManager>();
                return essence != null && essence.CanAfford(resolved.SlotIndex, resolved.AbilityIndex);
            }

            return HumanClassAbilityResources.CanAfford(stats, ability);
        }

        static bool CanExecuteRacial(BaseActor actor, HotbarResolvedAction resolved, AbilityAction ability)
        {
            if (HotbarResolver.IsElementalSpiritActiveBinding(resolved.RacialBindingKey))
            {
                ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
                return contracts != null && contracts.CanExecuteSpiritActiveForAbility(ability);
            }

            return ability.CanExecute(actor.gameObject);
        }

        public static bool RequiresTurnSlotBeforeUse(BaseActor actor, HotbarResolvedAction resolved)
        {
            if (resolved.Kind == HotbarEntryKind.ElementalSpiritSummon)
                return false;

            if (resolved.Source == PlayerAbilitySource.RacialActive
                && HotbarResolver.IsElementalSpiritActiveBinding(resolved.RacialBindingKey))
            {
                ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
                if (contracts == null || resolved.Ability == null)
                    return true;

                return contracts.SpiritActiveConsumesTurn(resolved.Ability);
            }

            return true;
        }

        public static bool RequiresTurnSlotAfterUse(BaseActor actor, HotbarResolvedAction resolved)
        {
            if (resolved.Kind == HotbarEntryKind.ElementalSpiritSummon)
                return false;

            if (resolved.Source == PlayerAbilitySource.RacialActive
                && HotbarResolver.IsElementalSpiritActiveBinding(resolved.RacialBindingKey))
            {
                ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
                if (contracts == null || resolved.Ability == null)
                    return true;

                return contracts.SpiritActiveConsumesTurn(resolved.Ability);
            }

            return true;
        }

        static string InsufficientResourceMessage(BaseActor actor)
        {
            CharacterStats stats = actor?.stats;
            if (stats == null)
                return "Insufficient resources.";

            return HumanClassAbilityResources.InsufficientResourceMessage(stats.humanClass);
        }
    }
}
