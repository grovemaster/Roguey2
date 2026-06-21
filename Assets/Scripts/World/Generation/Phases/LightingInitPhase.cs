using JRogue.Manager.Map;
using JRogue.World.Generation;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Zones;
using JRogue.World.Lighting;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class LightingInitPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            LightingService lighting = LightingService.Instance != null
                ? LightingService.Instance
                : Object.FindAnyObjectByType<LightingService>();

            if (lighting == null)
            {
                DungeonGenerationLog.Warn($"{nameof(LightingInitPhase)}: LightingService missing.");
                return;
            }

            lighting.FinalizeRegistry();
            lighting.SyncFloorReceiversFromMap();

            MapManager map = MapManager.Instance;
            if (context.UsesZoneComposite && map != null)
            {
                ZoneCompositeLightingSync.ApplyZoneAmbientRegionDefaults(
                    lighting,
                    context.Definition?.ZoneLayout);

                int zoneAmbientCells = ZoneAmbientApplicator.Apply(context, map, lighting);
                int emitterCells = ZoneTileEmitterApplicator.Apply(context, map, lighting);
                if (zoneAmbientCells > 0 || emitterCells > 0)
                {
                    DungeonGenerationLog.Phase(
                        nameof(LightingInitPhase),
                        $"zoneAmbientCells={zoneAmbientCells} tileEmitters={emitterCells} " +
                        ZoneCompositeLightingSync.DescribeZoneAmbientRegionsForLog(
                            lighting,
                            context.Definition?.ZoneLayout));
                    ZoneCompositeLightingSync.LogLightingDiagnosticsIfEnabled(
                        lighting,
                        map,
                        context,
                        emitterCells);
                }
            }

            if (context.Definition != null && context.Definition.FloorId == TownTorchSetupPhase.TownFloorId)
                TownLightingSync.ApplyForCurrentPhase();
            else if (context.Definition != null && TownPortalSetupPhase.IsTownInterior(context.Definition.FloorId))
                lighting.ApplyFullInteriorDaylight();
            else
                lighting.OnPartyVisionActivity();

            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            visibility?.RefreshPartyVision();

            if (context.Definition != null && context.Definition.FloorId == TownTorchSetupPhase.TownFloorId)
            {
                int ambient = TownLightingSync.AmbientLightForPhase(
                    TownTimeService.Instance?.CurrentPhase ?? TownTimePhase.Day);
                DungeonGenerationLog.Phase(nameof(LightingInitPhase), $"town ambient={ambient}");
            }
            else if (context.UsesZoneComposite && context.Definition?.ZoneLayout != null)
            {
                DungeonGenerationLog.Phase(
                    nameof(LightingInitPhase),
                    $"zone composite lighting synced " +
                    ZoneCompositeLightingSync.DescribeZoneAmbientRegionsForLog(
                        lighting,
                        context.Definition.ZoneLayout));
            }
            else
            {
                DungeonGenerationLog.Phase(
                    nameof(LightingInitPhase),
                    $"floor receivers synced ambient={LightLevel.FullDaylightAmbient}");
            }
        }
    }
}
