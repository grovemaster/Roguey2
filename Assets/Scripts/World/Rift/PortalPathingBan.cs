using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Rift
{
    /// <summary>Tracks portal cells so enemies never path onto them (G8).</summary>
    public static class PortalPathingBan
    {
        static readonly HashSet<Vector3Int> Cells = new HashSet<Vector3Int>();

        public static void RegisterPortalCell(Vector3Int cell)
        {
            cell.z = 0;
            Cells.Add(cell);
        }

        public static void UnregisterPortalCell(Vector3Int cell)
        {
            cell.z = 0;
            Cells.Remove(cell);
        }

        public static void Clear() => Cells.Clear();

        public static bool IsPortalCell(Vector3Int cell)
        {
            cell.z = 0;
            return Cells.Contains(cell);
        }

        public static bool IsEnemyPortalAvoidCell(Vector3Int cell) => IsPortalCell(cell);
    }
}
