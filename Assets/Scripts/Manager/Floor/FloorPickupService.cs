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

        public static bool TryAutoPickup(FloorItemPileService pile, FloorItemEntry entry, GameObject picker)
        {
            if (pile == null || entry?.instance == null)
                return false;

            ItemData def = entry.instance.Definition;
            if (def == null)
                return false;

            if (def is ManaStoneItemData ms)
            {
                PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
                if (ledger == null)
                {
                    Debug.LogWarning("[LOOT] Mana stone pickup failed: no PartyManaStoneLedger.");
                    return false;
                }

                ledger.Add(ms.tier, entry.instance.ManaStoneSourceSpeciesId, entry.instance.Quantity);
                pile.RemoveEntry(entry.entryId);
                Debug.Log(
                    $"[LOOT] Auto-picked Mana Stone T{ms.tier} ({entry.instance.ManaStoneSourceSpeciesId}).");
                return true;
            }

            if (picker == null)
                return false;

            InventoryManager inventory = picker.GetComponent<InventoryManager>();
            if (inventory == null || !inventory.AddItem(entry.instance))
                return false;

            pile.RemoveEntry(entry.entryId);
            Debug.Log($"[LOOT] Auto-picked {def.itemName}.");
            return true;
        }

        public static bool TryAutoPickup(WorldItem worldItem, GameObject picker, bool allowConfirmGated) =>
            InventoryCollector.TryCollectWorldItem(worldItem, picker, allowConfirmGated);
    }
}
