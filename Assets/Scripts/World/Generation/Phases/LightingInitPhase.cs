using JRogue.World.Lighting;

namespace JRogue.World.Generation.Phases
{
    public sealed class LightingInitPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            LightingService lighting = LightingService.Instance != null
                ? LightingService.Instance
                : UnityEngine.Object.FindAnyObjectByType<LightingService>();

            if (lighting == null)
            {
                DungeonGenerationLog.Warn($"{nameof(LightingInitPhase)}: LightingService missing.");
                return;
            }

            lighting.SyncFloorReceiversFromMap();
            lighting.OnPartyVisionActivity();
            DungeonGenerationLog.Phase(nameof(LightingInitPhase), "floor receivers synced");
        }
    }
}
