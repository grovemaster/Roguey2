using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.Status;
using UnityEngine;

namespace JRogue.World.Town
{
    public static class PartyTownRestService
    {
        const string LogPrefix = "[PartyTownRest]";

        public static void RestoreFullParty()
        {
            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return;

            AbilityCooldownService.ClearAll();

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member.stats == null)
                    continue;

                RestoreMember(member);
            }

            Debug.Log($"{LogPrefix} Party fully restored (HP, resources, cooldowns, statuses).");
        }

        static void RestoreMember(BaseActor member)
        {
            CharacterStats stats = member.stats;
            stats.currentHP = stats.MaxHP;

            if (stats.MaxSoulPower > 0)
                stats.currentSoulPower = stats.MaxSoulPower;

            if (stats.MaxMagicPower > 0)
                stats.currentMagicPower = stats.MaxMagicPower;

            if (stats.MaxDivinePower > 0)
                stats.currentDivinePower = stats.MaxDivinePower;

            StatusEffectController statuses = member.GetComponent<StatusEffectController>();
            statuses?.ClearAll();

            ClearItemCooldowns(member);
        }

        static void ClearItemCooldowns(BaseActor member)
        {
            InventoryManager inventory = member.GetComponent<InventoryManager>();
            if (inventory?.CarriedItems != null)
            {
                for (int i = 0; i < inventory.CarriedItems.Count; i++)
                    ClearInstanceCooldowns(inventory.CarriedItems[i]);
            }

            EquipmentManager equipment = member.GetComponent<EquipmentManager>();
            if (equipment?.EquippedSnapshot == null)
                return;

            foreach (ItemInstance item in equipment.EquippedSnapshot.Values)
                ClearInstanceCooldowns(item);
        }

        static void ClearInstanceCooldowns(ItemInstance instance)
        {
            if (instance == null)
                return;

            instance.HelmetCooldownTurnsRemaining = 0;
        }
    }
}
