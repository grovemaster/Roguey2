using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Scene-painted floors are authored in the open hub scene rather than generated. Activating one the
    /// scene does not contain silently produces an empty map, so transitions into it are refused instead.
    /// </summary>
    public static class ScenePaintedFloorGuard
    {
        /// <summary>Grep tag for the console error raised when a scene-painted floor is missing.</summary>
        public const string LogTag = "[ScenePaintedFloorMissing]";

        static readonly HashSet<string> ReportedFloorIds = new HashSet<string>();

        /// <summary>Called on manager wake so each play session reports the problem again.</summary>
        public static void ResetReportedFloors() => ReportedFloorIds.Clear();

        public static bool IsAuthoredInOpenScene(string floorId)
        {
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            return manager == null || manager.IsScenePaintedFloorAuthored(floorId);
        }

        /// <summary>Returns true (and logs) when the transition must be refused.</summary>
        public static bool DenyTransition(string floorId, string portalLinkId)
        {
            if (IsAuthoredInOpenScene(floorId))
                return false;

            LogMissing(floorId, $"Refused transition (portal '{portalLinkId ?? "none"}') to");
            return true;
        }

        /// <summary>Reports once per floor per session — the edge is bumped repeatedly while walking.</summary>
        public static void LogMissing(string floorId, string action)
        {
            if (!ReportedFloorIds.Add(floorId ?? string.Empty))
                return;

            Debug.LogError(
                $"{LogTag} {action} scene-painted floor '{floorId}' — the open scene does not author it, so the " +
                "party would land on an empty map with no player-start marker. Open the hub scene that contains " +
                "this floor and play from there (JRogue → Town → Fix District Town Test Scene rebuilds " +
                "DistrictTownTest, which authors the residential district and inn).");
        }
    }
}
