using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Manager.Door;
using JRogue.Manager.Floor;
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

            if (FloorItemPileService.Instance != null)
                FloorItemPileService.Instance.CaptureSnapshot(snapshot.floorItems);
        }

        public static void BindActiveFloor(
            DungeonFloorInstance instance,
            bool restoreFeaturesFromSnapshot = true)
        {
            if (instance == null)
                return;

            DungeonFloorTilemaps maps = instance.Tilemaps;

            if (HazardService.Instance != null)
            {
                HazardService.Instance.SetOverlayMap(maps.HazardOverlayMap);
                if (restoreFeaturesFromSnapshot)
                {
                    HazardService.Instance.ClearAllRegistrations();
                    HazardService.Instance.RestoreSnapshot(instance.FeatureSnapshot.hazards);
                }
            }

            if (TrapService.Instance != null)
            {
                TrapService.Instance.SetOverlayMap(maps.TrapOverlayMap);
                if (restoreFeaturesFromSnapshot)
                {
                    TrapService.Instance.ClearAllRegistrations();
                    TrapService.Instance.RestoreSnapshot(instance.FeatureSnapshot.traps);
                }
            }

            if (InteractableTileService.Instance != null)
            {
                InteractableTileService.Instance.SetOverlayMap(maps.InteractableOverlayMap);
                if (restoreFeaturesFromSnapshot)
                {
                    InteractableTileService.Instance.ClearAllRegistrations();
                    InteractableTileService.Instance.RestoreSnapshot(instance.FeatureSnapshot.interactables);
                }
            }

            if (FloorItemPileService.Instance != null)
            {
                FloorItemPileService.Instance.BindViewRoot(instance.DynamicViewsRoot);
                if (restoreFeaturesFromSnapshot)
                {
                    FloorItemPileService.Instance.ClearAllPiles();
                    FloorItemPileService.Instance.RestoreSnapshot(instance.FeatureSnapshot.floorItems);
                }
            }

            if (DoorService.Instance != null)
            {
                DoorService.Instance.SetOverlayMap(maps.DoorOverlayMap);
                DoorService.Instance.RefreshAllOverlays();
            }

            HazardService.Instance?.RefreshAllOverlayVisuals();
            TrapService.Instance?.RefreshOverlayVisibility();
            DoorService.Instance?.RefreshOverlayVisibility();
            InteractableTileService.Instance?.RefreshAllOverlayVisuals();
        }

        public static void ClearSingletonServices()
        {
            HazardService.Instance?.ClearAllRegistrations();
            TrapService.Instance?.ClearAllRegistrations();
            InteractableTileService.Instance?.ClearAllRegistrations();
            FloorItemPileService.Instance?.ClearAllPiles();
            DoorService.Instance?.ClearAllRegistrations();
        }
    }
}
