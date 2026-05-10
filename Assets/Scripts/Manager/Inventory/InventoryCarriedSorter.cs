using System;
using System.Collections.Generic;
using JRogue.Item;
using JRogue.UI.Inventory;

namespace JRogue.Manager.Inventory
{
    static class InventoryCarriedSorter
    {
        public static void SortInPlace(List<ItemInstance> carried)
        {
            if (carried == null || carried.Count < 2)
                return;

            carried.Sort(CompareDefault);
        }

        static int CompareDefault(ItemInstance a, ItemInstance b)
        {
            if (a?.Definition == null)
                return b?.Definition == null ? 0 : 1;
            if (b?.Definition == null)
                return -1;

            int ca = ItemCategoryRegistry.Get(a.Definition.category).SortOrder;
            int cb = ItemCategoryRegistry.Get(b.Definition.category).SortOrder;
            int c = ca.CompareTo(cb);
            if (c != 0)
                return c;

            return string.Compare(
                a.Definition.itemName,
                b.Definition.itemName,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
