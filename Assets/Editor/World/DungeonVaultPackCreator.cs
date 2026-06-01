#if UNITY_EDITOR
using System.IO;
using JRogue.Data.Door;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Item;
using JRogue.Spawn;
using JRogue.World.Generation;
using JRogue.World.Generation.Vaults;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    public static class DungeonVaultPackCreator
    {
        const string VaultRoot = "Assets/Data/Vaults";
        const string Floor1Folder = VaultRoot + "/Floor1";
        const string TileFolder = "Assets/TileMaps/Vault";
        const string CatalogPath = VaultRoot + "/Floor1_VaultCatalog.asset";
        const string RegistryPath = VaultRoot + "/VaultAssetRegistry.asset";
        const string Floor01Path = "Assets/Resources/Dungeon/Floor_dungeon_floor_01.asset";

        const string SandThemePath = "Assets/Sprites/Environment/SandTheme.png";
        const string SnowThemePath = "Assets/Sprites/Environment/SnowTheme.png";

        [MenuItem("JRogue/Dungeon/Create Floor 1 Vault Pack")]
        public static void CreateFloor1VaultPack()
        {
            EnsureVaultTile("SandTheme:32", SandThemePath, "Scavengers2_SpriteSheet_32");
            EnsureVaultTile("SandTheme:50", SandThemePath, "Scavengers2_SpriteSheet_50");
            EnsureVaultTile("SnowTheme:32", SnowThemePath, "Scavengers2_SpriteSheet_32");
            EnsureVaultTile("SnowTheme:48", SnowThemePath, "Scavengers2_SpriteSheet_48");

            Directory.CreateDirectory(Floor1Folder);
            WriteDefaultVaultFilesIfMissing();

            VaultAssetRegistry registry = LoadOrCreateRegistry();
            WireRegistry(registry);

            DungeonVaultCatalog catalog = LoadOrCreateCatalog(registry);
            WireCatalog(catalog);

            var floor01 = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(Floor01Path);
            if (floor01 != null)
            {
                SerializedObject floorSo = new SerializedObject(floor01);
                floorSo.FindProperty("vaultCatalog").objectReferenceValue = catalog;
                floorSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(floor01);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dungeon] Floor 1 vault pack created (tiles, registry, catalog, .vault files).");
        }

        static void WriteDefaultVaultFilesIfMissing()
        {
            CopyEmbeddedIfMissing(
                $"{Floor1Folder}/vault_shrine_5x5.vault",
                "Assets/Data/Vaults/Floor1/vault_shrine_5x5.vault");
            CopyEmbeddedIfMissing(
                $"{Floor1Folder}/vault_ambush_corridor_7x4.vault",
                "Assets/Data/Vaults/Floor1/vault_ambush_corridor_7x4.vault");
        }

        static void CopyEmbeddedIfMissing(string path, string existingPath)
        {
            if (File.Exists(path))
                return;

            if (File.Exists(existingPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Floor1Folder);
                File.Copy(existingPath, path);
                return;
            }
        }

        static VaultAssetRegistry LoadOrCreateRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<VaultAssetRegistry>(RegistryPath);
            if (registry != null)
                return registry;

            Directory.CreateDirectory(VaultRoot);
            registry = ScriptableObject.CreateInstance<VaultAssetRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            return registry;
        }

        static void WireRegistry(VaultAssetRegistry registry)
        {
            var healing = AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Resources/Item/Potion/Potion_HealingPotion.asset");
            var lever = AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(
                "Assets/Data/Interactables/LeverSwitch_First.asset");
            var lava = AssetDatabase.LoadAssetAtPath<EnvironmentalHazardDefinition>(
                "Assets/Resources/Hazards/EnvironmentalHazard_Lava.asset");
            var door = AssetDatabase.LoadAssetAtPath<DoorDefinition>(
                "Assets/Data/Doors/Door_Test_Horizontal.asset");
            var skeletonSpawn = AssetDatabase.LoadAssetAtPath<EnemySpawnDefinition>(
                "Assets/Resources/Dungeon/Spawn_DungeonTestSkeleton.asset");

            SerializedObject so = new SerializedObject(registry);

            SetTileEntries(so);
            SetLookup(so, "items", new (string id, Object asset)[]
            {
                ("healing_potion", healing),
            });
            SetLookup(so, "interactables", new (string id, Object asset)[]
            {
                ("lever_shrine", lever),
            });
            SetLookup(so, "hazards", new (string id, Object asset)[]
            {
                ("lava", lava),
            });
            SetLookup(so, "doors", new (string id, Object asset)[]
            {
                ("door_corridor", door),
            });
            SetLookup(so, "enemies", new (string id, Object asset)[]
            {
                ("skeleton", skeletonSpawn),
            });

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
            registry.RebuildLookups();
        }

        static void SetTileEntries(SerializedObject registrySo)
        {
            SerializedProperty tiles = registrySo.FindProperty("tiles");
            tiles.arraySize = 4;
            AssignTile(tiles, 0, "SandTheme:32", $"{TileFolder}/SandTheme_32.asset");
            AssignTile(tiles, 1, "SandTheme:50", $"{TileFolder}/SandTheme_50.asset");
            AssignTile(tiles, 2, "SnowTheme:32", $"{TileFolder}/SnowTheme_32.asset");
            AssignTile(tiles, 3, "SnowTheme:48", $"{TileFolder}/SnowTheme_48.asset");
        }

        static void AssignTile(SerializedProperty tiles, int index, string key, string tilePath)
        {
            SerializedProperty element = tiles.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("key").stringValue = key;
            element.FindPropertyRelative("tile").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TileBase>(tilePath);
        }

        static void SetLookup(
            SerializedObject registrySo,
            string propertyName,
            (string id, Object asset)[] entries)
        {
            SerializedProperty array = registrySo.FindProperty(propertyName);
            array.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = entries[i].id;
                if (propertyName == "items")
                    element.FindPropertyRelative("item").objectReferenceValue = entries[i].asset as ItemData;
                else if (propertyName == "interactables")
                    element.FindPropertyRelative("definition").objectReferenceValue =
                        entries[i].asset as InteractableTileDefinition;
                else if (propertyName == "hazards")
                    element.FindPropertyRelative("definition").objectReferenceValue =
                        entries[i].asset as EnvironmentalHazardDefinition;
                else if (propertyName == "doors")
                    element.FindPropertyRelative("definition").objectReferenceValue =
                        entries[i].asset as DoorDefinition;
                else if (propertyName == "enemies")
                    element.FindPropertyRelative("spawnDefinition").objectReferenceValue =
                        entries[i].asset as EnemySpawnDefinition;
            }
        }

        static DungeonVaultCatalog LoadOrCreateCatalog(VaultAssetRegistry registry)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DungeonVaultCatalog>(CatalogPath);
            if (catalog != null)
            {
                SerializedObject so = new SerializedObject(catalog);
                so.FindProperty("assetRegistry").objectReferenceValue = registry;
                so.ApplyModifiedPropertiesWithoutUndo();
                WireCatalog(catalog);
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<DungeonVaultCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            SerializedObject createSo = new SerializedObject(catalog);
            createSo.FindProperty("assetRegistry").objectReferenceValue = registry;
            createSo.ApplyModifiedPropertiesWithoutUndo();
            WireCatalog(catalog);
            return catalog;
        }

        static void WireCatalog(DungeonVaultCatalog catalog)
        {
            const string shrinePath = Floor1Folder + "/vault_shrine_5x5.vault";
            const string ambushPath = Floor1Folder + "/vault_ambush_corridor_7x4.vault";
            EnsureTextAssetImporter(shrinePath);
            EnsureTextAssetImporter(ambushPath);
            AssetDatabase.Refresh();

            TextAsset shrine = AssetDatabase.LoadAssetAtPath<TextAsset>(shrinePath);
            TextAsset ambush = AssetDatabase.LoadAssetAtPath<TextAsset>(ambushPath);
            if (shrine == null)
                Debug.LogWarning($"[Dungeon] Could not load TextAsset at {shrinePath} — catalog will use sourceAssetPath fallback.");
            if (ambush == null)
                Debug.LogWarning($"[Dungeon] Could not load TextAsset at {ambushPath} — catalog will use sourceAssetPath fallback.");

            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = 2;

            WriteCatalogEntry(entries, 0, "vault_shrine_5x5", shrine, ToDataPath(shrinePath), weight: 1, maxPerFloor: 1, minDistance: 6);
            WriteCatalogEntry(entries, 1, "vault_ambush_corridor_7x4", ambush, ToDataPath(ambushPath), weight: 1, maxPerFloor: 1, minDistance: 6);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        static void WriteCatalogEntry(
            SerializedProperty entries,
            int index,
            string vaultId,
            TextAsset source,
            string sourceAssetPath,
            int weight,
            int maxPerFloor,
            int minDistance)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("vaultId").stringValue = vaultId;
            entry.FindPropertyRelative("sourceFile").objectReferenceValue = source;
            entry.FindPropertyRelative("sourceAssetPath").stringValue = sourceAssetPath;
            entry.FindPropertyRelative("weight").intValue = weight;
            entry.FindPropertyRelative("maxPerFloor").intValue = maxPerFloor;
            entry.FindPropertyRelative("minDistanceFromPlayerStart").intValue = minDistance;
        }

        static string ToDataPath(string assetPath)
        {
            string normalized = assetPath.Replace('\\', '/');
            const string prefix = "Assets/";
            if (normalized.StartsWith(prefix, System.StringComparison.Ordinal))
                return normalized.Substring(prefix.Length);

            return normalized;
        }

        static void EnsureTextAssetImporter(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath) != null)
                return;

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        static void EnsureVaultTile(string key, string texturePath, string spriteName)
        {
            string safeName = key.Replace(":", "_");
            string assetPath = $"{TileFolder}/{safeName}.asset";
            Directory.CreateDirectory(TileFolder);

            var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (existing != null)
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
    }
}
#endif
