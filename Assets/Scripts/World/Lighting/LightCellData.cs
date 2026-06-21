using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>Per-cell lighting registry entry (emitter and/or receiver).</summary>
    public struct LightCellData
    {
        public bool IsEmitter;
        public bool IsReceiver;
        public LightEmitterDefinition EmitterDefinition;
        public int EmitLight;
        public int AmbientRegionId;
        public bool BlocksLos;

        /// <summary>Last computed received light (emitters + ambient). Receivers only.</summary>
        public int ReceivedLight;

        /// <summary>
        /// Zone composite id when registered from zone lighting passes.
        /// Tile emitters only contribute to receivers in the same zone.
        /// </summary>
        public string ZoneId;

        public static LightCellData Receiver(int ambientRegionId, int defaultAmbient, string zoneId = null)
        {
            return new LightCellData
            {
                IsReceiver = true,
                AmbientRegionId = ambientRegionId,
                ReceivedLight = defaultAmbient,
                ZoneId = zoneId
            };
        }

        public static LightCellData Emitter(
            LightEmitterDefinition definition,
            int initialEmission,
            int ambientRegionId = 0,
            bool alsoReceiver = true,
            string zoneId = null)
        {
            int emission = definition != null
                ? LightLevel.ClampEmission(initialEmission, definition)
                : LightLevel.Clamp(initialEmission);

            return new LightCellData
            {
                IsEmitter = true,
                IsReceiver = alsoReceiver,
                EmitterDefinition = definition,
                EmitLight = emission,
                AmbientRegionId = ambientRegionId,
                BlocksLos = definition != null && definition.BlocksLos,
                ZoneId = zoneId
            };
        }
    }
}
