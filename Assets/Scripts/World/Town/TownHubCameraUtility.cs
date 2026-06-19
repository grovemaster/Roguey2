using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Camera orthographic size when entering town hub floors.</summary>
    public static class TownHubCameraUtility
    {
        public static void ApplyForFloor(string floorId)
        {
            if (!TryGetOrthoSize(floorId, out float size))
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            camera.orthographic = true;
            camera.orthographicSize = size;
        }

        public static bool TryGetOrthoSize(string floorId, out float size)
        {
            if (floorId == AdventureGuildExchangeLayout.InteriorFloorId
                || floorId == DimensionSquareFloorIds.FloorId)
            {
                size = AdventureGuildExchangeLayout.DistrictHubCameraOrthoSize;
                return true;
            }

            size = 0f;
            return false;
        }
    }
}
