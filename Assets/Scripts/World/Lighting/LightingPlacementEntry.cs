using System;
using UnityEngine;

namespace JRogue.World.Lighting
{
    [Serializable]
    public struct LightingPlacementEntry
    {
        public Vector3Int cell;

        [Tooltip("When true, registers an emitter (optional definition / initial emission).")]
        public bool isEmitter;

        public LightEmitterDefinition emitterDefinition;

        [Range(LightLevel.Min, LightLevel.Max)]
        public int initialEmission;

        [Tooltip("When true, cell receives propagated light (default for floor tiles).")]
        public bool isReceiver;

        [Min(0)]
        public int ambientRegionId;
    }
}
