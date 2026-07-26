using UnityEngine;

namespace JRogue.World.Rift
{
    /// <summary>
    /// Pure gate logic for opening rift portals on a host floor.
    /// See Docs/World/Rift-Requirements.md §5.3 / §8.1.
    /// </summary>
    public static class RiftPortalGateLogic
    {
        public static bool PassesPlayerTrigger(
            bool hostHasRifts,
            int dungeonDay,
            int minDungeonDay,
            bool portalAlreadyUsedThisRun,
            int currentRunIndex,
            int lastPortalOpenedRunIndex,
            int minRunsBetweenPortals,
            out string denyReason)
        {
            if (!hostHasRifts)
            {
                denyReason = "No rifts are available on this floor.";
                return false;
            }

            if (dungeonDay < minDungeonDay)
            {
                denyReason = $"Rift portals cannot open before day {minDungeonDay}.";
                return false;
            }

            if (portalAlreadyUsedThisRun)
            {
                denyReason = "A rift portal was already opened on this floor this run.";
                return false;
            }

            if (lastPortalOpenedRunIndex > 0
                && currentRunIndex < lastPortalOpenedRunIndex + minRunsBetweenPortals + 1)
            {
                denyReason = "Too soon since the last rift portal on this floor.";
                return false;
            }

            denyReason = null;
            return true;
        }

        public static bool PassesWandering(
            bool hostHasRifts,
            int dungeonDay,
            int minDungeonDay,
            bool portalAlreadyUsedThisRun,
            int currentRunIndex,
            int lastRiftEnteredRunIndex,
            int minRunsBeforeWandering,
            out string denyReason)
        {
            if (!hostHasRifts)
            {
                denyReason = "No rifts.";
                return false;
            }

            if (dungeonDay < minDungeonDay)
            {
                denyReason = "Day too early.";
                return false;
            }

            if (portalAlreadyUsedThisRun)
            {
                denyReason = "Portal already used this run.";
                return false;
            }

            // After 5 runs without entry, wandering may begin on the 6th (runs since entry >= 5
            // when last entry was never → treat lastRiftEnteredRunIndex == 0 as "never").
            int runsWithoutEntry = lastRiftEnteredRunIndex <= 0
                ? currentRunIndex
                : currentRunIndex - lastRiftEnteredRunIndex;

            if (runsWithoutEntry < minRunsBeforeWandering + 1)
            {
                denyReason = "Wandering not yet eligible.";
                return false;
            }

            denyReason = null;
            return true;
        }

        public static int NextEligibleRunAfterPortal(int lastPortalOpenedRunIndex, int minRunsBetween) =>
            lastPortalOpenedRunIndex <= 0
                ? 1
                : lastPortalOpenedRunIndex + minRunsBetween + 1;
    }
}
