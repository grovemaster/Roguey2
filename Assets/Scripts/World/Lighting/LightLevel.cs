namespace JRogue.World.Lighting
{
    /// <summary>
    /// Project-wide light intensity scale (locked v0).
    /// See <c>Docs/World/Lighting-Requirements.md</c> §6.1 and §16.
    /// </summary>
    public static class LightLevel
    {
        public const int Min = 0;
        public const int Max = 10;

        /// <summary>Pitch dark — used for gameplay visibility thresholds (Phase C).</summary>
        public const int PitchDark = 0;

        /// <summary>Typical lit wall torch at full brightness (authoring default).</summary>
        public const int TorchEmission = 6;

        /// <summary>Typical permanent fungus / glow wall (authoring default).</summary>
        public const int LuminescentWallEmission = 4;

        /// <summary>Full overhead daylight ambient (authoring default).</summary>
        public const int FullDaylightAmbient = 10;

        public static int Clamp(int value) => UnityEngine.Mathf.Clamp(value, Min, Max);

        public static int ClampEmission(int value, LightEmitterDefinition definition)
        {
            int max = definition != null ? definition.BaseEmissionMax : Max;
            return UnityEngine.Mathf.Clamp(value, Min, max);
        }
    }
}
