using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Manager.Door;
using JRogue.Manager.Map;
using JRogue.Traps;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Rebinds singleton world-feature services to the active dungeon floor and snapshots state on park.
    /// </summary>
    public static class DungeonFloorServiceBinder
    {
        public static void CaptureFeatureState(DungeonFloorInstance instance)
        {
            if (instance == null)
                return;

            DungeonFloorFeatureSnapshot snapshot = instance.FeatureSnapshot;
            snapshot.Clear();

            if (HazardService.Instance != null)
                HazardService.Instance.CaptureSnapshot(snapshot.hazards);

            if (TrapService.Instance != null)
                TrapService.Instance.CaptureSnapshot(snapshot.traps);

            if (InteractableTileService.Instance != null)
                InteractableTileService.Instance.CaptureSnapshot(snapshot.interactables);
        }

        public static void BindActiveFloor(DungeonFloorInstance instance)
        {
            if (instance == null)
                return;

            DungeonFloorTilemaps maps = instance.Tilemaps;

            if (HazardService.Instance != null)
            {
                HazardService.Instance.SetOverlayMap(maps.HazardOverlayMap);
                HazardService.Instance.ClearAllRegistrations();
                HazardService.Instance.RestoreSnapshot(instance.FeatureSnapshot.hazards);
            }

            if (TrapService.Instance != null)
            {
                TrapService.Instance.SetOverlayMap(maps.TrapOverlayMap);
                TrapService.Instance.ClearAllRegistrations();
                TrapService.Instance.RestoreSnapshot(instance.FeatureSnapshot.traps);
            }

            if (InteractableTileService.Instance != null)
            {
                InteractableTileService.Instance.SetOverlayMap(maps.InteractableOverlayMap);
                InteractableTileService.Instance.ClearAllRegistrations();
                InteractableTileService.Instance.RestoreSnapshot(instance.FeatureSnapshot.interactables);
            }

            if (DoorService.Instance != null)
                DoorService.Instance.SetOverlayMap(maps.DoorOverlayMap);

            HazardService.Instance?.RefreshAllOverlayVisuals();
            TrapService.Instance?.RefreshOverlayVisibility();
        }

        public static void ClearSingletonServices()
        {
            HazardService.Instance?.ClearAllRegistrations();
            TrapService.Instance?.ClearAllRegistrations();
            InteractableTileService.Instance?.ClearAllRegistrations();
        }
    }
}
