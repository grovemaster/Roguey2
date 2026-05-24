using System.Collections.Generic;
using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Manager.Floor
{
    public static class FloorPickupService
    {
        public static void PickupConfirmGatedAt(Vector3Int tile, GameObject picker)
        {
            FloorItemPileService pile = FloorItemPileService.Instance;
            if (pile != null)
            {
                IReadOnlyList<FloorItemEntry> entries = pile.GetConfirmGatedAutoPickupEntries(tile);
                for (int i = 0; i < entries.Count; i++)
                    TryAutoPickup(pile, entries[i], picker);
            }

            IReadOnlyList<WorldItem> worldItems = FloorPickupQuery.GetConfirmGatedWorldItems(tile);
            for (int i = 0; i < worldItems.Count; i++)
                TryAutoPickup(worldItems[i], picker, allowConfirmGated: true);
        }

        public static void PickupSilentAt(Vector3Int tile, GameObject picker)
        {
            FloorItemPileService pile = FloorItemPileService.Instance;
            if (pile != null)
            {
                IReadOnlyList<FloorItemEntry> entries = pile.GetSilentAutoPickupEntries(tile);
                for (int i = 0; i < entries.Count; i++)
                    TryAutoPickup(pile, entries[i], picker);
            }

            if (picker == null)
                return;

            IReadOnlyList<WorldItem> worldItems = FloorPickupQuery.GetSilentAutoPickupWorldItems(tile);
            for (int i = 0; i < worldItems.Count; i++)
                TryAutoPickup(worldItems[i], picker, allowConfirmGated: false);
        }

        public static bool TryAutoPickup(FloorItemPileService pile, FloorItemEntry entry, GameObject picker) =>
            TryPickupPileEntry(pile, entry, picker, logPrefix: "[LOOT]");

        static bool TryPickupPileEntry(
            FloorItemPileService pile,
            FloorItemEntry entry,
            GameObject picker,
            string logPrefix)
        {
            if (pile == null || entry?.instance == null)
                return false;

            ItemInstance inst = entry.instance;
            ItemData def = inst.Definition;
            if (def == null)
                return false;

            if (inst.IsManaStone && def is ManaStoneItemData ms)
            {
                PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
                if (ledger == null)
                {
                    Debug.LogWarning($"{logPrefix} Mana stone pickup failed: no PartyManaStoneLedger.");
                    return false;
                }

                ledger.Add(ms.tier, inst.ManaStoneSourceSpeciesId, inst.Quantity);
                pile.RemoveEntry(entry.entryId);
                Debug.Log($"{logPrefix} Picked up Mana Stone T{ms.tier} ({inst.ManaStoneSourceSpeciesId}).");
                return true;
            }

            if (picker == null)
                return false;

            InventoryManager inventory = picker.GetComponent<InventoryManager>();
            if (inventory == null || !inventory.AddItem(inst))
                return false;

            pile.RemoveEntry(entry.entryId);
            Debug.Log($"{logPrefix} Picked up {def.itemName}.");
            return true;
        }

        public static bool TryAutoPickup(WorldItem worldItem, GameObject picker, bool allowConfirmGated) =>
            InventoryCollector.TryCollectWorldItem(worldItem, picker, allowConfirmGated);

        /// <summary>Manual floor pickup (`,` / menu) — ignores <see cref="ItemData.autoPickupOnStep"/>.</summary>
        public static bool TryManualPickup(FloorItemPileService pile, FloorItemEntry entry, GameObject picker) =>
            TryPickupPileEntry(pile, entry, picker, logPrefix: "[Pickup]");

        public static bool TryManualPickup(WorldItem worldItem, GameObject picker) =>
            InventoryCollector.TryCollectWorldItem(worldItem, picker, allowConfirmGated: true, manualPickup: true);
    }
}
