using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class DungeonTilePaletteResolverTests
    {
        readonly System.Collections.Generic.List<Object> _assets = new System.Collections.Generic.List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _assets.Count; i++)
            {
                if (_assets[i] != null)
                    Object.DestroyImmediate(_assets[i]);
            }

            _assets.Clear();
        }

        [Test]
        public void WeightedPick_IsStableForSameSeedAndCell()
        {
            DungeonTilePalette palette = CreatePalette(
                ("a", 5),
                ("b", 1),
                ("c", 3));

            var context = new ZoneTilePaintContext(100001, "dungeon_floor_01", "dungeon_floor_01".GetHashCode());
            var cell = new Vector3Int(4, 7, 0);

            TileBase first = palette.PickTile(cell, "snow", context, DungeonTilePaletteResolver.FloorLayerSalt);
            TileBase second = palette.PickTile(cell, "snow", context, DungeonTilePaletteResolver.FloorLayerSalt);

            Assert.AreSame(first, second);
        }

        [Test]
        public void WeightedPick_UsesDifferentTilesAcrossCells()
        {
            DungeonTilePalette palette = CreatePalette(
                ("a", 1),
                ("b", 1),
                ("c", 1));

            var context = new ZoneTilePaintContext(4242, "dungeon_floor_01", "dungeon_floor_01".GetHashCode());
            var seen = new System.Collections.Generic.HashSet<TileBase>();

            for (int x = 0; x < 12; x++)
            {
                TileBase tile = palette.PickTile(
                    new Vector3Int(x, 3, 0),
                    "desert",
                    context,
                    DungeonTilePaletteResolver.FloorLayerSalt);
                if (tile != null)
                    seen.Add(tile);
            }

            Assert.GreaterOrEqual(seen.Count, 2);
        }

        [Test]
        public void ResolveFloorTile_FallsBackToLegacyZoneTile()
        {
            var floorDef = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
            _assets.Add(floorDef);

            var zoneDef = ScriptableObject.CreateInstance<DungeonZoneDefinition>();
            _assets.Add(zoneDef);

            TileBase legacy = ScriptableObject.CreateInstance<Tile>();
            _assets.Add(legacy);

            SetPrivateField(zoneDef, "floorTile", legacy);

            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            _assets.Add(layout);
            SetPrivateField(layout, "zoneDefinitions", new[] { zoneDef });
            SetPrivateField(zoneDef, "zoneId", "dungeon");

            var context = new ZoneTilePaintContext(1, "floor", "floor".GetHashCode());
            TileBase resolved = DungeonTilePaletteResolver.ResolveFloorTile(
                new Vector3Int(2, 2, 0),
                layout,
                floorDef,
                "dungeon",
                context);

            Assert.AreSame(legacy, resolved);
        }

        DungeonTilePalette CreatePalette(params (string label, int weight)[] entries)
        {
            var palette = ScriptableObject.CreateInstance<DungeonTilePalette>();
            _assets.Add(palette);

            var paletteEntries = new DungeonTilePaletteEntry[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                var tile = ScriptableObject.CreateInstance<Tile>();
                _assets.Add(tile);
                tile.name = entries[i].label;
                paletteEntries[i] = new DungeonTilePaletteEntry
                {
                    tile = tile,
                    weight = entries[i].weight,
                };
            }

            SetPrivateField(palette, "entries", paletteEntries);
            SetPrivateField(palette, "defaultVariationMode", DungeonTileVariationMode.WeightedRandom);
            return palette;
        }

        static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
