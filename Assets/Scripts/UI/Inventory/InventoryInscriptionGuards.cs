using System;
using JRogue.Item;

namespace JRogue.UI.Inventory
{
    /// <summary>Future: parse roguelike-style guard tokens (!d drop, !u use, …) from <see cref="ItemInstance.UserInscription"/>.</summary>
    public static class InventoryInscriptionGuards
    {
        [Flags]
        public enum ParsedGuards
        {
            None = 0,
            /// <summary>Reserved: block drop unless explicitly confirmed.</summary>
            NoDrop = 1 << 0,
            /// <summary>Reserved: block use-on-self.</summary>
            NoUse = 1 << 1
        }

        /// <summary>Stub — returns <see cref="ParsedGuards.None"/> until token grammar is defined.</summary>
        public static ParsedGuards Parse(string inscription) =>
            string.IsNullOrWhiteSpace(inscription) ? ParsedGuards.None : ParsedGuards.None;
    }
}
