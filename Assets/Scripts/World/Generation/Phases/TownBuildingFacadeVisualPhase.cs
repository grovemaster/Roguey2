using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// Overpaints stamped walls/floors with building-specific Kenney facade tiles.</summary>
    public sealed class TownBuildingFacadeVisualPhase : IDungeonGenerationPhase
    {
        const string OverlayResourceFolder = "Town/FacadeOverlay_";

        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context?.Definition;
            if (def == null || string.IsNullOrEmpty(def.FloorId))
                return;

            TownBuildingFacadeOverlay overlay =
                Resources.Load<TownBuildingFacadeOverlay>($"{OverlayResourceFolder}{def.FloorId}");
            if (overlay == null || overlay.Cells == null || overlay.Cells.Length == 0)
                return;

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                DungeonGenerationLog.Error($"{nameof(TownBuildingFacadeVisualPhase)}: MapManager.Instance is null.");
                return;
            }

            if (def.FloorId == TownPortalSetupPhase.TownFloorId)
            {
                TownBuildingMassService.Clear();
                TownBuildingFacadeSight.Clear();
            }

            int painted = 0;
            TownFacadePaintCell[] cells = overlay.Cells;
            for (int i = 0; i < cells.Length; i++)
            {
                TownFacadePaintCell entry = cells[i];
                if (entry.tile == null)
                    continue;

                if (TownPortalSetupPhase.IsHubFloor(def.FloorId))
                    TownBuildingFacadeSight.RegisterCell(entry.cell);

                if (entry.layer == TownFacadePaintLayer.Floor)
                    map.SetCellFloor(entry.cell, entry.tile);
                else if (entry.layer == TownFacadePaintLayer.InteriorMass)
                {
                    map.SetCellFloor(entry.cell, entry.tile);
                    TownBuildingMassService.RegisterBlocked(entry.cell);
                }
                else
                    map.SetCellWall(entry.cell, entry.tile);

                painted++;
            }

            DungeonGenerationLog.Phase(nameof(TownBuildingFacadeVisualPhase), $"painted={painted} floorId={def.FloorId}");
        }
    }
}
