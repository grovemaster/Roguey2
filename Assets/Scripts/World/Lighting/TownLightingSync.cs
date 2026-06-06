using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>
    /// Maps <see cref="TownTimePhase"/> to town ambient light and refreshes vision.
    /// </summary>
    public static class TownLightingSync
    {
        public const string LogPrefix = "[TownLighting]";

        public static int AmbientLightForPhase(TownTimePhase phase)
        {
            switch (phase)
            {
                case TownTimePhase.Morning:
                    return 8;
                case TownTimePhase.Day:
                    return LightLevel.FullDaylightAmbient;
                case TownTimePhase.Night:
                    return LightLevel.PitchDark;
                default:
                    return LightLevel.FullDaylightAmbient;
            }
        }

        public static void ApplyForCurrentPhase()
        {
            TownTimeService townTime = TownTimeService.Instance;
            if (townTime == null)
                return;

            ApplyForPhase(townTime.CurrentPhase);
        }

        public static void ApplyForPhase(TownTimePhase phase)
        {
            LightingService lighting = LightingService.Instance;
            if (lighting == null)
                return;

            int regionId = lighting.DefaultFloorAmbientRegionId;
            int level = AmbientLightForPhase(phase);
            lighting.SetAmbientLight(regionId, level, $"town-phase-{phase}");
            Debug.Log($"{LogPrefix} Phase {phase} → ambient {level}.");

            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            visibility?.RefreshPartyVision();
        }
    }
}
