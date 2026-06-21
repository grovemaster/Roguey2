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

        public TileBase PickTile(Vector3Int cell, string zoneId, ZoneTilePaintContext paintContext, int layerSalt) =>
            TryPickEntry(cell, zoneId, paintContext, layerSalt, out DungeonTilePaletteEntry entry)
                ? entry.tile
                : null;

        public bool TryPickEntry(
            Vector3Int cell,
            string zoneId,
            ZoneTilePaintContext paintContext,
            int layerSalt,
            out DungeonTilePaletteEntry entry)
        {
            entry = default;
            if (!HasValidEntries)
                return false;

            DungeonTileVariationMode mode = defaultVariationMode;
            if (mode == DungeonTileVariationMode.Single || CountValidEntries() == 1)
            {
                entry = FirstValidEntry();
                return entry.tile != null;
            }

            int hash = DungeonTilePaletteResolver.ComputeCellHash(paintContext, zoneId, cell, layerSalt);
            entry = mode == DungeonTileVariationMode.DeterministicHash
                ? PickUniformEntry(hash)
                : PickWeightedEntry(hash);
            return entry.tile != null;
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

        TileBase FirstValidTile() => FirstValidEntry().tile;

        DungeonTilePaletteEntry FirstValidEntry()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile != null)
                    return entries[i];
            }

            return default;
        }

        DungeonTilePaletteEntry PickUniformEntry(int hash)
        {
            int validCount = CountValidEntries();
            if (validCount <= 0)
                return default;

            int pick = Math.Abs(hash) % validCount;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile == null)
                    continue;

                if (pick == 0)
                    return entries[i];

                pick--;
            }

            return FirstValidEntry();
        }

        DungeonTilePaletteEntry PickWeightedEntry(int hash)
        {
            int totalWeight = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile != null)
                    totalWeight += entries[i].EffectiveWeight;
            }

            if (totalWeight <= 0)
                return default;

            int roll = Math.Abs(hash) % totalWeight;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile == null)
                    continue;

                roll -= entries[i].EffectiveWeight;
                if (roll < 0)
                    return entries[i];
            }

            return FirstValidEntry();
        }

        TileBase PickUniform(int hash) => PickUniformEntry(hash).tile;

        TileBase PickWeighted(int hash) => PickWeightedEntry(hash).tile;
    }
}
