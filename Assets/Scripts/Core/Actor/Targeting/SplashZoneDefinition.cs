using UnityEngine;

namespace JRogue.Core.Targeting
{
    [CreateAssetMenu(fileName = "SplashZone", menuName = "JRogue/Targeting/Splash Zone Definition")]
    public sealed class SplashZoneDefinition : ScriptableObject
    {
        public SplashZoneShapeKind shapeKind = SplashZoneShapeKind.None;

        [Min(0)]
        public int radius = 2;

        [Min(1)]
        public int maxLength = 5;

        [Tooltip("When true, primary is included in damage cells but still excluded from red preview.")]
        public bool includePrimaryInEffect = true;

        public SplashZoneDistanceMetric distanceMetric = SplashZoneDistanceMetric.Chebyshev;
    }
}
