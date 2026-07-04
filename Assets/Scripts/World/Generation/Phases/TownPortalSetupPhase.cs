using JRogue.World.Town;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// Places the town → dungeon portal at the stamp marker (center plaza).
    /// </summary>
    public sealed class TownPortalSetupPhase : IDungeonGenerationPhase
    {
        public const string TownFloorId = "town_main";

        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null || !HasTownDungeonPortal(def.FloorId))
                return;

            Vector3Int portalCell = ResolvePortalCell(context);
            context.Instance.PlacePortalVisual(portalCell, requiresTownTimeOpen: true);

            var portal = new TownToDungeonPortalInteractable(portalCell);
            context.Instance.RegisterMapInteractable(portal);

            DungeonGenerationLog.Phase(nameof(TownPortalSetupPhase), $"town portal at {portalCell}");
        }

        /// <summary>Outdoor hub districts that participate in town time (not building interiors).</summary>
        public static bool IsHubFloor(string floorId) =>
            floorId == TownFloorId
            || floorId == DimensionSquareFloorIds.FloorId
            || floorId == MarketTownFloorIds.FloorId
            || floorId == ResidentialTownFloorIds.FloorId
            || floorId == HolyLandFloorIds.Nexus
            || floorId == HolyLandFloorIds.HolyLandProper;

        /// <summary>Only dimension_square (and legacy town_main) expose the town → dungeon portal.</summary>
        public static bool HasTownDungeonPortal(string floorId) =>
            floorId == TownFloorId || floorId == DimensionSquareFloorIds.FloorId;

        public static bool IsTownInterior(string floorId) =>
            !string.IsNullOrEmpty(floorId)
            && (floorId.StartsWith("town_interior") || floorId == HolyLandFloorIds.ShamanTentInterior);

        static Vector3Int ResolvePortalCell(DungeonGenerationContext context)
        {
            if (context.Definition?.LayoutMode == FloorLayoutMode.ScenePainted
                && context.Instance != null
                && ScenePaintedMarkerUtility.TryGetCell(
                    context.Instance.transform,
                    StaticHubMarkerKind.DungeonPortal,
                    out Vector3Int markerCell))
            {
                return markerCell;
            }

            DungeonLayoutStamp stamp = context.Definition.LayoutStamp;
            if (stamp != null && stamp.TryGetMarker(StampMarkerIds.TownDungeonPortal, out Vector3Int stampCell))
                return stampCell;

            return new Vector3Int(stamp != null ? stamp.Width / 2 : 10, stamp != null ? stamp.Height / 2 : 10, 0);
        }
    }
}
