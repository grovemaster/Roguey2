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
        public const string ElementalSpiritActiveBindingPrefix = "ElementalSpiritActive:";
        public const string ElementalSpiritSummonBindingPrefix = "ElementalSpiritSummon:";
        public const string TieflingImplantActiveBindingPrefix = "TieflingImplant:";
        public const string LegacyElementalSpiritBindingPrefix = "ElementalSpirit:";

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
                HotbarEntryKind.DragonianSpell => ResolveDragonianSpell(actor, entry),
                HotbarEntryKind.RacialActive => ResolveRacial(actor, entry),
                HotbarEntryKind.ElementalSpiritSummon => ResolveElementalSpiritSummon(actor, entry),
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

        static HotbarResolvedAction ResolveDragonianSpell(BaseActor actor, HotbarEntry entry)
        {
            CharacterStats stats = actor.stats;
            if (stats == null || stats.race != Race.Dragonian)
                return Stale(entry.Kind, "Not a Dragonian.");

            DragonianSpellsRuntime dragonianSpells = actor.GetComponent<DragonianSpellsRuntime>();
            if (dragonianSpells == null)
                return Stale(entry.Kind, "No Dragonian spell runtime.");

            AbilityAction ability = dragonianSpells.GetMemorizedAbility(entry.abilityIndex);
            if (ability == null)
                return Stale(entry.Kind, "Dragonian spell unavailable.");

            return Valid(
                entry.Kind,
                ability,
                PlayerAbilitySource.DragonianSpell,
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

            if (IsElementalSpiritActiveBinding(bindingKey))
                return ResolveElementalSpiritActive(actor, entry, bindingKey);

            if (bindingKey.StartsWith(TieflingImplantActiveBindingPrefix, System.StringComparison.Ordinal))
                return ResolveTieflingImplantActive(actor, entry, bindingKey);

            if (bindingKey.StartsWith(LegacyElementalSpiritBindingPrefix, System.StringComparison.Ordinal))
                return Stale(entry.Kind, "Elemental spirit binding format changed.");

            return Stale(entry.Kind, "Unknown racial binding key.");
        }

        static HotbarResolvedAction ResolveElementalSpiritSummon(BaseActor actor, HotbarEntry entry)
        {
            string instanceId = entry.contractInstanceId;
            if (string.IsNullOrEmpty(instanceId))
                return Stale(entry.Kind, "Missing contract instance id.");

            ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (contracts == null)
                return Stale(entry.Kind, "No elemental spirit runtime.");

            if (!contracts.TryGetPreset(instanceId, out ElementalSpiritContractPreset preset) || preset.spirit == null)
                return Stale(entry.Kind, "Spirit instance is not contracted.");

            return new HotbarResolvedAction
            {
                IsValid = true,
                IsStale = false,
                Kind = entry.Kind,
                ContractInstanceId = instanceId,
            };
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

        static HotbarResolvedAction ResolveElementalSpiritActive(
            BaseActor actor,
            HotbarEntry entry,
            string bindingKey)
        {
            if (!TryParseElementalSpiritActiveBinding(bindingKey, out string abilityAssetName))
                return Stale(entry.Kind, "Invalid Elemental Spirit active binding.");

            ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (contracts == null)
                return Stale(entry.Kind, "No elemental spirit runtime.");

            if (!TryFindRosterSpiritActiveByAssetName(contracts, abilityAssetName, out AbilityAction ability))
                return Stale(entry.Kind, "Elemental spirit ability unavailable.");

            return ValidRacial(entry, ability, 0);
        }

        static HotbarResolvedAction ResolveTieflingImplantActive(
            BaseActor actor,
            HotbarEntry entry,
            string bindingKey)
        {
            if (!TryParseTieflingImplantActiveBinding(bindingKey, out ImplantSlot slot, out int abilityIndex))
                return Stale(entry.Kind, "Invalid Tiefling implant binding.");

            TieflingImplantsRuntime implants = actor.GetComponent<TieflingImplantsRuntime>();
            if (implants == null)
                return Stale(entry.Kind, "No Tiefling implant runtime.");

            if (!implants.TryGetInstalled(slot, out CyborgImplantDefinition implant)
                || implant?.activeAbilities == null
                || abilityIndex < 0
                || abilityIndex >= implant.activeAbilities.Count)
            {
                return Stale(entry.Kind, "Tiefling implant ability unavailable.");
            }

            AbilityAction ability = implant.activeAbilities[abilityIndex];
            if (ability == null)
                return Stale(entry.Kind, "Tiefling implant ability missing.");

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

        public static string BuildElementalSpiritActiveBindingKey(string abilityAssetName) =>
            $"{ElementalSpiritActiveBindingPrefix}{abilityAssetName}";

        public static string BuildElementalSpiritSummonBindingKey(string contractInstanceId) =>
            $"{ElementalSpiritSummonBindingPrefix}{contractInstanceId}";

        public static bool IsElementalSpiritActiveBinding(string bindingKey) =>
            !string.IsNullOrEmpty(bindingKey)
            && bindingKey.StartsWith(ElementalSpiritActiveBindingPrefix, System.StringComparison.Ordinal);

        public static bool TryParseElementalSpiritActiveBinding(string bindingKey, out string abilityAssetName)
        {
            abilityAssetName = null;
            if (!IsElementalSpiritActiveBinding(bindingKey))
                return false;

            abilityAssetName = bindingKey.Substring(ElementalSpiritActiveBindingPrefix.Length);
            return !string.IsNullOrEmpty(abilityAssetName);
        }

        public static string BuildTieflingImplantActiveBindingKey(ImplantSlot slot, int abilityIndex) =>
            $"{TieflingImplantActiveBindingPrefix}{slot}:{abilityIndex}";

        public static bool TryParseTieflingImplantActiveBinding(
            string bindingKey,
            out ImplantSlot slot,
            out int abilityIndex)
        {
            slot = default;
            abilityIndex = -1;
            if (string.IsNullOrEmpty(bindingKey)
                || !bindingKey.StartsWith(TieflingImplantActiveBindingPrefix, System.StringComparison.Ordinal))
            {
                return false;
            }

            string remainder = bindingKey.Substring(TieflingImplantActiveBindingPrefix.Length);
            int separator = remainder.LastIndexOf(':');
            if (separator <= 0 || separator >= remainder.Length - 1)
                return false;

            string slotText = remainder.Substring(0, separator);
            if (!System.Enum.TryParse(slotText, out slot))
                return false;

            return int.TryParse(remainder.Substring(separator + 1), out abilityIndex) && abilityIndex >= 0;
        }

        public static bool TryFindSummonedSpiritActiveByAssetName(
            ElementalSpiritContractsRuntime contracts,
            string abilityAssetName,
            out AbilityAction ability) =>
            TryFindSpiritActiveByAssetName(
                contracts,
                abilityAssetName,
                summonedOnly: true,
                out ability);

        public static bool TryFindRosterSpiritActiveByAssetName(
            ElementalSpiritContractsRuntime contracts,
            string abilityAssetName,
            out AbilityAction ability) =>
            TryFindSpiritActiveByAssetName(
                contracts,
                abilityAssetName,
                summonedOnly: false,
                out ability);

        public static bool HasSummonedSpiritActiveByAssetName(
            ElementalSpiritContractsRuntime contracts,
            string abilityAssetName) =>
            TryFindSummonedSpiritActiveByAssetName(contracts, abilityAssetName, out _);

        static bool TryFindSpiritActiveByAssetName(
            ElementalSpiritContractsRuntime contracts,
            string abilityAssetName,
            bool summonedOnly,
            out AbilityAction ability)
        {
            ability = null;
            if (contracts == null || string.IsNullOrEmpty(abilityAssetName))
                return false;

            foreach (ElementalSpiritContractPreset preset in contracts.ContractedSpirits)
            {
                if (preset?.spirit == null)
                    continue;

                preset.EnsureInstanceId();
                if (summonedOnly && !contracts.IsInstanceSummoned(preset.contractInstanceId))
                    continue;

                if (!contracts.TryGetContractLevelForInstance(preset.contractInstanceId, out int contractLevel))
                    continue;

                if (!TryGetSpiritActiveByAssetName(preset.spirit, contractLevel, abilityAssetName, out ability))
                    continue;

                if (ability == null)
                    continue;

                return true;
            }

            return false;
        }

        public static bool TryGetSpiritActiveByAssetName(
            ElementalSpiritDefinition definition,
            int contractLevel,
            string abilityAssetName,
            out AbilityAction ability)
        {
            ability = null;
            if (definition == null || string.IsNullOrEmpty(abilityAssetName))
                return false;

            for (int level = 1; level <= contractLevel; level++)
            {
                if (!definition.TryGetLevelRow(level, out ElementalSpiritLevelData row) || row.activeEntries == null)
                    continue;

                foreach (ElementalSpiritActiveEntry activeEntry in row.activeEntries)
                {
                    if (activeEntry?.ability == null)
                        continue;

                    if (activeEntry.ability.name != abilityAssetName)
                        continue;

                    ability = activeEntry.ability;
                    return true;
                }
            }

            return false;
        }

        public static void CollectDedupedElementalSpiritActives(
            ElementalSpiritContractsRuntime contracts,
            HashSet<string> seenAbilityAssetNames,
            List<(AbilityAction ability, string displayName)> destination)
        {
            if (destination == null || contracts == null)
                return;

            seenAbilityAssetNames ??= new HashSet<string>();

            foreach (ElementalSpiritContractPreset preset in contracts.ContractedSpirits)
            {
                if (preset?.spirit == null)
                    continue;

                preset.EnsureInstanceId();
                if (!contracts.TryGetContractLevelForInstance(preset.contractInstanceId, out int contractLevel))
                    continue;

                ElementalSpiritDefinition definition = preset.spirit;
                for (int level = 1; level <= contractLevel; level++)
                {
                    if (!definition.TryGetLevelRow(level, out ElementalSpiritLevelData row) || row.activeEntries == null)
                        continue;

                    foreach (ElementalSpiritActiveEntry activeEntry in row.activeEntries)
                    {
                        AbilityAction ability = activeEntry?.ability;
                        if (ability == null || string.IsNullOrEmpty(ability.name))
                            continue;

                        if (!seenAbilityAssetNames.Add(ability.name))
                            continue;

                        string displayName = !string.IsNullOrWhiteSpace(ability.abilityName)
                            ? ability.abilityName.Trim()
                            : ability.name;
                        destination.Add((ability, displayName));
                    }
                }
            }
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
