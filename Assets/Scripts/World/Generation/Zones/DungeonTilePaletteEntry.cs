using System;
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

        public int EffectiveWeight => Mathf.Max(1, weight);
    }
}
