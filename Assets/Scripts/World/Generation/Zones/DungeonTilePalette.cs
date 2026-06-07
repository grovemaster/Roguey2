using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Zones
{
    [CreateAssetMenu(fileName = "DungeonTilePalette", menuName = "JRogue/World/Dungeon Tile Palette")]
    public sealed class DungeonTilePalette : ScriptableObject
    {
        [SerializeField] string paletteId;
        [SerializeField] DungeonTilePaletteLayer layer = DungeonTilePaletteLayer.Floor;
        [SerializeField] DungeonTileVariationMode defaultVariationMode = DungeonTileVariationMode.WeightedRandom;
        [SerializeField] DungeonTilePaletteEntry[] entries = Array.Empty<DungeonTilePaletteEntry>();

        public string PaletteId => paletteId;
        public DungeonTilePaletteLayer Layer => layer;
        public DungeonTileVariationMode DefaultVariationMode => defaultVariationMode;
        public DungeonTilePaletteEntry[] Entries => entries;

        public bool HasValidEntries
        {
            get
            {
                if (entries == null || entries.Length == 0)
                    return false;

                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].tile != null)
                        return true;
                }

                return false;
            }
        }

        public TileBase PickTile(Vector3Int cell, string zoneId, ZoneTilePaintContext paintContext, int layerSalt)
        {
            if (!HasValidEntries)
                return null;

            DungeonTileVariationMode mode = defaultVariationMode;
            if (mode == DungeonTileVariationMode.Single || CountValidEntries() == 1)
                return FirstValidTile();

            int hash = DungeonTilePaletteResolver.ComputeCellHash(paintContext, zoneId, cell, layerSalt);
            return mode == DungeonTileVariationMode.DeterministicHash
                ? PickUniform(hash)
                : PickWeighted(hash);
        }

        int CountValidEntries()
        {
            int count = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile != null)
                    count++;
            }

            return count;
        }

        TileBase FirstValidTile()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile != null)
                    return entries[i].tile;
            }

            return null;
        }

        TileBase PickUniform(int hash)
        {
            int validCount = CountValidEntries();
            if (validCount <= 0)
                return null;

            int pick = Math.Abs(hash) % validCount;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile == null)
                    continue;

                if (pick == 0)
                    return entries[i].tile;

                pick--;
            }

            return FirstValidTile();
        }

        TileBase PickWeighted(int hash)
        {
            int totalWeight = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile != null)
                    totalWeight += entries[i].EffectiveWeight;
            }

            if (totalWeight <= 0)
                return null;

            int roll = Math.Abs(hash) % totalWeight;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile == null)
                    continue;

                roll -= entries[i].EffectiveWeight;
                if (roll < 0)
                    return entries[i].tile;
            }

            return FirstValidTile();
        }
    }
}
