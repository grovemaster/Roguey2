using System.Collections.Generic;
using JRogue.Item;
using JRogue.Item.World;
using UnityEngine;

namespace JRogue.Manager.Floor
{
    public enum TelekinesisPickableSource
    {
        None = 0,
        PileEntry = 1,
        WorldItem = 2
    }

    public readonly struct TelekinesisPickable
    {
        public TelekinesisPickable(
            TelekinesisPickableSource source,
            FloorItemEntry pileEntry,
            WorldItem worldItem,
            ItemInstance instance)
        {
            Source = source;
            PileEntry = pileEntry;
            WorldItem = worldItem;
            Instance = instance;
        }

        public TelekinesisPickableSource Source { get; }
        public FloorItemEntry PileEntry { get; }
        public WorldItem WorldItem { get; }
        public ItemInstance Instance { get; }
    }

    /// <summary>Resolves a single physical floor item for <see cref="JRogue.Ability.Telekinesis.TelekinesisAbility"/>.</summary>
    public static class TelekinesisFloorQuery
    {
        public static bool IsPhysicalPickable(ItemInstance instance)
        {
            if (instance?.Definition == null)
                return false;

            if (instance.IsManaStone || instance.IsCurrency)
                return false;

            ItemCategory category = instance.Definition.category;
            if (category == ItemCategory.Currency || category == ItemCategory.Essence)
                return false;

            return true;
        }

        public static bool IsPhysicalPickable(WorldItem worldItem)
        {
            if (worldItem == null || worldItem.data == null)
                return false;

            if (worldItem.data is ManaStoneItemData)
                return false;

            if (worldItem.data.category == ItemCategory.Currency
                || worldItem.data.category == ItemCategory.Essence)
                return false;

            return true;
        }

        public static bool TryGetSinglePickable(Vector3Int tile, out TelekinesisPickable pickable)
        {
            pickable = default;

            FloorItemPileService pile = FloorItemPileService.Instance;
            IReadOnlyList<FloorItemEntry> pileEntries =
                pile != null ? pile.GetEntries(tile) : System.Array.Empty<FloorItemEntry>();
            IReadOnlyList<WorldItem> worldItems = FloorPickupQuery.GetAllWorldItemsOnTile(tile);

            if (pileEntries.Count > 0 && worldItems.Count > 0)
                return false;

            if (pileEntries.Count > 0)
                return TryGetSingleFromPile(pileEntries, out pickable);

            return TryGetSingleFromWorld(worldItems, out pickable);
        }

        static bool TryGetSingleFromPile(IReadOnlyList<FloorItemEntry> entries, out TelekinesisPickable pickable)
        {
            pickable = default;
            FloorItemEntry match = null;

            for (int i = 0; i < entries.Count; i++)
            {
                FloorItemEntry entry = entries[i];
                if (!IsPhysicalPickable(entry?.instance))
                    continue;

                if (match != null)
                    return false;

                match = entry;
            }

            if (match == null)
                return false;

            pickable = new TelekinesisPickable(
                TelekinesisPickableSource.PileEntry,
                match,
                null,
                match.instance);
            return true;
        }

        static bool TryGetSingleFromWorld(IReadOnlyList<WorldItem> worldItems, out TelekinesisPickable pickable)
        {
            pickable = default;
            WorldItem match = null;

            for (int i = 0; i < worldItems.Count; i++)
            {
                WorldItem item = worldItems[i];
                if (!IsPhysicalPickable(item))
                    continue;

                if (match != null)
                    return false;

                match = item;
            }

            if (match == null)
                return false;

            pickable = new TelekinesisPickable(
                TelekinesisPickableSource.WorldItem,
                null,
                match,
                null);
            return true;
        }
    }
}
