#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Interactables;
using JRogue.World.Generation;
using JRogue.World.Generation.Vaults;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Phase 3 production vault pack: DCSS tile registry entries, bump interactables, catalog placement rules.
    /// </summary>
    public static class DungeonFloor1ProductionVaultPackCreator
    {
        const string MenuPath = "JRogue/Dungeon/Create Floor 1 Production Vault Pack";

        const string VaultRoot = "Assets/Data/Vaults";
        const string ProductionVaultFolder = VaultRoot + "/Floor1/Production";
        const string CatalogPath = VaultRoot + "/Floor1_Production_VaultCatalog.asset";
        const string RegistryPath = VaultRoot + "/VaultAssetRegistry.asset";
        const string FloorProdPath = "Assets/Resources/Dungeon/Floor_prod_dungeon_floor_01.asset";
        const string TileRoot = "Assets/TileMaps/Dcss/Cavern";
        const string InteractableRoot = "Assets/Data/Interactables/Production";
        const string EffectRoot = "Assets/Data/Interactables/Effects/Production";

        const string DcssRoot = "Assets/Sprites/DCSS/Dungeon Crawl Stone Soup Full";

        [MenuItem(MenuPath, false, 53)]
        public static void CreateFloor1ProductionVaultPack()
        {
            EnsureFolder(TileRoot);
            EnsureFolder(InteractableRoot);
            EnsureFolder(EffectRoot);

            EnsureTileFromSprite($"{DcssRoot}/dungeon/floor/grey_dirt_0_new.png", "grey_dirt_0_new");
            EnsureTileFromSprite($"{DcssRoot}/dungeon/floor/grey_dirt_1_new.png", "grey_dirt_1_new");
            EnsureTileFromSprite($"{DcssRoot}/dungeon/floor/grey_dirt_2_new.png", "grey_dirt_2_new");
            EnsureTileFromSprite($"{DcssRoot}/dungeon/floor/grey_dirt_3_new.png", "grey_dirt_3_new");
            EnsureTileFromSprite($"{DcssRoot}/dungeon/floor/grey_dirt_4_new.png", "grey_dirt_4_new");
            EnsureTileFromSprite($"{DcssRoot}/dungeon/wall/stone2_gray_2_new.png", "stone2_gray_2_new");
            EnsureTileFromSprite($"{DcssRoot}/dungeon/wall/stone2_gray_3_new.png", "stone2_gray_3_new");
            EnsureTileFromSprite($"{DcssRoot}/dungeon/altars/misc_altar.png", "altar_misc");

            EnsureTileFromSprite($"{DcssRoot}/dungeon/water/shoals_shallow_water_1_new.png", "shoals_shallow_water_1_new");
            EnsureTileFromSprite($"{DcssRoot}/dungeon/water/shoals_shallow_water_2_new.png", "shoals_shallow_water_2_new");
            EnsureTileFromSprite($"{DcssRoot}/dungeon/water/shoals_shallow_water_3_new.png", "shoals_shallow_water_3_new");
            EnsureTileFromSprite($"{DcssRoot}/dungeon/water/shoals_shallow_water_4_new.png", "shoals_shallow_water_4_new");

            // Reuse Phase 2 cyan glow tiles when present.
            LinkExistingTile("_cyan_floor_nerves_2_new", "DcssCavern:_cyan_floor_nerves_2_new");
            LinkExistingTile("_cyan_floor_nerves_4_new", "DcssCavern:_cyan_floor_nerves_4_new");

            ShowFlavorDialogEffect monumentEffect = EnsureFlavorEffect(
                $"{EffectRoot}/ShowFlavorDialog_Monument.asset",
                "There is a faded inscription on the monument.");
            ShowFlavorDialogEffect altarEffect = EnsureFlavorEffect(
                $"{EffectRoot}/ShowFlavorDialog_Altar.asset",
                "There are 3 small indentations and 1 larger indentation.");

            InteractableTileDefinition monumentDef = EnsureBumpInteractable(
                $"{InteractableRoot}/BumpMonumentInscription.asset",
                "Monument inscription",
                InteractableTileId.BumpMonumentInscription,
                monumentEffect);
            InteractableTileDefinition altarDef = EnsureBumpInteractable(
                $"{InteractableRoot}/BumpAltarIndentations.asset",
                "Altar indentations",
                InteractableTileId.BumpAltarIndentations,
                altarEffect);

            VaultAssetRegistry registry = LoadOrCreateRegistry();
            WireRegistry(registry, monumentDef, altarDef);

            DungeonVaultCatalog catalog = LoadOrCreateCatalog(registry);
            WireProductionCatalog(catalog);

            var floorProd = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(FloorProdPath);
            if (floorProd != null)
            {
                SerializedObject floorSo = new SerializedObject(floorProd);
                floorSo.FindProperty("vaultCatalog").objectReferenceValue = catalog;
                floorSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(floorProd);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dungeon] Floor 1 production vault pack created (registry, catalog rules, bump interactables).");
        }

        /// <summary>Entry point for Unity batchmode: -executeMethod JRogue.Editor.World.DungeonFloor1ProductionVaultPackCreator.CreateFloor1ProductionVaultPackBatch</summary>
        public static void CreateFloor1ProductionVaultPackBatch()
        {
            CreateFloor1ProductionVaultPack();
            EditorApplication.Exit(0);
        }

        static void WireRegistry(
            VaultAssetRegistry registry,
            InteractableTileDefinition monumentDef,
            InteractableTileDefinition altarDef)
        {
            SerializedObject so = new SerializedObject(registry);

            WriteTileEntries(so);
            WriteInteractableEntries(so, monumentDef, altarDef);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
        }

        static void WriteTileEntries(SerializedObject registrySo)
        {
            var keys = new (string key, string tileName)[]
            {
                ("DcssCavern:grey_dirt_0_new", "grey_dirt_0_new"),
                ("DcssCavern:grey_dirt_1_new", "grey_dirt_1_new"),
                ("DcssCavern:grey_dirt_2_new", "grey_dirt_2_new"),
                ("DcssCavern:grey_dirt_3_new", "grey_dirt_3_new"),
                ("DcssCavern:grey_dirt_4_new", "grey_dirt_4_new"),
                ("DcssCavern:stone2_gray_2_new", "stone2_gray_2_new"),
                ("DcssCavern:stone2_gray_3_new", "stone2_gray_3_new"),
                ("DcssCavern:altar_misc", "altar_misc"),
                ("DcssCavern:shoals_shallow_water_1_new", "shoals_shallow_water_1_new"),
                ("DcssCavern:shoals_shallow_water_2_new", "shoals_shallow_water_2_new"),
                ("DcssCavern:shoals_shallow_water_3_new", "shoals_shallow_water_3_new"),
                ("DcssCavern:shoals_shallow_water_4_new", "shoals_shallow_water_4_new"),
                ("DcssCavern:_cyan_floor_nerves_2_new", "_cyan_floor_nerves_2_new"),
                ("DcssCavern:_cyan_floor_nerves_4_new", "_cyan_floor_nerves_4_new"),
            };

            SerializedProperty tiles = registrySo.FindProperty("tiles");
            var existing = new Dictionary<string, int>();
            for (int i = 0; i < tiles.arraySize; i++)
            {
                string key = tiles.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue;
                if (!string.IsNullOrEmpty(key))
                    existing[key] = i;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                (string key, string tileName) = keys[i];
                TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileRoot}/{tileName}.asset");
                if (tile == null)
                    continue;

                if (existing.TryGetValue(key, out int index))
                {
                    tiles.GetArrayElementAtIndex(index).FindPropertyRelative("tile").objectReferenceValue = tile;
                    continue;
                }

                int newIndex = tiles.arraySize;
                tiles.InsertArrayElementAtIndex(newIndex);
                SerializedProperty entry = tiles.GetArrayElementAtIndex(newIndex);
                entry.FindPropertyRelative("key").stringValue = key;
                entry.FindPropertyRelative("tile").objectReferenceValue = tile;
            }
        }

        static void WriteInteractableEntries(
            SerializedObject registrySo,
            InteractableTileDefinition monumentDef,
            InteractableTileDefinition altarDef)
        {
            UpsertInteractable(registrySo, "bump_monument_inscription", monumentDef);
            UpsertInteractable(registrySo, "bump_altar_indentations", altarDef);
        }

        static void UpsertInteractable(
            SerializedObject registrySo,
            string id,
            InteractableTileDefinition definition)
        {
            SerializedProperty interactables = registrySo.FindProperty("interactables");
            for (int i = 0; i < interactables.arraySize; i++)
            {
                SerializedProperty entry = interactables.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("id").stringValue != id)
                    continue;

                entry.FindPropertyRelative("definition").objectReferenceValue = definition;
                return;
            }

            int index = interactables.arraySize;
            interactables.InsertArrayElementAtIndex(index);
            SerializedProperty added = interactables.GetArrayElementAtIndex(index);
            added.FindPropertyRelative("id").stringValue = id;
            added.FindPropertyRelative("definition").objectReferenceValue = definition;
        }

        static void WireProductionCatalog(DungeonVaultCatalog catalog)
        {
            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty entries = so.FindProperty("entries");

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                string vaultId = entry.FindPropertyRelative("vaultId").stringValue;

                if (vaultId == "vault_monument_8x8")
                {
                    entry.FindPropertyRelative("placementRule").enumValueIndex = (int)VaultPlacementRule.ZoneCenter;
                    entry.FindPropertyRelative("mandatory").boolValue = true;
                    entry.FindPropertyRelative("requiredZoneId").stringValue = "luminescent_cavern";
                }
                else if (vaultId == "vault_altar_3x3")
                {
                    entry.FindPropertyRelative("placementRule").enumValueIndex = (int)VaultPlacementRule.MandatoryRandom;
                    entry.FindPropertyRelative("mandatory").boolValue = true;
                    entry.FindPropertyRelative("requiredZoneId").stringValue = "northern_dark";
                }
                else if (vaultId != null && vaultId.StartsWith("vault_pond_"))
                {
                    entry.FindPropertyRelative("placementRule").enumValueIndex = (int)VaultPlacementRule.PondScatter;
                    entry.FindPropertyRelative("mandatory").boolValue = false;
                    entry.FindPropertyRelative("requiredZoneId").stringValue = "luminescent_cavern";
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        static ShowFlavorDialogEffect EnsureFlavorEffect(string path, string line)
        {
            var effect = AssetDatabase.LoadAssetAtPath<ShowFlavorDialogEffect>(path);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<ShowFlavorDialogEffect>();
                effect.dialogLine = line;
                AssetDatabase.CreateAsset(effect, path);
            }
            else
            {
                effect.dialogLine = line;
                EditorUtility.SetDirty(effect);
            }

            return effect;
        }

        static InteractableTileDefinition EnsureBumpInteractable(
            string path,
            string displayName,
            InteractableTileId interactableId,
            ShowFlavorDialogEffect effect)
        {
            var def = AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<InteractableTileDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            def.displayName = displayName;
            def.interactableId = interactableId;
            def.kind = InteractableTileKind.Shrine;
            def.blocksOccupancy = true;
            def.bumpEnabled = true;
            def.allowRepeatActivation = true;
            def.onActivateEffects = new InteractableEffect[] { effect };
            EditorUtility.SetDirty(def);
            return def;
        }

        static VaultAssetRegistry LoadOrCreateRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<VaultAssetRegistry>(RegistryPath);
            if (registry != null)
                return registry;

            registry = ScriptableObject.CreateInstance<VaultAssetRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            return registry;
        }

        static DungeonVaultCatalog LoadOrCreateCatalog(VaultAssetRegistry registry)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DungeonVaultCatalog>(CatalogPath);
            if (catalog != null)
            {
                SerializedObject so = new SerializedObject(catalog);
                so.FindProperty("assetRegistry").objectReferenceValue = registry;
                so.ApplyModifiedPropertiesWithoutUndo();
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<DungeonVaultCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            SerializedObject createSo = new SerializedObject(catalog);
            createSo.FindProperty("assetRegistry").objectReferenceValue = registry;
            createSo.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        static void EnsureTileFromSprite(string spritePath, string tileName)
        {
            EnsureSingleSpriteImport(spritePath);

            string tilePath = $"{TileRoot}/{tileName}.asset";
            Sprite sprite = LoadSingleSprite(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[VaultPack] Missing sprite {spritePath}");
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (existing != null)
            {
                if (existing.sprite != sprite)
                {
                    existing.sprite = sprite;
                    EditorUtility.SetDirty(existing);
                }

                return;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, tilePath);
        }

        static void EnsureSingleSpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static void LinkExistingTile(string tileName, string registryKey)
        {
            string tilePath = $"{TileRoot}/{tileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<TileBase>(tilePath) == null)
                Debug.LogWarning($"[VaultPack] Expected existing tile {tilePath} for {registryKey}");
        }

        static Sprite LoadSingleSprite(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    return sprite;
            }

            return null;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
