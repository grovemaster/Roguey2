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
            out DungeonTilePaletteEntry entry) =>
            TryPickEntry(cell, zoneId, paintContext, layerSalt, null, out entry);

        public bool TryPickNonEmitterEntry(
            Vector3Int cell,
            string zoneId,
            ZoneTilePaintContext paintContext,
            int layerSalt,
            out DungeonTilePaletteEntry entry) =>
            TryPickEntry(cell, zoneId, paintContext, layerSalt, e => !e.isLightEmitter, out entry);

        public bool TryPickEntry(
            Vector3Int cell,
            string zoneId,
            ZoneTilePaintContext paintContext,
            int layerSalt,
            System.Func<DungeonTilePaletteEntry, bool> filter,
            out DungeonTilePaletteEntry entry)
        {
            entry = default;
            if (!HasValidEntries)
                return false;

            DungeonTileVariationMode mode = defaultVariationMode;
            if (mode == DungeonTileVariationMode.Single || CountValidEntries(filter) == 1)
            {
                entry = FirstValidEntry(filter);
                return entry.tile != null;
            }

            int hash = DungeonTilePaletteResolver.ComputeCellHash(paintContext, zoneId, cell, layerSalt);
            entry = mode == DungeonTileVariationMode.DeterministicHash
                ? PickUniformEntry(hash, filter)
                : PickWeightedEntry(hash, filter);
            return entry.tile != null;
        }

        int CountValidEntries(System.Func<DungeonTilePaletteEntry, bool> filter = null)
        {
            int count = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile != null && PassesFilter(entries[i], filter))
                    count++;
            }

            return count;
        }

        TileBase FirstValidTile() => FirstValidEntry().tile;

        DungeonTilePaletteEntry FirstValidEntry(System.Func<DungeonTilePaletteEntry, bool> filter = null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile != null && PassesFilter(entries[i], filter))
                    return entries[i];
            }

            return default;
        }

        DungeonTilePaletteEntry PickUniformEntry(int hash, System.Func<DungeonTilePaletteEntry, bool> filter = null)
        {
            int validCount = CountValidEntries(filter);
            if (validCount <= 0)
                return default;

            int pick = Math.Abs(hash) % validCount;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile == null || !PassesFilter(entries[i], filter))
                    continue;

                if (pick == 0)
                    return entries[i];

                pick--;
            }

            return FirstValidEntry(filter);
        }

        DungeonTilePaletteEntry PickWeightedEntry(int hash, System.Func<DungeonTilePaletteEntry, bool> filter = null)
        {
            int totalWeight = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile != null && PassesFilter(entries[i], filter))
                    totalWeight += entries[i].EffectiveWeight;
            }

            if (totalWeight <= 0)
                return default;

            int roll = Math.Abs(hash) % totalWeight;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile == null || !PassesFilter(entries[i], filter))
                    continue;

                roll -= entries[i].EffectiveWeight;
                if (roll < 0)
                    return entries[i];
            }

            return FirstValidEntry(filter);
        }

        static bool PassesFilter(DungeonTilePaletteEntry entry, System.Func<DungeonTilePaletteEntry, bool> filter) =>
            filter == null || filter(entry);

        TileBase PickUniform(int hash) => PickUniformEntry(hash).tile;

        TileBase PickWeighted(int hash) => PickWeightedEntry(hash).tile;
    }
}
