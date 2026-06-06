using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Item
{
    /// <summary>Runtime rules for <see cref="LightSourceItemData"/> (Handheld Torch, Helmet of Light).</summary>
    public static class LightSourceItemRules
    {
        public const int DefaultHelmetLightDurationTurns = 5;

        public const string LogPrefixHelmet = "[Lighting:Helmet]";
        public const string LogPrefixCooldown = "[Ability:Cooldown]";

        public static bool IsLightSource(ItemData definition) => definition is LightSourceItemData;

        public static LightSourceItemData AsLightSource(ItemData definition) => definition as LightSourceItemData;

        /// <summary>Whether this equipped instance should register a virtual carried emitter right now.</summary>
        public static bool ShouldEmitCarriedLight(ItemInstance instance, EquipmentSlot slot, bool isEquipped)
        {
            if (instance?.Definition is not LightSourceItemData lightSource || !isEquipped)
                return false;

            if (lightSource.IsPassiveEquippedEmitter && IsAccessorySlot(slot))
                return true;

            if (slot == EquipmentSlot.Head && instance.HelmetLightTurnsRemaining > 0)
                return true;

            return false;
        }

        public static bool IsAccessorySlot(EquipmentSlot slot) =>
            slot is EquipmentSlot.Accessory_MainHand
                or EquipmentSlot.Accessory_OffHand
                or EquipmentSlot.Accessory_Head;

        public static bool CanActivateTimedLight(ItemInstance instance, AbilityAction ability)
        {
            if (instance == null || ability == null)
                return false;

            if (instance.HelmetLightTurnsRemaining > 0)
            {
                Debug.Log($"{LogPrefixCooldown} Blocked — light still active ({instance.HelmetLightTurnsRemaining} turns).");
                return false;
            }

            if (instance.HelmetCooldownTurnsRemaining > 0)
            {
                Debug.Log(
                    $"{LogPrefixCooldown} Blocked — cooldown ({instance.HelmetCooldownTurnsRemaining} turns remaining).");
                return false;
            }

            if (AbilityCooldownService.GetRemainingCooldown(instance, ability) > 0)
                return false;

            return true;
        }

        public static void BeginHelmetRadiance(ItemInstance instance, AbilityAction ability, int durationTurns = DefaultHelmetLightDurationTurns)
        {
            if (instance == null)
                return;

            instance.HelmetLightTurnsRemaining = Mathf.Max(1, durationTurns);
            instance.HelmetCooldownTurnsRemaining = 0;
            Debug.Log($"{LogPrefixHelmet} Radiance started for {instance.HelmetLightTurnsRemaining} player turns.");
        }

        public static int ResolveCooldownAfterLightExpires(ItemInstance instance)
        {
            if (instance?.Definition is not LightSourceItemData lightSource)
                return 0;

            if (lightSource.activeAbilities == null || lightSource.activeAbilities.Count == 0)
                return 3;

            AbilityAction ability = lightSource.activeAbilities[0];
            return ability != null ? Mathf.Max(0, ability.cooldownTurns) : 3;
        }

        /// <summary>Decrements helmet light/cooldown counters on all party-owned items once per completed player phase.</summary>
        public static void TickPartyAfterPlayerPhase()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers == null)
                return;

            var seen = new HashSet<string>();
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null)
                    continue;

                InventoryManager inventory = member.GetComponent<InventoryManager>();
                if (inventory != null)
                    TickInstances(inventory.CarriedItems, seen);

                EquipmentManager equipment = member.GetComponent<EquipmentManager>();
                if (equipment != null)
                {
                    foreach (KeyValuePair<EquipmentSlot, ItemInstance> pair in equipment.EquippedSnapshot)
                    {
                        if (pair.Value != null && seen.Add(pair.Value.Id))
                            TickInstance(pair.Value);
                    }
                }
            }
        }

        static void TickInstances(IReadOnlyList<ItemInstance> items, HashSet<string> seen)
        {
            if (items == null)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                ItemInstance instance = items[i];
                if (instance != null && seen.Add(instance.Id))
                    TickInstance(instance);
            }
        }

        static void TickInstance(ItemInstance instance)
        {
            if (instance.HelmetLightTurnsRemaining > 0)
            {
                instance.HelmetLightTurnsRemaining--;
                if (instance.HelmetLightTurnsRemaining == 0)
                {
                    instance.HelmetCooldownTurnsRemaining = ResolveCooldownAfterLightExpires(instance);
                    Debug.Log(
                        $"{LogPrefixHelmet} Light expired on {instance.Definition?.itemName}; " +
                        $"cooldown {instance.HelmetCooldownTurnsRemaining} turns.");
                }

                return;
            }

            if (instance.HelmetCooldownTurnsRemaining > 0)
            {
                instance.HelmetCooldownTurnsRemaining--;
                if (instance.HelmetCooldownTurnsRemaining == 0)
                    Debug.Log($"{LogPrefixHelmet} Cooldown complete on {instance.Definition?.itemName}.");
            }

            AbilityCooldownService.TickInstanceCooldowns(instance);
        }

        /// <summary>Single-instance tick for unit tests.</summary>
        public static void TickInstanceForTests(ItemInstance instance) => TickInstance(instance);

        public static string FormatInspectSubtitle(ItemInstance instance, LightSourceItemData definition)
        {
            if (instance == null || definition == null)
                return string.Empty;

            if (definition.IsPassiveEquippedEmitter)
                return "Emits light while equipped";

            if (instance.HelmetLightTurnsRemaining > 0)
                return $"Light: {instance.HelmetLightTurnsRemaining} turn(s)";

            if (instance.HelmetCooldownTurnsRemaining > 0)
                return $"Cooldown: {instance.HelmetCooldownTurnsRemaining} turn(s)";

            return string.Empty;
        }
    }
}
