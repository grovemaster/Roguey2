using System.Collections.Generic;
using JRogue.Item;
using JRogue.Manager.Equipment;

namespace JRogue.UI.Inventory
{
    /// <summary>
    /// Read-only snapshot of inventory rows for UI. Built from gameplay managers; UI does not mutate lists directly.
    /// </summary>
    public sealed class InventoryViewModel
    {
        public readonly struct Row
        {
            public Row(
                char letter,
                ItemData item,
                int quantity,
                int firstInventoryIndex,
                bool isEquipped,
                EquipmentSlot? equippedSlot,
                float stackedWeight)
            {
                Letter = letter;
                Item = item;
                Quantity = quantity;
                FirstInventoryIndex = firstInventoryIndex;
                IsEquipped = isEquipped;
                EquippedSlot = equippedSlot;
                StackedWeight = stackedWeight;
            }

            public char Letter { get; }
            public ItemData Item { get; }
            public int Quantity { get; }
            public int FirstInventoryIndex { get; }
            public bool IsEquipped { get; }
            public EquipmentSlot? EquippedSlot { get; }
            public float StackedWeight { get; }
        }

        readonly List<Row> _rows;

        InventoryViewModel(List<Row> rows) => _rows = rows;

        public IReadOnlyList<Row> Rows => _rows;

        public static InventoryViewModel Build(IReadOnlyList<ItemData> items, EquipmentManager equipment)
        {
            var raw = new List<Row>();

            int idx = 0;
            while (idx < items.Count)
            {
                ItemData cur = items[idx];
                int count = 1;
                float stackedWeight = cur.weight;
                while (idx + count < items.Count && items[idx + count] == cur)
                {
                    count++;
                    stackedWeight += cur.weight;
                }

                bool equipped = equipment.TryGetEquippedSlot(cur, out EquipmentSlot slot);

                raw.Add(new Row(
                    '?',
                    cur,
                    count,
                    idx,
                    equipped,
                    equipped ? slot : (EquipmentSlot?)null,
                    stackedWeight));

                idx += count;
            }

            for (int i = 0; i < raw.Count; i++)
            {
                Row r = raw[i];
                raw[i] = new Row(
                    LetterForIndex(i),
                    r.Item,
                    r.Quantity,
                    r.FirstInventoryIndex,
                    r.IsEquipped,
                    r.EquippedSlot,
                    r.StackedWeight);
            }

            return new InventoryViewModel(raw);
        }

        static char LetterForIndex(int i)
        {
            if (i < 26) return (char)('a' + i);
            if (i < 52) return (char)('A' + (i - 26));
            return '?';
        }
    }
}
