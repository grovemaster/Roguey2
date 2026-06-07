using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Input;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Manager.Equipment;
using JRogue.Manager.Essence;
using JRogue.Manager.Inventory;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.UI.Hotbar
{
    public static class HotbarResolver
    {
        public const string SpiritImprintBindingPrefix = "SpiritImprint:";
        public const string ElementalSpiritBindingPrefix = "ElementalSpirit:";

        public static HotbarResolvedAction Resolve(BaseActor actor, HotbarEntry entry)
        {
            if (actor == null || entry == null || entry.IsEmpty())
            {
                return Invalid(HotbarEntryKind.Empty, "Empty hotbar entry.");
            }

            return entry.Kind switch
            {
                HotbarEntryKind.EssenceActive => ResolveEssence(actor, entry),
                HotbarEntryKind.EquipmentActive => ResolveEquipment(actor, entry),
                HotbarEntryKind.HumanMageSpell => ResolveHumanMageSpell(actor, entry),
                HotbarEntryKind.RacialActive => ResolveRacial(actor, entry),
                HotbarEntryKind.InventoryActive => ResolveInventoryActive(actor, entry),
                HotbarEntryKind.InventoryUse => ResolveInventoryUse(actor, entry),
                _ => Invalid(entry.Kind, "Unknown hotbar entry kind."),
            };
        }

        static HotbarResolvedAction ResolveEssence(BaseActor actor, HotbarEntry entry)
        {
            EssenceSlotManager essence = actor.GetComponent<EssenceSlotManager>();
            if (essence == null)
                return Stale(entry.Kind, "No essence manager.");

            EssenceData equipped = essence.GetEssenceInSlot(entry.essenceSlotIndex);
            if (equipped == null
                || equipped.activeAbilities == null
                || entry.abilityIndex < 0
                || entry.abilityIndex >= equipped.activeAbilities.Count)
            {
                return Stale(entry.Kind, "Essence ability unavailable.");
            }

            AbilityAction ability = equipped.activeAbilities[entry.abilityIndex];
            if (ability == null)
                return Stale(entry.Kind, "Essence ability missing.");

            return Valid(
                entry.Kind,
                ability,
                PlayerAbilitySource.Essence,
                entry.essenceSlotIndex,
                entry.abilityIndex);
        }

        static HotbarResolvedAction ResolveEquipment(BaseActor actor, HotbarEntry entry)
        {
            EquipmentManager equipment = actor.GetComponent<EquipmentManager>();
            if (equipment == null)
                return Stale(entry.Kind, "No equipment manager.");

            if (!System.Enum.IsDefined(typeof(EquipmentSlot), entry.equipmentSlot))
                return Stale(entry.Kind, "Invalid equipment slot.");

            var slot = (EquipmentSlot)entry.equipmentSlot;
            ItemInstance equipped = equipment.GetEquippedInstance(slot);
            if (equipped?.Definition?.activeAbilities == null
                || entry.abilityIndex < 0
                || entry.abilityIndex >= equipped.Definition.activeAbilities.Count)
            {
                return Stale(entry.Kind, "Equipment ability unavailable.");
            }

            AbilityAction ability = equipped.Definition.activeAbilities[entry.abilityIndex];
            if (ability == null)
                return Stale(entry.Kind, "Equipment ability missing.");

            return Valid(
                entry.Kind,
                ability,
                PlayerAbilitySource.EquipmentItem,
                EquipmentSlotToLegacyIndex(slot),
                entry.abilityIndex);
        }

        static HotbarResolvedAction ResolveHumanMageSpell(BaseActor actor, HotbarEntry entry)
        {
            CharacterStats stats = actor.stats;
            if (stats == null || stats.humanClass != HumanClass.Mage)
                return Stale(entry.Kind, "Not a mage.");

            HumanMageSpellsRuntime mageSpells = actor.GetComponent<HumanMageSpellsRuntime>();
            if (mageSpells == null)
                return Stale(entry.Kind, "No mage spell runtime.");

            AbilityAction ability = mageSpells.GetEquippedAbility(entry.abilityIndex);
            if (ability == null)
                return Stale(entry.Kind, "Mage spell unavailable.");

            return Valid(
                entry.Kind,
                ability,
                PlayerAbilitySource.HumanMageSpell,
                entry.abilityIndex,
                entry.abilityIndex);
        }

        static HotbarResolvedAction ResolveRacial(BaseActor actor, HotbarEntry entry)
        {
            string bindingKey = entry.racialBindingKey;
            if (string.IsNullOrEmpty(bindingKey))
                return Stale(entry.Kind, "Missing racial binding key.");

            if (bindingKey.StartsWith(SpiritImprintBindingPrefix, System.StringComparison.Ordinal))
                return ResolveSpiritImprint(actor, entry, bindingKey);

            if (bindingKey.StartsWith(ElementalSpiritBindingPrefix, System.StringComparison.Ordinal))
                return ResolveElementalSpirit(actor, entry, bindingKey);

            return Stale(entry.Kind, "Unknown racial binding key.");
        }

        static HotbarResolvedAction ResolveSpiritImprint(BaseActor actor, HotbarEntry entry, string bindingKey)
        {
            string[] parts = bindingKey.Split(':');
            if (parts.Length < 3)
                return Stale(entry.Kind, "Invalid Spirit Imprint binding.");

            string nodeId = parts[1];
            if (!int.TryParse(parts[2], out int abilityIndex))
                return Stale(entry.Kind, "Invalid Spirit Imprint ability index.");

            SpiritImprintRuntime imprint = actor.GetComponent<SpiritImprintRuntime>();
            if (imprint?.Graph == null)
                return Stale(entry.Kind, "No Spirit Imprint runtime.");

            IReadOnlyList<string> path = imprint.ChosenPathNodeIds;
            if (path == null || !ContainsNode(path, nodeId))
                return Stale(entry.Kind, "Spirit Imprint node not on chosen path.");

            if (!imprint.Graph.TryFindNode(nodeId, out SpiritImprintNodeData node)
                || node.activeAbilities == null
                || abilityIndex < 0
                || abilityIndex >= node.activeAbilities.Count)
            {
                return Stale(entry.Kind, "Spirit Imprint ability unavailable.");
            }

            AbilityAction ability = node.activeAbilities[abilityIndex];
            if (ability == null)
                return Stale(entry.Kind, "Spirit Imprint ability missing.");

            return ValidRacial(entry, ability, abilityIndex);
        }

        static HotbarResolvedAction ResolveElementalSpirit(BaseActor actor, HotbarEntry entry, string bindingKey)
        {
            string[] parts = bindingKey.Split(':');
            if (parts.Length < 3)
                return Stale(entry.Kind, "Invalid Elemental Spirit binding.");

            string spiritId = parts[1];
            if (!int.TryParse(parts[2], out int abilityIndex))
                return Stale(entry.Kind, "Invalid Elemental Spirit ability index.");

            ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (contracts == null)
                return Stale(entry.Kind, "No elemental spirit runtime.");

            if (!contracts.IsSpiritSummoned(spiritId))
                return Stale(entry.Kind, "Spirit is not summoned.");

            if (!TryGetSpiritActiveAtIndex(contracts, spiritId, abilityIndex, out AbilityAction ability))
                return Stale(entry.Kind, "Elemental spirit ability unavailable.");

            return ValidRacial(entry, ability, abilityIndex);
        }

        static HotbarResolvedAction ResolveInventoryActive(BaseActor actor, HotbarEntry entry)
        {
            if (!HotbarItemLookup.TryFindOwnedItem(
                    actor,
                    entry.itemInstanceId,
                    out ItemInstance instance,
                    out BaseActor owner,
                    out _,
                    out _))
            {
                return Stale(entry.Kind, "Item not found.");
            }

            ItemData definition = instance.Definition;
            if (definition?.activeAbilities == null
                || entry.abilityIndex < 0
                || entry.abilityIndex >= definition.activeAbilities.Count)
            {
                return Stale(entry.Kind, "Item ability unavailable.");
            }

            AbilityAction ability = definition.activeAbilities[entry.abilityIndex];
            if (ability == null)
                return Stale(entry.Kind, "Item ability missing.");

            return new HotbarResolvedAction
            {
                IsValid = true,
                IsStale = false,
                Ability = ability,
                Source = PlayerAbilitySource.InventoryItem,
                SlotIndex = 0,
                AbilityIndex = entry.abilityIndex,
                ItemInstance = instance,
                ItemOwner = owner,
                Kind = entry.Kind,
            };
        }

        static HotbarResolvedAction ResolveInventoryUse(BaseActor actor, HotbarEntry entry)
        {
            if (!HotbarItemLookup.TryFindOwnedItem(
                    actor,
                    entry.itemInstanceId,
                    out ItemInstance instance,
                    out BaseActor owner,
                    out bool isEquipped,
                    out _))
            {
                return Stale(entry.Kind, "Item not found.");
            }

            ItemData definition = instance.Definition;
            if (definition == null)
                return Stale(entry.Kind, "Item definition missing.");

            AbilityAction ability = ResolveInventoryUseAbility(definition);
            if (ability == null && !CanInventoryUseWithoutAbility(definition))
                return Stale(entry.Kind, "Item cannot be used.");

            return new HotbarResolvedAction
            {
                IsValid = true,
                IsStale = false,
                Ability = ability,
                Source = PlayerAbilitySource.InventoryItem,
                ItemInstance = instance,
                ItemOwner = owner,
                Kind = entry.Kind,
                DenyReason = isEquipped ? "Unequip item before using from hotbar." : null,
            };
        }

        static AbilityAction ResolveInventoryUseAbility(ItemData definition)
        {
            if (definition.activeAbilities != null && definition.activeAbilities.Count > 0)
                return definition.activeAbilities[0];

            if (definition is EvocableItemData evocable)
                return EvocableChargeRules.GetInvokeAbility(evocable);

            return null;
        }

        static bool CanInventoryUseWithoutAbility(ItemData definition) =>
            definition.category == ItemCategory.Key;

        public static int EquipmentSlotToLegacyIndex(EquipmentSlot slot) =>
            slot switch
            {
                EquipmentSlot.MainHand => 0,
                EquipmentSlot.OffHand => 1,
                EquipmentSlot.Torso => 2,
                EquipmentSlot.Head => 3,
                EquipmentSlot.Accessory_MainHand => 4,
                EquipmentSlot.Accessory_OffHand => 5,
                EquipmentSlot.Accessory_Head => 6,
                EquipmentSlot.Legs => 6,
                EquipmentSlot.Feet => 6,
                _ => 6,
            };

        public static string BuildSpiritImprintBindingKey(string nodeId, int abilityIndex) =>
            $"{SpiritImprintBindingPrefix}{nodeId}:{abilityIndex}";

        public static string BuildElementalSpiritBindingKey(string spiritId, int abilityIndex) =>
            $"{ElementalSpiritBindingPrefix}{spiritId}:{abilityIndex}";

        public static bool TryGetSpiritActiveAtIndex(
            ElementalSpiritContractsRuntime contracts,
            string spiritId,
            int abilityIndex,
            out AbilityAction ability)
        {
            ability = null;
            if (contracts == null || string.IsNullOrEmpty(spiritId) || abilityIndex < 0)
                return false;

            if (!contracts.TryGetContractLevel(spiritId, out int contractLevel))
                return false;

            var actives = new List<AbilityAction>();
            CollectElementalSpiritActives(contracts, spiritId, contractLevel, actives);
            if (abilityIndex >= actives.Count)
                return false;

            ability = actives[abilityIndex];
            return ability != null;
        }

        public static void CollectElementalSpiritActives(
            ElementalSpiritContractsRuntime contracts,
            string spiritId,
            int contractLevel,
            List<AbilityAction> destination)
        {
            if (destination == null || contracts == null || string.IsNullOrEmpty(spiritId))
                return;

            if (!TryGetSpiritDefinition(contracts, spiritId, out ElementalSpiritDefinition definition))
                return;

            for (int level = 1; level <= contractLevel; level++)
            {
                if (!definition.TryGetLevelRow(level, out ElementalSpiritLevelData row) || row.activeEntries == null)
                    continue;

                foreach (ElementalSpiritActiveEntry activeEntry in row.activeEntries)
                {
                    if (activeEntry?.ability != null)
                        destination.Add(activeEntry.ability);
                }
            }
        }

        static bool TryGetSpiritDefinition(
            ElementalSpiritContractsRuntime contracts,
            string spiritId,
            out ElementalSpiritDefinition definition)
        {
            definition = null;
            return contracts != null && contracts.TryGetSpiritDefinition(spiritId, out definition);
        }

        static bool ContainsNode(IReadOnlyList<string> path, string nodeId)
        {
            for (int i = 0; i < path.Count; i++)
            {
                if (path[i] == nodeId)
                    return true;
            }

            return false;
        }

        static HotbarResolvedAction Valid(
            HotbarEntryKind kind,
            AbilityAction ability,
            PlayerAbilitySource source,
            int slotIndex,
            int abilityIndex) =>
            new HotbarResolvedAction
            {
                IsValid = true,
                IsStale = false,
                Ability = ability,
                Source = source,
                SlotIndex = slotIndex,
                AbilityIndex = abilityIndex,
                Kind = kind,
            };

        static HotbarResolvedAction ValidRacial(HotbarEntry entry, AbilityAction ability, int abilityIndex) =>
            new HotbarResolvedAction
            {
                IsValid = true,
                IsStale = false,
                Ability = ability,
                Source = PlayerAbilitySource.RacialActive,
                SlotIndex = 0,
                AbilityIndex = abilityIndex,
                Kind = entry.Kind,
                RacialBindingKey = entry.racialBindingKey,
            };

        static HotbarResolvedAction Invalid(HotbarEntryKind kind, string reason) =>
            new HotbarResolvedAction
            {
                IsValid = false,
                IsStale = false,
                Kind = kind,
                DenyReason = reason,
            };

        static HotbarResolvedAction Stale(HotbarEntryKind kind, string reason) =>
            new HotbarResolvedAction
            {
                IsValid = false,
                IsStale = true,
                Kind = kind,
                DenyReason = reason,
            };
    }
}
