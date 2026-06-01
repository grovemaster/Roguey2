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
            if (def == null || def.FloorId != TownFloorId)
                return;

            Vector3Int portalCell = ResolvePortalCell(context);
            context.Instance.PlacePortalVisual(portalCell);

            var portal = new TownToDungeonPortalInteractable(portalCell);
            context.Instance.RegisterMapInteractable(portal);

            DungeonGenerationLog.Phase(nameof(TownPortalSetupPhase), $"town portal at {portalCell}");
        }

        static Vector3Int ResolvePortalCell(DungeonGenerationContext context)
        {
            DungeonLayoutStamp stamp = context.Definition.LayoutStamp;
            if (stamp != null && stamp.TryGetMarker(StampMarkerIds.TownDungeonPortal, out Vector3Int markerCell))
                return markerCell;

            return new Vector3Int(stamp != null ? stamp.Width / 2 : 10, stamp != null ? stamp.Height / 2 : 10, 0);
        }
    }
}
