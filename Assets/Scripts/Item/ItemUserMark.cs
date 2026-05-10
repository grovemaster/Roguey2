using System;

namespace JRogue.Item
{
    /// <summary>Per-instance UX flags (Phase 3). Orthogonal to <see cref="ItemInventoryRiskHint"/> on <see cref="ItemData"/>.</summary>
    [Flags]
    public enum ItemUserMark : byte
    {
        None = 0,
        Favorite = 1 << 0,
        Protected = 1 << 1,
        Junk = 1 << 2
    }
}
