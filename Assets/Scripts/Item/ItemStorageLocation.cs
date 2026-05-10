namespace JRogue.Item
{
    /// <summary>
    /// Where an <see cref="ItemInstance"/> is considered to live for UI / policy.
    /// World drops set <see cref="OnGround"/> before pickup; bags and equipment update on transfer.
    /// </summary>
    public enum ItemStorageLocation
    {
        Unknown = 0,
        OnGround = 1,
        Carried = 2,
        Equipped = 3
    }
}
