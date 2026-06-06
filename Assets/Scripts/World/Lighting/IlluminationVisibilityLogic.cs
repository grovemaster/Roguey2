using JRogue.Actors;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>
    /// Per-cell illumination gating for live visibility (Improved Illumination §5.2).
    /// </summary>
    public static class IlluminationVisibilityLogic
    {
        public static bool IsPartyMemberOccupyingCell(Vector3Int cell)
        {
            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return false;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member != null && member.GridPosition == cell)
                    return true;
            }

            return false;
        }

        public static bool IsCellLiveVisible(
            int emitLight,
            int receivedLight,
            bool partyOccupied,
            bool isWallInGeometricLos = false)
        {
            if (partyOccupied)
                return true;

            if (emitLight > 0)
                return true;

            // Walls in LOS stay visible as silhouettes; they are not floor light receivers.
            if (isWallInGeometricLos)
                return true;

            return receivedLight > 0;
        }

        public static bool IsCellFullyBright(int emitLight, int receivedLight, bool partyOccupied, int threshold)
        {
            if (partyOccupied)
                return true;

            if (emitLight > 0)
                return true;

            return receivedLight >= threshold;
        }
    }
}
