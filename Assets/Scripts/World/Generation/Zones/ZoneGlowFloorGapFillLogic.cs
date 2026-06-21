using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneGlowFloorGapFillLogic
    {
        public static bool NeedsGlowFill(int receivedLight, int minReceivedLight) =>
            receivedLight < minReceivedLight;

        public static bool IsWithinSpacing(Vector3Int cell, IReadOnlyList<Vector3Int> placed, int minSpacingChebyshev)
        {
            if (placed == null || placed.Count == 0 || minSpacingChebyshev <= 1)
                return false;

            for (int i = 0; i < placed.Count; i++)
            {
                if (ChebyshevDistance(cell, placed[i]) < minSpacingChebyshev)
                    return true;
            }

            return false;
        }

        public static int ChebyshevDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
}
