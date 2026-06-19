#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.GridFeatures;
using JRogue.Manager.Door;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Visibility;
using JRogue.View;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using JRogue.World.Lighting;
using JRogue.World.MapInteract;
using JRogue.World.Town;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>
    /// DistrictTest hub scene: scene-painted Dimension Square under Resources/Town/DistrictTest/.
    /// </summary>
    public static class DistrictTownTestSceneCreator
    {
        const string TemplateScenePath = "Assets/Scenes/Dungeon/DungeonFloorTest.unity";
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        const string WallTilePath = "Assets/TileMaps/Town/Town_WallBuilding.asset";
        const string DcssTileFolder = "Assets/TileMaps/Town/Dcss";

        static readonly string[] DcssFloorTileAssets =
        {
            "Dcss_Floor_RectGray0.asset",
            "Dcss_Floor_RectGray1.asset",
            "Dcss_Floor_RectGray2.asset",
            "Dcss_Floor_RectGray3.asset",
        };

        [MenuItem("JRogue/Town/Create District Town Test Scene")]
        public static void CreateDistrictTownTestScene() => CreateOrFixInternal(repaintTiles: true);

        [MenuItem("JRogue/Town/Fix District Town Test Scene")]
        public static void FixDistrictTownTestScene() => CreateOrFixInternal(repaintTiles: true);

        static void CreateOrFixInternal(bool repaintTiles)
        {
            if (!File.Exists(TemplateScenePath))
            {
                Debug.LogError(
                    $"[DistrictTownTest] Missing template {TemplateScenePath}. Run JRogue → Dungeon → Create DungeonFloorTest Scene.");
                return;
            }

            EnsureDistrictFolders();
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
            EnsureDimensionSquarePalettes();
            DungeonFloorDefinition squareDef = EnsureDimensionSquareFloorDefinition();
            AdventureGuildExchangePackCreator.SetupAdventureGuildExchange();
            MarketTownPackCreator.SetupMarketTown();
            MarketGeneralStorePackCreator.SetupMarketGeneralStore();
            DungeonFloorDefinition guildInteriorDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.AdventureGuildInteriorFloorDef);
            DungeonFloorDefinition marketDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.MarketFloorDef);
            DungeonFloorDefinition storeInteriorDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.MarketGeneralStoreInteriorFloorDef);
            MarketTownPackCreator.UpdateDistrictCatalog(squareDef, marketDef, guildInteriorDef, storeInteriorDef);

            if (!File.Exists(TownDistrictTestPaths.DistrictTownTestScene))
            {
                AssetDatabase.CopyAsset(TemplateScenePath, TownDistrictTestPaths.DistrictTownTestScene);
                AssetDatabase.Refresh();
            }

            Scene scene = EditorSceneManager.OpenScene(TownDistrictTestPaths.DistrictTownTestScene, OpenSceneMode.Single);
            ConfigureSceneHierarchy(scene, squareDef, marketDef, guildInteriorDef, storeInteriorDef);
            if (repaintTiles)
            {
                PaintDimensionSquareLayout();
                PaintMarketTownLayout();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[DistrictTownTest] Saved {TownDistrictTestPaths.DistrictTownTestScene}. " +
                $"Data: {TownDistrictTestPaths.DimensionSquareFolder}. Press Play to walk the hub.");
        }

        static void EnsureDistrictFolders()
        {
            EnsureFolder("Assets/Scenes/Town");
            EnsureFolder(TownDistrictTestPaths.DistrictTestRoot);
            EnsureFolder(TownDistrictTestPaths.DistrictTestRoot + "/TownArea");
            EnsureFolder(TownDistrictTestPaths.DimensionSquareFolder);
            EnsureFolder(TownDistrictTestPaths.MarketFolder);
            EnsureFolder(TownDistrictTestPaths.MarketGeneralStoreFolder);
            EnsureFolder(TownDistrictTestPaths.DistrictTestRoot + "/Building");
        }

        static void EnsureDimensionSquarePalettes()
        {
            var floorTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < DcssFloorTileAssets.Length; i++)
                floorTiles.Add(($"{DcssTileFolder}/{DcssFloorTileAssets[i]}", 5));

            CreateOrUpdatePalette(
                TownDistrictTestPaths.DimensionSquareFloorPalette,
                "dimension_square_floor",
                DungeonTilePaletteLayer.Floor,
                floorTiles.ToArray());

            CreateOrUpdatePalette(
                TownDistrictTestPaths.DimensionSquareWallPalette,
                "dimension_square_wall",
                DungeonTilePaletteLayer.Wall,
                new[] { (WallTilePath, 5) });
        }

        static DungeonFloorDefinition EnsureDimensionSquareFloorDefinition()
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{DcssFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            DungeonTilePalette floorPalette =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.DimensionSquareFloorPalette);
            DungeonTilePalette wallPalette =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.DimensionSquareWallPalette);
            PartyFormationSpawnProfile formation =
                AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(
                    "Assets/Resources/Dungeon/PartyFormation_Default.asset");

            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.DimensionSquareFloorDef);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
                AssetDatabase.CreateAsset(def, TownDistrictTestPaths.DimensionSquareFloorDef);
            }

            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = DimensionSquareFloorIds.FloorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("layoutStamp").objectReferenceValue = null;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue = floorPalette;
            so.FindProperty("defaultWallPalette").objectReferenceValue = wallPalette;
            so.FindProperty("playerSafeRadius").intValue = 8;
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        // Catalog updated via MarketTownPackCreator.UpdateDistrictCatalog (4 hub floors).

        static void ConfigureSceneHierarchy(
            Scene scene,
            DungeonFloorDefinition squareDef,
            DungeonFloorDefinition marketDef,
            DungeonFloorDefinition guildInteriorDef,
            DungeonFloorDefinition storeInteriorDef)
        {
            GameObject systems = GameObject.Find(DungeonFloorTestSceneValidator.SystemsObjectName);
            if (systems == null)
            {
                Debug.LogError("[DistrictTownTest] DungeonTestSystems missing — recreate from template.");
                return;
            }

            TownPackCreatorLighting.EnsureLightingOnSystems(systems);
            DungeonWorldFeatureServices.EnsureOn(systems);
            if (systems.GetComponent<DoorService>() == null)
                systems.AddComponent<DoorService>();
            if (systems.GetComponent<VisibilityManager>() == null)
                systems.AddComponent<VisibilityManager>();
            if (systems.GetComponent<PortalEntryService>() == null)
                systems.AddComponent<PortalEntryService>();

            DungeonFloorDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinitionCatalog>(TownDistrictTestPaths.DistrictTestCatalog);

            DungeonFloorInstanceManager floorManager = systems.GetComponent<DungeonFloorInstanceManager>()
                ?? systems.AddComponent<DungeonFloorInstanceManager>();

            var managerSo = new SerializedObject(floorManager);
            managerSo.FindProperty("useDontDestroyOnLoad").boolValue = false;
            managerSo.FindProperty("floorDefinitions").arraySize = 4;
            managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(0).objectReferenceValue = squareDef;
            managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(1).objectReferenceValue = marketDef;
            managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(2).objectReferenceValue = guildInteriorDef;
            managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(3).objectReferenceValue = storeInteriorDef;
            Transform floorsRoot = EnsureFloorsRoot(systems, floorManager);
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            RemoveChildFloorsExcept(
                floorsRoot,
                DimensionSquareFloorIds.FloorId,
                MarketTownFloorIds.FloorId,
                AdventureGuildExchangeLayout.InteriorFloorId,
                MarketGeneralStoreLayout.InteriorFloorId);

            DungeonFloorInstance squareInstance = EnsureScenePaintedFloor(floorsRoot, squareDef);
            squareInstance.gameObject.SetActive(true);

            if (marketDef != null)
            {
                DungeonFloorInstance marketInstance = EnsureScenePaintedFloor(floorsRoot, marketDef);
                marketInstance.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError(
                    $"[DistrictTownTest] Missing market floor definition at {TownDistrictTestPaths.MarketFloorDef}. " +
                    "Run JRogue → Town → Setup Market Town Area.");
            }

            if (guildInteriorDef != null)
            {
                DungeonFloorInstance guildInteriorInstance = EnsureScenePaintedFloor(floorsRoot, guildInteriorDef);
                guildInteriorInstance.gameObject.SetActive(false);
                AdventureGuildExchangePackCreator.IntegrateDistrictTownScene(guildInteriorInstance);
            }

            if (storeInteriorDef != null)
            {
                DungeonFloorInstance storeInteriorInstance = EnsureScenePaintedFloor(floorsRoot, storeInteriorDef);
                storeInteriorInstance.gameObject.SetActive(false);
                MarketGeneralStorePackCreator.IntegrateDistrictTownScene(storeInteriorInstance);
            }

            DungeonFloorTestController test = systems.GetComponent<DungeonFloorTestController>()
                ?? systems.AddComponent<DungeonFloorTestController>();

            var testSo = new SerializedObject(test);
            testSo.FindProperty("floorInstanceManager").objectReferenceValue = floorManager;
            testSo.FindProperty("floorCatalog").objectReferenceValue = catalog;
            testSo.FindProperty("startFloorId").stringValue = DimensionSquareFloorIds.FloorId;
            testSo.FindProperty("runSeed").intValue = 200001;
            testSo.FindProperty("autoGenerateOnPlay").boolValue = true;
            testSo.FindProperty("validateSceneOnPlay").boolValue = true;
            testSo.FindProperty("tryRepairSceneAtRuntime").boolValue = true;
            testSo.FindProperty("showGenerateButton").boolValue = false;

            GameObject party = GameObject.Find(DungeonFloorTestSceneValidator.PartyObjectName);
            DungeonRunBootstrap bootstrap = party != null ? party.GetComponent<DungeonRunBootstrap>() : null;
            if (bootstrap != null)
                testSo.FindProperty("runBootstrap").objectReferenceValue = bootstrap;
            testSo.ApplyModifiedPropertiesWithoutUndo();

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = AdventureGuildExchangeLayout.DistrictHubCameraOrthoSize;
                if (cam.GetComponent<CameraFollow>() == null)
                    cam.gameObject.AddComponent<CameraFollow>();
            }

            EnsureGameplayUiFromSampleScene(scene);
            EnsureMarkers(squareInstance);
        }

        static DungeonFloorInstance EnsureScenePaintedFloor(Transform floorsRoot, DungeonFloorDefinition floorDef)
        {
            string floorId = floorDef.FloorId;
            Transform existing = floorsRoot.Find(floorId);
            GameObject floorGo = existing != null ? existing.gameObject : new GameObject(floorId);
            if (existing == null)
                floorGo.transform.SetParent(floorsRoot, false);

            DungeonFloorInstance instance = floorGo.GetComponent<DungeonFloorInstance>()
                ?? floorGo.AddComponent<DungeonFloorInstance>();
            instance.Configure(floorDef);
            instance.EnsureHierarchyBuilt();
            return instance;
        }

        static void PaintDimensionSquareLayout()
        {
            DungeonFloorInstance instance = FindDimensionSquareInstance();
            if (instance == null)
            {
                Debug.LogError("[DistrictTownTest] No dimension_square DungeonFloorInstance to paint.");
                return;
            }

            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            if (floorMap == null || wallMap == null)
            {
                Debug.LogError("[DistrictTownTest] Floor/Wall tilemaps missing.");
                return;
            }

            TileBase[] floorTiles = LoadFloorTilesFromPalette();
            TileBase wallTile = LoadWallTileFromPalette();
            if (floorTiles == null || floorTiles.Length == 0 || wallTile == null)
            {
                Debug.LogError("[DistrictTownTest] Missing floor or wall palette tiles.");
                return;
            }

            Undo.RecordObject(floorMap, "Paint dimension square floor");
            Undo.RecordObject(wallMap, "Paint dimension square walls");
            DimensionSquareLayout.Paint(floorMap, wallMap, floorTiles, wallTile);
            AdventureGuildExchangePackCreator.PaintAdventureGuildExteriorFacade(floorMap, wallMap);

            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(floorMap);
            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(wallMap);

            floorMap.CompressBounds();
            wallMap.CompressBounds();
            EditorUtility.SetDirty(floorMap);
            EditorUtility.SetDirty(wallMap);
        }

        static void PaintMarketTownLayout()
        {
            DungeonFloorInstance instance = FindFloorInstance(MarketTownFloorIds.FloorId);
            if (instance == null)
            {
                Debug.LogError("[DistrictTownTest] No town_market DungeonFloorInstance to paint.");
                return;
            }

            MarketTownPackCreator.IntegrateDistrictTownScene(instance);
        }

        static DungeonFloorInstance FindFloorInstance(string floorId)
        {
            GameObject systems = GameObject.Find(DungeonFloorTestSceneValidator.SystemsObjectName);
            if (systems != null)
            {
                Transform floorsRoot = systems.transform.Find("Floors");
                if (floorsRoot != null)
                {
                    Transform child = floorsRoot.Find(floorId);
                    if (child != null && child.TryGetComponent(out DungeonFloorInstance hierarchyInstance))
                        return hierarchyInstance;
                }
            }

            DungeonFloorInstance[] instances =
                Object.FindObjectsByType<DungeonFloorInstance>(FindObjectsInactive.Include);
            for (int i = 0; i < instances.Length; i++)
            {
                DungeonFloorInstance instance = instances[i];
                if (instance != null && instance.FloorId == floorId)
                    return instance;
            }

            return null;
        }

        static DungeonFloorInstance FindDimensionSquareInstance() =>
            FindFloorInstance(DimensionSquareFloorIds.FloorId);

        static TileBase[] LoadFloorTilesFromPalette()
        {
            var palette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(
                TownDistrictTestPaths.DimensionSquareFloorPalette);
            return ExtractTilesFromPalette(palette);
        }

        static TileBase LoadWallTileFromPalette()
        {
            var palette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(
                TownDistrictTestPaths.DimensionSquareWallPalette);
            TileBase[] tiles = ExtractTilesFromPalette(palette);
            return tiles != null && tiles.Length > 0 ? tiles[0] : null;
        }

        static TileBase[] ExtractTilesFromPalette(DungeonTilePalette palette)
        {
            if (palette == null || palette.Entries == null)
                return null;

            var tiles = new List<TileBase>();
            DungeonTilePaletteEntry[] entries = palette.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile != null)
                    tiles.Add(entries[i].tile);
            }

            return tiles.Count > 0 ? tiles.ToArray() : null;
        }

        static void EnsureMarkers(DungeonFloorInstance instance)
        {
            Transform grid = instance.transform.Find("Grid");
            if (grid == null)
                return;

            Transform markersRoot = instance.transform.Find("Markers");
            if (markersRoot == null)
            {
                var markersGo = new GameObject("Markers");
                markersGo.transform.SetParent(instance.transform, false);
                markersRoot = markersGo.transform;
            }

            ClearChildren(markersRoot);
            Grid gridComponent = grid.GetComponent<Grid>();
            Tilemap floorMap = instance.Tilemaps.FloorMap;

            CreateMarker(markersRoot, gridComponent, floorMap, "PlayerStart", StaticHubMarkerKind.PlayerStart, DimensionSquareLayout.PlayerStartCell);
            CreateMarker(markersRoot, gridComponent, floorMap, "DungeonPortal", StaticHubMarkerKind.DungeonPortal, DimensionSquareLayout.DungeonPortalCell);
            CreateMarker(markersRoot, gridComponent, floorMap, "NpcSlot_North", StaticHubMarkerKind.NpcSlot, DimensionSquareLayout.NpcSlotNorthCell, "npc_slot_north");
            CreateMarker(markersRoot, gridComponent, floorMap, "NpcSlot_South", StaticHubMarkerKind.NpcSlot, DimensionSquareLayout.NpcSlotSouthCell, "npc_slot_south");
            CreateMarker(markersRoot, gridComponent, floorMap, "NpcSlot_East", StaticHubMarkerKind.NpcSlot, DimensionSquareLayout.NpcSlotEastCell, "npc_slot_east");
            CreateMarker(markersRoot, gridComponent, floorMap, "NpcSlot_West", StaticHubMarkerKind.NpcSlot, DimensionSquareLayout.NpcSlotWestCell, "npc_slot_west");
        }

        static void CreateMarker(
            Transform parent,
            Grid grid,
            Tilemap floorMap,
            string objectName,
            StaticHubMarkerKind kind,
            Vector3Int cell,
            string markerId = null)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            StaticHubMarker marker = go.AddComponent<StaticHubMarker>();
            marker.EditorConfigure(kind, cell, markerId);

            Vector3 world = grid != null
                ? grid.GetCellCenterWorld(cell)
                : floorMap != null
                    ? GridCellWorld.GetCellCenter(floorMap, cell)
                    : new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            go.transform.position = world;
        }

        internal static void CreateOrUpdatePalette(
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

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        static Transform EnsureFloorsRoot(GameObject systems, DungeonFloorInstanceManager floorManager)
        {
            Transform floors = systems.transform.Find("Floors");
            if (floors == null)
            {
                var floorsGo = new GameObject("Floors");
                floorsGo.transform.SetParent(systems.transform, false);
                floors = floorsGo.transform;
            }

            var managerSo = new SerializedObject(floorManager);
            managerSo.FindProperty("floorsRoot").objectReferenceValue = floors;
            managerSo.ApplyModifiedPropertiesWithoutUndo();
            return floors;
        }

        static void RemoveChildFloorsExcept(Transform floorsRoot, params string[] keepFloorIds)
        {
            if (floorsRoot == null || keepFloorIds == null || keepFloorIds.Length == 0)
                return;

            var keep = new HashSet<string>(keepFloorIds);
            for (int i = floorsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = floorsRoot.GetChild(i);
                if (!keep.Contains(child.name))
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        static void EnsureGameplayUiFromSampleScene(Scene targetScene)
        {
            if (SceneHasRootNamed(targetScene, "Canvas") && SceneHasRootNamed(targetScene, "EventSystem"))
                return;

            if (!File.Exists(SampleScenePath))
                return;

            Scene sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject root in sampleScene.GetRootGameObjects())
                {
                    if (root.name != "Canvas" && root.name != "EventSystem")
                        continue;

                    if (SceneHasRootNamed(targetScene, root.name))
                        continue;

                    GameObject copy = Object.Instantiate(root);
                    copy.name = root.name;
                    SceneManager.MoveGameObjectToScene(copy, targetScene);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(sampleScene, true);
            }
        }

        static bool SceneHasRootNamed(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return true;
            }

            return false;
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
