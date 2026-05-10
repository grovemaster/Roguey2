namespace JRogue.UI.Inventory
{
    /// <summary>How filtered rows are ordered and sectioned in <see cref="InventoryPresentationModel"/>.</summary>
    public enum InventorySortMode
    {
        /// <summary>Category headers; alphabetical within each category.</summary>
        CategoryThenName = 0,

        /// <summary>Category headers; favorites first, then alphabetical.</summary>
        CategoryFavoritesFirst = 1,

        /// <summary>Single list, all items A–Z.</summary>
        FlatByName = 2,

        /// <summary>Single list, heaviest stacks first.</summary>
        FlatByWeightDesc = 3
    }
}
