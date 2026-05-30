using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Manager.Inventory
{
    /// <summary>Party-scoped recharge ticks for carried rechargeable evocables (§5.3 option A).</summary>
    public static class EvocableRechargeService
    {
        static int _lastRechargeTickFrame = -1;

        /// <summary>
        /// Advances rechargeable evocables once per completed player turn cycle
        /// (leader move, wait, inventory invoke, etc.), including formation rush endings.
        /// </summary>
        public static void TickPartyAfterPlayerPhase()
        {
            if (UnityEngine.Time.frameCount == _lastRechargeTickFrame)
                return;
            _lastRechargeTickFrame = UnityEngine.Time.frameCount;

            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers == null)
                return;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null)
                    continue;

                InventoryManager inventory = member.GetComponent<InventoryManager>();
                if (inventory != null)
                    TickInventory(inventory);
            }
        }

        /// <summary>Exposed for unit tests (single inventory tick).</summary>
        public static void TickInventoryForTests(InventoryManager inventory) => TickInventory(inventory);

        static void TickInventory(InventoryManager inventory)
        {
            IReadOnlyList<ItemInstance> carried = inventory.CarriedItems;
            for (int i = 0; i < carried.Count; i++)
            {
                ItemInstance instance = carried[i];
                if (instance?.Definition is not EvocableItemData definition)
                    continue;

                if (definition.consumesWhenEmpty)
                    continue;

                EvocableChargeRules.ClampCharges(instance);

                if (instance.CurrentCharges >= instance.MaxCharges)
                {
                    instance.RechargePhasesAccumulated = 0;
                    continue;
                }

                int interval = Mathf.Max(1, definition.rechargeIntervalPlayerPhases);
                instance.RechargePhasesAccumulated++;
                if (instance.RechargePhasesAccumulated < interval)
                    continue;

                instance.RechargePhasesAccumulated = 0;
                int before = instance.CurrentCharges;
                instance.CurrentCharges = Mathf.Min(instance.MaxCharges, instance.CurrentCharges + 1);
                EvocableChargeRules.ClampCharges(instance);

                Debug.Log(
                    $"{EvocableChargeRules.LogPrefix} Recharge +1 id={instance.Id.Substring(0, Mathf.Min(6, instance.Id.Length))} " +
                    $"{before}->{instance.CurrentCharges}/{instance.MaxCharges} ({definition.itemName}).");
            }
        }
    }
}
