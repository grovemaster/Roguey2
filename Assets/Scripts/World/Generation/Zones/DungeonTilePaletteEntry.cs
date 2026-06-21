using System;
using JRogue.World.Lighting;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Zones
{
    [Serializable]
    public struct DungeonTilePaletteEntry
    {
        public TileBase tile;
        public string registryKey;
        [Min(1)] public int weight;
        public bool isLightEmitter;
        public LightEmitterDefinition emitLight;
        [Min(0)] public int emissionOverride;

        public int EffectiveWeight => Mathf.Max(1, weight);
    }
}
