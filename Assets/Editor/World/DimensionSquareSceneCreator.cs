#if UNITY_EDITOR
using System.IO;
using JRogue.GridFeatures;
using JRogue.Manager.Door;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Visibility;
using JRogue.View;
using JRogue.World.Generation;
using JRogue.World.Lighting;
using JRogue.World.Town;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>Creates the hand-painted Dimension Square hub test scene (40×40 plus layout).</summary>
    public static class DimensionSquareSceneCreator
    {
        const string ScenePath = "Assets/Scenes/Town/DimensionSquareTest.unity";
        const string TemplateScenePath = "Assets/Scenes/Dungeon/DungeonFloorTest.unity";
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        const string DataRoot = "Assets/Resources/Town";
        const string FloorDefPath = DataRoot + "/Floor_dimension_square.asset";
        const string CatalogPath = DataRoot + "/DimensionSquareCatalog.asset";
        const string WallTilePath = "Assets/TileMaps/Town/Town_WallBuilding.asset";
        const string DcssTileFolder = "Assets/TileMaps/Town/Dcss";

        static readonly string[] DcssFloorTileAssets =
        {
            "Dcss_Floor_RectGray0.asset",
            "Dcss_Floor_RectGray1.asset",
            "Dcss_Floor_RectGray2.asset",
            "Dcss_Floor_RectGray3.asset",
        };

        [MenuItem("JRogue/Town/Create Dimension Square Test Scene")]
        public static void CreateDimensionSquareTestScene()
        {
            if (!File.Exists(TemplateScenePath))
            {
                Debug.LogError($"[DimensionSquare] Missing template {TemplateScenePath}. Run JRogue → Dungeon → Create DungeonFloorTest Scene.");
                return;
            }

            EnsureFolder("Assets/Scenes/Town");
            EnsureFloorDefinition();
            EnsureCatalog();
            EnsureDcssFloorTiles();

            if (!File.Exists(ScenePath))
            {
                AssetDatabase.CopyAsset(TemplateScenePath, ScenePath);
                AssetDatabase.Refresh();
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ConfigureSceneHierarchy(scene);
            PaintDimensionSquareLayout();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DimensionSquare] Saved {ScenePath}. Open Scene view — 40×40 plus layout is painted. Press Play to walk the hub.");
        }

        static void EnsureFloorDefinition()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(FloorDefPath);
            if (existing != null)
                return;

            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{DcssFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation =
                AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(
                    "Assets/Resources/Dungeon/PartyFormation_Default.asset");

            var def = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
            AssetDatabase.CreateAsset(def, FloorDefPath);

            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = DimensionSquareFloorIds.FloorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("layoutStamp").objectReferenceValue = null;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("playerSafeRadius").intValue = 8;
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }

        static void EnsureCatalog()
        {
            DungeonFloorDefinition floorDef = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(FloorDefPath);
            var catalog = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinitionCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DungeonFloorDefinitionCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            SerializedProperty floors = so.FindProperty("floors");
            floors.arraySize = 1;
            floors.GetArrayElementAtIndex(0).objectReferenceValue = floorDef;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        static void EnsureDcssFloorTiles()
        {
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
        }

        static void ConfigureSceneHierarchy(Scene scene)
        {
            GameObject systems = GameObject.Find(DungeonFloorTestSceneValidator.SystemsObjectName);
            if (systems == null)
            {
                Debug.LogError("[DimensionSquare] DungeonTestSystems missing — recreate from template.");
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

            DungeonFloorDefinition floorDef = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(FloorDefPath);
            DungeonFloorDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinitionCatalog>(CatalogPath);

            DungeonFloorInstanceManager floorManager = systems.GetComponent<DungeonFloorInstanceManager>()
                ?? systems.AddComponent<DungeonFloorInstanceManager>();

            var managerSo = new SerializedObject(floorManager);
            managerSo.FindProperty("useDontDestroyOnLoad").boolValue = false;
            managerSo.FindProperty("floorDefinitions").arraySize = 1;
            managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(0).objectReferenceValue = floorDef;
            Transform floorsRoot = EnsureFloorsRoot(systems, floorManager);
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            RemoveChildFloorsExcept(floorsRoot, DimensionSquareFloorIds.FloorId);
            DungeonFloorInstance instance = EnsureScenePaintedFloor(floorsRoot, floorDef);
            instance.gameObject.SetActive(true);

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
                cam.orthographicSize = 20f;
                if (cam.GetComponent<CameraFollow>() == null)
                    cam.gameObject.AddComponent<CameraFollow>();
            }

            EnsureGameplayUiFromSampleScene(scene);
            EnsureMarkers(instance);
        }

        static DungeonFloorInstance EnsureScenePaintedFloor(Transform floorsRoot, DungeonFloorDefinition floorDef)
        {
            Transform existing = floorsRoot.Find(DimensionSquareFloorIds.FloorId);
            GameObject floorGo = existing != null ? existing.gameObject : new GameObject(DimensionSquareFloorIds.FloorId);
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
            DungeonFloorInstance instance = Object.FindAnyObjectByType<DungeonFloorInstance>();
            if (instance == null)
            {
                Debug.LogError("[DimensionSquare] No DungeonFloorInstance to paint.");
                return;
            }

            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            if (floorMap == null || wallMap == null)
            {
                Debug.LogError("[DimensionSquare] Floor/Wall tilemaps missing.");
                return;
            }

            TileBase[] floorTiles = LoadDcssFloorTiles();
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            if (floorTiles == null || floorTiles.Length == 0 || wallTile == null)
            {
                Debug.LogError("[DimensionSquare] Missing floor or wall tile assets.");
                return;
            }

            Undo.RecordObject(floorMap, "Paint dimension square floor");
            Undo.RecordObject(wallMap, "Paint dimension square walls");
            DimensionSquareLayout.Paint(floorMap, wallMap, floorTiles, wallTile);

            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(floorMap);
            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(wallMap);

            floorMap.CompressBounds();
            wallMap.CompressBounds();
            EditorUtility.SetDirty(floorMap);
            EditorUtility.SetDirty(wallMap);
        }

        static TileBase[] LoadDcssFloorTiles()
        {
            var tiles = new TileBase[DcssFloorTileAssets.Length];
            for (int i = 0; i < DcssFloorTileAssets.Length; i++)
            {
                tiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{DcssFloorTileAssets[i]}");
            }

            return tiles;
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

            var keep = new System.Collections.Generic.HashSet<string>(keepFloorIds);
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

    /// <summary>Shared lighting setup extracted for town scene creators.</summary>
    static class TownPackCreatorLighting
    {
        public static void EnsureLightingOnSystems(GameObject systems)
        {
            LightingService service = systems.GetComponent<LightingService>() ?? systems.AddComponent<LightingService>();
            var serviceSo = new SerializedObject(service);
            serviceSo.FindProperty("defaultFloorAmbientRegionId").intValue = 0;
            serviceSo.FindProperty("defaultFloorAmbientLight").intValue = LightLevel.FullDaylightAmbient;
            serviceSo.FindProperty("verboseReceiveLogs").boolValue = false;
            serviceSo.ApplyModifiedPropertiesWithoutUndo();

            LightingBootstrap bootstrap = systems.GetComponent<LightingBootstrap>() ?? systems.AddComponent<LightingBootstrap>();
            var bootstrapSo = new SerializedObject(bootstrap);
            SerializedProperty regions = bootstrapSo.FindProperty("ambientRegions");
            regions.arraySize = 1;
            SerializedProperty region = regions.GetArrayElementAtIndex(0);
            region.FindPropertyRelative("regionId").intValue = 0;
            region.FindPropertyRelative("currentAmbientLight").intValue = LightLevel.FullDaylightAmbient;
            region.FindPropertyRelative("cycleLengthTurns").intValue = 0;
            region.FindPropertyRelative("phases").arraySize = 0;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
