using JRogue.World.Generation;
using JRogue.World.Generation.Phases;
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

            if (context.Definition != null && context.Definition.FloorId == TownTorchSetupPhase.TownFloorId)
                TownLightingSync.ApplyForCurrentPhase();
            else
                lighting.OnPartyVisionActivity();

            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            visibility?.RefreshPartyVision();

            int ambient = context.Definition != null && context.Definition.FloorId == TownTorchSetupPhase.TownFloorId
                ? TownLightingSync.AmbientLightForPhase(TownTimeService.Instance?.CurrentPhase ?? TownTimePhase.Day)
                : LightLevel.FullDaylightAmbient;
            DungeonGenerationLog.Phase(nameof(LightingInitPhase), $"floor receivers synced ambient={ambient}");
        }
    }
}
