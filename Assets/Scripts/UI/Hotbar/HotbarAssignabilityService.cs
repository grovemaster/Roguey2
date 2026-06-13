using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Manager.Combat;
using JRogue.Manager.Equipment;
using JRogue.Manager.Essence;
using JRogue.Manager.Inventory;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Inventory;
using UnityEngine;

namespace JRogue.UI.Hotbar
{
    public static class HotbarAssignabilityService
    {
        public const string GroupEssence = "Essence Actives";
        public const string GroupEquipment = "Equipment Actives";
        public const string GroupMage = "Mage Spells";
        public const string GroupInventory = "Inventory";
        public const string GroupRacial = "Racial Actives";

        public static List<(HotbarEntry entry, string displayName, string group)> BuildPool(BaseActor actor)
        {
            var pool = new List<(HotbarEntry, string, string)>();
            if (actor == null)
                return pool;

            AppendEssenceActives(actor, pool);
            AppendEquipmentActives(actor, pool);
            AppendMageSpells(actor, pool);
            AppendInventoryEntries(actor, pool);
            AppendRacialActives(actor, pool);
            return pool;
        }

        static void AppendEssenceActives(
            BaseActor actor,
            List<(HotbarEntry, string, string)> pool)
        {
            EssenceSlotManager essence = actor.GetComponent<EssenceSlotManager>();
            if (essence == null)
                return;

            for (int slot = 0; slot < essence.totalSlots; slot++)
            {
                EssenceData equipped = essence.GetEssenceInSlot(slot);
                if (equipped?.activeAbilities == null)
                    continue;

                for (int abilityIndex = 0; abilityIndex < equipped.activeAbilities.Count; abilityIndex++)
                {
                    AbilityAction ability = equipped.activeAbilities[abilityIndex];
                    if (ability == null)
                        continue;

                    pool.Add((
                        new HotbarEntry
                        {
                            Kind = HotbarEntryKind.EssenceActive,
                            essenceSlotIndex = slot,
                            abilityIndex = abilityIndex,
                            abilityAssetName = ability.name,
                        },
                        FormatAbilityName(ability, equipped.essenceName),
                        GroupEssence));
                }
            }
        }

        static void AppendEquipmentActives(
            BaseActor actor,
            List<(HotbarEntry, string, string)> pool)
        {
            EquipmentManager equipment = actor.GetComponent<EquipmentManager>();
            if (equipment == null)
                return;

            foreach (var kv in equipment.EquippedSnapshot)
            {
                ItemData definition = kv.Value?.Definition;
                if (definition?.activeAbilities == null)
                    continue;

                for (int abilityIndex = 0; abilityIndex < definition.activeAbilities.Count; abilityIndex++)
                {
                    AbilityAction ability = definition.activeAbilities[abilityIndex];
                    if (ability == null)
                        continue;

                    pool.Add((
                        new HotbarEntry
                        {
                            Kind = HotbarEntryKind.EquipmentActive,
                            equipmentSlot = (int)kv.Key,
                            abilityIndex = abilityIndex,
                            abilityAssetName = ability.name,
                        },
                        FormatAbilityName(ability, definition.itemName),
                        GroupEquipment));
                }
            }
        }

        static void AppendMageSpells(
            BaseActor actor,
            List<(HotbarEntry, string, string)> pool)
        {
            CharacterStats stats = actor.stats;
            if (stats == null || stats.humanClass != HumanClass.Mage)
                return;

            HumanMageSpellsRuntime mageSpells = actor.GetComponent<HumanMageSpellsRuntime>();
            if (mageSpells == null)
                return;

            IReadOnlyList<MageSpellDefinition> equipped = mageSpells.EquippedSpells;
            for (int i = 0; i < equipped.Count; i++)
            {
                MageSpellDefinition spell = equipped[i];
                AbilityAction ability = spell?.ability;
                if (ability == null)
                    continue;

                pool.Add((
                    new HotbarEntry
                    {
                        Kind = HotbarEntryKind.HumanMageSpell,
                        abilityIndex = i,
                        abilityAssetName = ability.name,
                    },
                    FormatAbilityName(ability, spell.displayName),
                    GroupMage));
            }
        }

        static void AppendInventoryEntries(
            BaseActor actor,
            List<(HotbarEntry, string, string)> pool)
        {
            InventoryViewModel viewModel = InventoryViewModel.BuildPartyMember(new[] { actor }, actor);
            bool inCombat = CombatThreatCoordinator.Instance != null && CombatThreatCoordinator.Instance.IsInCombat;

            foreach (InventoryViewModel.Row row in viewModel.Rows)
            {
                if (row.Owner != actor || row.Item == null || row.Instance == null)
                    continue;

                if (row.Item.activeAbilities != null && row.Item.activeAbilities.Count > 0)
                {
                    for (int abilityIndex = 0; abilityIndex < row.Item.activeAbilities.Count; abilityIndex++)
                    {
                        AbilityAction ability = row.Item.activeAbilities[abilityIndex];
                        if (ability == null)
                            continue;

                        pool.Add((
                            new HotbarEntry
                            {
                                Kind = HotbarEntryKind.InventoryActive,
                                itemInstanceId = row.Instance.Id,
                                abilityIndex = abilityIndex,
                                abilityAssetName = ability.name,
                            },
                            FormatAbilityName(ability, row.Item.itemName),
                            GroupInventory));
                    }
                }

                if (row.Item is EvocableItemData evocableDef)
                {
                    AbilityAction invoke = EvocableChargeRules.GetInvokeAbility(evocableDef);
                    if (invoke == null)
                        continue;

                    pool.Add((
                        new HotbarEntry
                        {
                            Kind = HotbarEntryKind.InventoryActive,
                            itemInstanceId = row.Instance.Id,
                            abilityIndex = 0,
                            abilityAssetName = invoke.name,
                        },
                        FormatAbilityName(invoke, row.Item.itemName),
                        GroupInventory));
                }

                if (!InventoryUsability.AppearsUsableNow(row, inCombat))
                    continue;

                if (row.Item.category == ItemCategory.Key
                    || (row.Item.activeAbilities == null || row.Item.activeAbilities.Count == 0))
                {
                    pool.Add((
                        new HotbarEntry
                        {
                            Kind = HotbarEntryKind.InventoryUse,
                            itemInstanceId = row.Instance.Id,
                        },
                        row.Item.itemName,
                        GroupInventory));
                }
            }
        }

        static void AppendRacialActives(
            BaseActor actor,
            List<(HotbarEntry, string, string)> pool)
        {
            AppendSpiritImprintActives(actor, pool);
            AppendElementalSpiritSummonEntries(actor, pool);
            AppendElementalSpiritActives(actor, pool);
        }

        static void AppendSpiritImprintActives(
            BaseActor actor,
            List<(HotbarEntry, string, string)> pool)
        {
            SpiritImprintRuntime imprint = actor.GetComponent<SpiritImprintRuntime>();
            if (imprint?.Graph == null)
                return;

            IReadOnlyList<string> path = imprint.ChosenPathNodeIds;
            if (path == null)
                return;

            foreach (string nodeId in path)
            {
                if (!imprint.Graph.TryFindNode(nodeId, out SpiritImprintNodeData node)
                    || node.activeAbilities == null)
                {
                    continue;
                }

                for (int abilityIndex = 0; abilityIndex < node.activeAbilities.Count; abilityIndex++)
                {
                    AbilityAction ability = node.activeAbilities[abilityIndex];
                    if (ability == null)
                        continue;

                    pool.Add((
                        new HotbarEntry
                        {
                            Kind = HotbarEntryKind.RacialActive,
                            racialBindingKey = HotbarResolver.BuildSpiritImprintBindingKey(nodeId, abilityIndex),
                            abilityAssetName = ability.name,
                        },
                        FormatAbilityName(ability, node.displayName),
                        GroupRacial));
                }
            }
        }

        static void AppendElementalSpiritSummonEntries(
            BaseActor actor,
            List<(HotbarEntry, string, string)> pool)
        {
            ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (contracts == null)
                return;

            IReadOnlyList<ElementalSpiritContractPreset> roster = contracts.ContractedSpirits;
            foreach (ElementalSpiritContractPreset preset in roster)
            {
                if (preset?.spirit == null)
                    continue;

                preset.EnsureInstanceId();
                bool summoned = contracts.IsInstanceSummoned(preset.contractInstanceId);
                string label = ElementalSpiritDisplayNames.BuildSummonHotbarLabel(preset, roster, summoned);

                pool.Add((
                    new HotbarEntry
                    {
                        Kind = HotbarEntryKind.ElementalSpiritSummon,
                        contractInstanceId = preset.contractInstanceId,
                    },
                    label,
                    GroupRacial));
            }
        }

        static void AppendElementalSpiritActives(
            BaseActor actor,
            List<(HotbarEntry, string, string)> pool)
        {
            ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (contracts == null)
                return;

            var seen = new HashSet<string>();
            var actives = new List<(AbilityAction ability, string displayName)>();
            HotbarResolver.CollectDedupedElementalSpiritActives(contracts, seen, actives);

            foreach ((AbilityAction ability, string displayName) in actives)
            {
                if (ability == null)
                    continue;

                pool.Add((
                    new HotbarEntry
                    {
                        Kind = HotbarEntryKind.RacialActive,
                        racialBindingKey = HotbarResolver.BuildElementalSpiritActiveBindingKey(ability.name),
                        abilityAssetName = ability.name,
                    },
                    displayName,
                    GroupRacial));
            }
        }

        static string FormatAbilityName(AbilityAction ability, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(ability?.abilityName))
                return ability.abilityName.Trim();

            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback.Trim();

            return ability != null ? ability.name : "Ability";
        }
    }
}
