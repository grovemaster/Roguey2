using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Non-wall building mass cells (opaque interior fill on the floor tilemap) that still block actors.
    /// </summary>
    public static class TownBuildingMassService
    {
        static readonly HashSet<Vector3Int> BlockedCells = new HashSet<Vector3Int>();

        public static void Clear() => BlockedCells.Clear();

        public static void RegisterBlocked(Vector3Int cell) => BlockedCells.Add(cell);

        public static bool IsBlocked(Vector3Int cell) => BlockedCells.Contains(cell);
    }
}
