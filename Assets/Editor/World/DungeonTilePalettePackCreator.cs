#if UNITY_EDITOR
using System.IO;
using JRogue.World.Generation.Zones;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    public static class DungeonTilePalettePackCreator
    {
        const string PaletteRoot = "Assets/Data/Dungeon/TilePalettes";
        const string TileFolder = "Assets/TileMaps";
        const string VaultTileFolder = "Assets/TileMaps/Vault";

        const string SandThemePath = "Assets/Sprites/Environment/SandTheme.png";
        const string SnowThemePath = "Assets/Sprites/Environment/SnowTheme.png";

        /// <summary>
        /// Vault theme keys (Theme:N) are logical ids; N often matches the sprite index but
        /// SandTheme/SnowTheme sheets skip indices 40–46 — map those keys explicitly.
        /// </summary>
        static readonly (string key, string texturePath, string spriteName)[] ThemeTileSpecs =
        {
            ("SandTheme:40", SandThemePath, "Scavengers2_SpriteSheet_33"),
            ("SandTheme:41", SandThemePath, "Scavengers2_SpriteSheet_34"),
            ("SandTheme:51", SandThemePath, "Scavengers2_SpriteSheet_51"),
            ("SnowTheme:33", SnowThemePath, "Scavengers2_SpriteSheet_33"),
            ("SnowTheme:34", SnowThemePath, "Scavengers2_SpriteSheet_34"),
            ("SnowTheme:40", SnowThemePath, "Scavengers2_SpriteSheet_35"),
            ("SnowTheme:41", SnowThemePath, "Scavengers2_SpriteSheet_36"),
            ("SnowTheme:42", SnowThemePath, "Scavengers2_SpriteSheet_37"),
            ("SnowTheme:49", SnowThemePath, "Scavengers2_SpriteSheet_49"),
        };

        [MenuItem("JRogue/Dungeon/Create Tile Palettes")]
        public static void CreateTilePalettes()
        {
            EnsureFolder(PaletteRoot);
            EnsureVaultThemeTiles();

            CreateOrUpdatePalette(
                $"{PaletteRoot}/Palette_Dungeon_Floor.asset",
                "dungeon_floor",
                DungeonTilePaletteLayer.Floor,
                new (string tilePath, int weight)[]
                {
                    ("Assets/TileMaps/Scavengers_SpriteSheet_32.asset", 5),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_33.asset", 5),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_34.asset", 4),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_36.asset", 3),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_37.asset", 3),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_38.asset", 2),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_39.asset", 2),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_35.asset", 1),
                });

            CreateOrUpdatePalette(
                $"{PaletteRoot}/Palette_Dungeon_Wall.asset",
                "dungeon_wall",
                DungeonTilePaletteLayer.Wall,
                new (string tilePath, int weight)[]
                {
                    ("Assets/TileMaps/Scavengers_SpriteSheet_48.asset", 4),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_49.asset", 4),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_50.asset", 5),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_51.asset", 3),
                    ("Assets/TileMaps/Scavengers_SpriteSheet_52.asset", 2),
                });

            CreateOrUpdatePalette(
                $"{PaletteRoot}/Palette_Sand_Floor.asset",
                "sand_floor",
                DungeonTilePaletteLayer.Floor,
                new (string tilePath, int weight)[]
                {
                    ($"{VaultTileFolder}/SandTheme_32.asset", 5),
                    ($"{VaultTileFolder}/SandTheme_40.asset", 4),
                    ($"{VaultTileFolder}/SandTheme_41.asset", 3),
                });

            CreateOrUpdatePalette(
                $"{PaletteRoot}/Palette_Sand_Wall.asset",
                "sand_wall",
                DungeonTilePaletteLayer.Wall,
                new (string tilePath, int weight)[]
                {
                    ($"{VaultTileFolder}/SandTheme_50.asset", 5),
                    ($"{VaultTileFolder}/SandTheme_51.asset", 3),
                });

            CreateOrUpdatePalette(
                $"{PaletteRoot}/Palette_Snow_Floor.asset",
                "snow_floor",
                DungeonTilePaletteLayer.Floor,
                new (string tilePath, int weight)[]
                {
                    ($"{VaultTileFolder}/SnowTheme_32.asset", 5),
                    ($"{VaultTileFolder}/SnowTheme_33.asset", 5),
                    ($"{VaultTileFolder}/SnowTheme_34.asset", 4),
                    ($"{VaultTileFolder}/SnowTheme_40.asset", 3),
                    ($"{VaultTileFolder}/SnowTheme_41.asset", 2),
                    ($"{VaultTileFolder}/SnowTheme_42.asset", 1),
                });

            CreateOrUpdatePalette(
                $"{PaletteRoot}/Palette_Snow_Wall.asset",
                "snow_wall",
                DungeonTilePaletteLayer.Wall,
                new (string tilePath, int weight)[]
                {
                    ($"{VaultTileFolder}/SnowTheme_48.asset", 5),
                    ($"{VaultTileFolder}/SnowTheme_49.asset", 3),
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Dungeon] Tile palettes created under {PaletteRoot}.");
        }

        public static DungeonTilePalette LoadPalette(string assetPath) =>
            AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(assetPath);

        static void EnsureVaultThemeTiles()
        {
            for (int i = 0; i < ThemeTileSpecs.Length; i++)
            {
                (string key, string texturePath, string spriteName) = ThemeTileSpecs[i];
                EnsureVaultTile(key, texturePath, spriteName);
            }
        }

        static void CreateOrUpdatePalette(
            string path,
            string paletteId,
            DungeonTilePaletteLayer layer,
            (string tilePath, int weight)[] tiles)
        {
            var palette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(path);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<DungeonTilePalette>();
                AssetDatabase.CreateAsset(palette, path);
            }

            SerializedObject so = new SerializedObject(palette);
            so.FindProperty("paletteId").stringValue = paletteId;
            so.FindProperty("layer").enumValueIndex = (int)layer;
            so.FindProperty("defaultVariationMode").enumValueIndex =
                (int)DungeonTileVariationMode.WeightedRandom;

            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = tiles.Length;
            for (int i = 0; i < tiles.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("tile").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<TileBase>(tiles[i].tilePath);
                entry.FindPropertyRelative("registryKey").stringValue = string.Empty;
                entry.FindPropertyRelative("weight").intValue = tiles[i].weight;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(palette);
        }

        static void EnsureVaultTile(string key, string texturePath, string spriteName)
        {
            string safeName = key.Replace(":", "_");
            string assetPath = $"{VaultTileFolder}/{safeName}.asset";
            if (AssetDatabase.LoadAssetAtPath<Tile>(assetPath) != null)
                return;

            Sprite sprite = FindSprite(texturePath, spriteName);
            if (sprite == null)
            {
                Debug.LogWarning($"[Dungeon] Could not find sprite '{spriteName}' on '{texturePath}'.");
                return;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, assetPath);
        }

        static Sprite FindSprite(string texturePath, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            }

            return null;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
