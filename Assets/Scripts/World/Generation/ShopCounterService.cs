using System.Collections.Generic;
using JRogue.World.Town;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>Shop counter cells that block movement but allow NPC talk across (see <see cref="Dialog.NpcCounterTalkBinding"/>).</summary>
    public static class ShopCounterService
    {
        static readonly HashSet<Vector3Int> CounterCells = new HashSet<Vector3Int>();

        public static void Clear() => CounterCells.Clear();

        public static void RegisterCounter(Vector3Int cell) => CounterCells.Add(cell);

        public static bool IsCounterCell(Vector3Int cell) => CounterCells.Contains(cell);

        public static void EnsureAdventureGuildExchangeCounters()
        {
            foreach (Vector3Int cell in AdventureGuildExchangeLayout.EnumerateCounterCells())
                RegisterCounter(cell);
        }
    }
}
