#if UNITY_EDITOR
using System.Collections.Generic;
using JRogue.GridFeatures;
using JRogue.World.Town;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Replaces painted floor cells in the open scene with DCSS rect_gray stone variants.
    /// </summary>
    public static class DcssRectGrayFloorVarietyEditor
    {
        const string MenuPath = "JRogue/Town/Randomize Stone Floor Tiles (Current Scene)";
        const string ConfigureMenuPath = "JRogue/Town/Configure DCSS Rect Gray Floor Tiles";
        const string FixAlignmentMenuPath = "JRogue/Town/Fix Painted Tile Alignment (Current Scene)";

        const string SpriteFolder =
            "Assets/Art/NPC/StyleComparison/_temp/crawl-tiles Oct-5-2010/dc-dngn/floor";

        const string TileAssetFolder = "Assets/TileMaps/Town/Dcss";

        static readonly string[] SpriteFiles =
        {
            "rect_gray0.png",
            "rect_gray1.png",
            "rect_gray2.png",
            "rect_gray3.png",
        };

        static readonly string[] TileAssetNames =
        {
            "Dcss_Floor_RectGray0.asset",
            "Dcss_Floor_RectGray1.asset",
            "Dcss_Floor_RectGray2.asset",
            "Dcss_Floor_RectGray3.asset",
        };

        [MenuItem(ConfigureMenuPath)]
        public static void ConfigureRectGrayFloorTiles()
        {
            int configured = 0;
            for (int i = 0; i < SpriteFiles.Length; i++)
            {
                if (ConfigureFloorSprite($"{SpriteFolder}/{SpriteFiles[i]}"))
                    configured++;
            }

            EnsureTileAssets();
            AssetDatabase.SaveAssets();
            Debug.Log($"[DcssFloor] Configured {configured} rect_gray sprite(s) and tile assets under {TileAssetFolder}.");
        }

        [MenuItem(FixAlignmentMenuPath)]
        public static void FixPaintedTileAlignmentInCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[DcssFloor] Open a scene before fixing tile alignment.");
                return;
            }

            if (Application.isPlaying)
            {
                Debug.LogError("[DcssFloor] Exit Play mode before fixing tile alignment.");
                return;
            }

            List<Tilemap> targets = CollectAlignmentTilemaps(scene);
            if (targets.Count == 0)
            {
                Debug.LogWarning("[DcssFloor] No Floor or Wall tilemaps found in the open scene.");
                return;
            }

            int cellCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                Tilemap tilemap = targets[i];
                Undo.RecordObject(tilemap, "Fix painted tile alignment");
                int before = CountPaintedCells(tilemap);
                GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(tilemap);
                if (before > 0)
                {
                    cellCount += before;
                    EditorUtility.SetDirty(tilemap);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(
                $"[DcssFloor] Applied center-pivot alignment to {cellCount} cell(s) across {targets.Count} tilemap(s) in '{scene.name}'.");
        }

        [MenuItem(FixAlignmentMenuPath, true)]
        static bool ValidateFixAlignmentMenu() =>
            !Application.isPlaying && SceneManager.GetActiveScene().isLoaded;

        [MenuItem(MenuPath)]
        public static void RandomizeStoneFloorTilesInCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[DcssFloor] Open a scene before randomizing floor tiles.");
                return;
            }

            if (Application.isPlaying)
            {
                Debug.LogError("[DcssFloor] Exit Play mode before randomizing floor tiles.");
                return;
            }

            for (int i = 0; i < SpriteFiles.Length; i++)
                ConfigureFloorSprite($"{SpriteFolder}/{SpriteFiles[i]}");

            TileBase[] tiles = EnsureTileAssets();
            if (tiles == null || tiles.Length == 0)
            {
                Debug.LogError("[DcssFloor] Missing rect_gray tile assets. Run Configure DCSS Rect Gray Floor Tiles first.");
                return;
            }

            List<Tilemap> targets = CollectTargetTilemaps(scene);
            if (targets.Count == 0)
            {
                Debug.LogWarning(
                    "[DcssFloor] No floor tilemaps found. Paint a tilemap named 'Floor', "
                    + "or add DcssFloorVarietyInclude to a tilemap you want randomized. "
                    + "Use DcssFloorVarietyExclude on interiors to skip them.");
                return;
            }

            int sceneSalt = scene.path.GetHashCode();
            int tilemapCount = 0;
            int cellCount = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                Tilemap tilemap = targets[i];
                Undo.RecordObject(tilemap, "Randomize stone floor tiles");

                BoundsInt bounds = tilemap.cellBounds;
                int replaced = 0;
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, bounds.zMin);
                        if (!tilemap.HasTile(cell))
                            continue;

                        tilemap.SetTile(cell, PickTile(cell, sceneSalt, tiles));
                        tilemap.SetTransformMatrix(cell, GridOverlayPainter.GetPaintMatrix(tilemap, null, fillScale: 1f));
                        replaced++;
                    }
                }

                if (replaced > 0)
                {
                    tilemapCount++;
                    cellCount += replaced;
                    EditorUtility.SetDirty(tilemap);
                }
            }

            if (cellCount == 0)
            {
                Debug.LogWarning("[DcssFloor] Target tilemaps had no painted floor cells.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(
                $"[DcssFloor] Randomized {cellCount} floor cell(s) across {tilemapCount} tilemap(s) "
                + $"in '{scene.name}'. Skipped tilemaps marked with {nameof(DcssFloorVarietyExclude)}.");
        }

        [MenuItem(MenuPath, true)]
        static bool ValidateRandomizeMenu() =>
            !Application.isPlaying && SceneManager.GetActiveScene().isLoaded;

        static List<Tilemap> CollectTargetTilemaps(Scene scene)
        {
            var results = new List<Tilemap>();
            var seen = new HashSet<Tilemap>();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int r = 0; r < roots.Length; r++)
            {
                Tilemap[] tilemaps = roots[r].GetComponentsInChildren<Tilemap>(true);
                for (int i = 0; i < tilemaps.Length; i++)
                {
                    Tilemap tilemap = tilemaps[i];
                    if (tilemap == null || seen.Contains(tilemap))
                        continue;

                    if (!ShouldRandomize(tilemap))
                        continue;

                    seen.Add(tilemap);
                    results.Add(tilemap);
                }
            }

            return results;
        }

        static bool ShouldRandomize(Tilemap tilemap)
        {
            if (IsExcluded(tilemap.gameObject))
                return false;

            if (tilemap.GetComponent<DcssFloorVarietyInclude>() != null)
                return true;

            return tilemap.gameObject.name == "Floor";
        }

        static List<Tilemap> CollectAlignmentTilemaps(Scene scene)
        {
            var results = new List<Tilemap>();
            var seen = new HashSet<Tilemap>();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int r = 0; r < roots.Length; r++)
            {
                Tilemap[] tilemaps = roots[r].GetComponentsInChildren<Tilemap>(true);
                for (int i = 0; i < tilemaps.Length; i++)
                {
                    Tilemap tilemap = tilemaps[i];
                    if (tilemap == null || seen.Contains(tilemap))
                        continue;

                    string name = tilemap.gameObject.name;
                    if (name != "Floor" && name != "Wall")
                        continue;

                    seen.Add(tilemap);
                    results.Add(tilemap);
                }
            }

            return results;
        }

        static int CountPaintedCells(Tilemap tilemap)
        {
            int count = 0;
            BoundsInt bounds = tilemap.cellBounds;
            for (int z = bounds.zMin; z < bounds.zMax; z++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        if (tilemap.HasTile(new Vector3Int(x, y, z)))
                            count++;
                    }
                }
            }

            return count;
        }

        static bool IsExcluded(GameObject gameObject)
        {
            Transform current = gameObject.transform;
            while (current != null)
            {
                if (current.GetComponent<DcssFloorVarietyExclude>() != null)
                    return true;

                current = current.parent;
            }

            return false;
        }

        static TileBase PickTile(Vector3Int cell, int sceneSalt, TileBase[] tiles)
        {
            int hash = sceneSalt;
            hash = CombineHash(hash, cell.x);
            hash = CombineHash(hash, cell.y);
            int index = Mathf.Abs(hash) % tiles.Length;
            return tiles[index];
        }

        static int CombineHash(int a, int b) => unchecked((a * 397) ^ b);

        static TileBase[] EnsureTileAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TileMaps/Town"))
                AssetDatabase.CreateFolder("Assets/TileMaps", "Town");
            if (!AssetDatabase.IsValidFolder(TileAssetFolder))
                AssetDatabase.CreateFolder("Assets/TileMaps/Town", "Dcss");

            var tiles = new TileBase[TileAssetNames.Length];
            for (int i = 0; i < TileAssetNames.Length; i++)
            {
                string assetPath = $"{TileAssetFolder}/{TileAssetNames[i]}";
                var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                if (existing != null)
                {
                    tiles[i] = existing;
                    continue;
                }

                Sprite sprite = LoadSprite($"{SpriteFolder}/{SpriteFiles[i]}");
                if (sprite == null)
                {
                    Debug.LogWarning($"[DcssFloor] Missing sprite: {SpriteFiles[i]}");
                    continue;
                }

                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                AssetDatabase.CreateAsset(tile, assetPath);
                tiles[i] = tile;
            }

            return tiles;
        }

        static bool ConfigureFloorSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[DcssFloor] Missing texture: {path}");
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SaveAndReimport();
            return true;
        }

        static Sprite LoadSprite(string texturePath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    return sprite;
            }

            return null;
        }
    }
}
#endif
