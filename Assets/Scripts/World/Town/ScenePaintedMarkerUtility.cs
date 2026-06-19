using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Town
{
    public static class ScenePaintedMarkerUtility
    {
        public static bool TryGetCell(
            Transform floorRoot,
            StaticHubMarkerKind kind,
            out Vector3Int cell)
        {
            cell = default;
            if (floorRoot == null)
                return false;

            StaticHubMarker[] markers = floorRoot.GetComponentsInChildren<StaticHubMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                StaticHubMarker marker = markers[i];
                if (marker != null && marker.Kind == kind)
                {
                    cell = marker.Cell;
                    return true;
                }
            }

            return false;
        }

        public static IReadOnlyList<StaticHubMarker> GetMarkers(Transform floorRoot)
        {
            if (floorRoot == null)
                return System.Array.Empty<StaticHubMarker>();

            return floorRoot.GetComponentsInChildren<StaticHubMarker>(true);
        }
    }
}
