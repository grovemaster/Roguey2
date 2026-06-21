using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>
    /// Limits how far party LOS reveals pitch-dark cells (Improved Illumination §5.2 extension).
    /// Peering from lit areas shows at most one tile into darkness; standing in pitch dark
    /// without a personal light source reveals only occupied tiles.
    /// </summary>
    public static class DarknessVisibilityLogic
    {
        public static bool IsPitchDarkCell(int emitLight, int receivedLight) =>
            emitLight <= 0 && receivedLight <= 0;

        /// <summary>
        /// True when the member has no personal light and stands in a zone that requires one.
        /// </summary>
        public static bool MemberNavigatesBlind(bool zoneRequiresPersonalLight, bool hasPersonalVisionLight) =>
            !hasPersonalVisionLight && zoneRequiresPersonalLight;

        public static bool IsAdjacentToAny(IReadOnlyCollection<Vector3Int> cells, Vector3Int candidate)
        {
            if (cells == null)
                return false;

            foreach (Vector3Int cell in cells)
            {
                if (IsAdjacent(cell, candidate))
                    return true;
            }

            return false;
        }

        public static bool IsAdjacent(Vector3Int a, Vector3Int b) =>
            Mathf.Abs(a.x - b.x) <= 1 && Mathf.Abs(a.y - b.y) <= 1 && !(a.x == b.x && a.y == b.y);

        public static void CollectDarknessEdgeCells(
            IReadOnlyList<Vector3Int> geometricLos,
            HashSet<Vector3Int> litCore,
            System.Func<Vector3Int, bool> isPitchDarkForVision,
            HashSet<Vector3Int> darknessEdge)
        {
            if (geometricLos == null || litCore == null || darknessEdge == null || isPitchDarkForVision == null)
                return;

            for (int i = 0; i < geometricLos.Count; i++)
            {
                Vector3Int cell = geometricLos[i];
                if (litCore.Contains(cell))
                    continue;

                if (!isPitchDarkForVision(cell))
                    continue;

                if (!IsAdjacentToAny(litCore, cell))
                    continue;

                darknessEdge.Add(cell);
            }
        }

        /// <summary>
        /// Applies darkness clamping to one member's geometric LOS contribution.
        /// </summary>
        public static void ApplyMemberVisibility(
            IReadOnlyList<Vector3Int> geometricLos,
            Vector3Int origin,
            bool blindInPitchDark,
            System.Func<Vector3Int, bool> isLiveVisible,
            System.Func<Vector3Int, bool> isFullyBright,
            System.Func<Vector3Int, bool> isPitchDarkForVision,
            HashSet<Vector3Int> visible,
            HashSet<Vector3Int> litVisible)
        {
            if (geometricLos == null || visible == null || litVisible == null)
                return;

            if (blindInPitchDark)
            {
                visible.Add(origin);
                litVisible.Add(origin);
                return;
            }

            var litCore = new HashSet<Vector3Int>();
            for (int i = 0; i < geometricLos.Count; i++)
            {
                Vector3Int cell = geometricLos[i];
                if (!isLiveVisible(cell))
                    continue;

                litCore.Add(cell);
                visible.Add(cell);
                if (isFullyBright(cell))
                    litVisible.Add(cell);
            }

            var darknessEdge = new HashSet<Vector3Int>();
            CollectDarknessEdgeCells(geometricLos, litCore, isPitchDarkForVision, darknessEdge);
            foreach (Vector3Int cell in darknessEdge)
                visible.Add(cell);
        }
    }
}
