using JRogue.Manager.Map;
using JRogue.World.Town;
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

            TownBuildingFacadeOverlay overlay = LoadFacadeOverlay(def.FloorId);
            if (overlay == null || overlay.Cells == null || overlay.Cells.Length == 0)
                return;

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                DungeonGenerationLog.Error($"{nameof(TownBuildingFacadeVisualPhase)}: MapManager.Instance is null.");
                return;
            }

            if (def.FloorId == TownPortalSetupPhase.TownFloorId
                || def.FloorId == DimensionSquareFloorIds.FloorId
                || def.FloorId == MarketTownFloorIds.FloorId)
            {
                TownBuildingMassService.Clear();
                TownBuildingFacadeSight.Clear();
            }

            if (def.FloorId == AdventureGuildExchangeLayout.InteriorFloorId)
                ShopCounterService.Clear();

            if (def.FloorId == MarketGeneralStoreLayout.InteriorFloorId)
                ShopCounterService.Clear();

            if (def.FloorId == MarketItemShopLayout.InteriorFloorId)
                ShopCounterService.Clear();

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

        static TownBuildingFacadeOverlay LoadFacadeOverlay(string floorId)
        {
            string[] resourcePaths =
            {
                $"{OverlayResourceFolder}{floorId}",
                $"Town/DistrictTest/TownArea/DimensionSquare/FacadeOverlay_{floorId}",
                $"Town/DistrictTest/TownArea/Market/FacadeOverlay_{floorId}",
                $"Town/DistrictTest/Building/AdventureGuildExchange/FacadeOverlay_{floorId}",
                $"Town/DistrictTest/Building/MarketGeneralStore/FacadeOverlay_{floorId}",
                $"Town/DistrictTest/Building/MarketItemShop/FacadeOverlay_{floorId}",
            };

            for (int i = 0; i < resourcePaths.Length; i++)
            {
                TownBuildingFacadeOverlay overlay =
                    Resources.Load<TownBuildingFacadeOverlay>(resourcePaths[i]);
                if (overlay != null)
                    return overlay;
            }

            return null;
        }
    }
}
