#if UNITY_EDITOR
using System.IO;
using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Data.Progression;
using JRogue.Input;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Spawn;
using JRogue.UI.Targeting;
using JRogue.View;
using JRogue.World.Generation;
using JRogue.World.Lighting;
using JRogue.World.MapInteract;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    public static class DungeonV0aPackCreator
    {
        const string DataRoot = "Assets/Resources/Dungeon";
        const string SceneFolder = "Assets/Scenes/Dungeon";
        const string ScenePath = SceneFolder + "/DungeonFloorTest.unity";
        const string FloorTilePath = "Assets/TileMaps/Scavengers_SpriteSheet_50.asset";
        const string WallTilePath = "Assets/TileMaps/Scavengers_SpriteSheet_32.asset";
        const string EnemyPrefabPath = "Assets/Prefabs/Actor/Enemy/Enemy.prefab";
        const string BarbarianPrefabPath = "Assets/Prefabs/Actor/Race/BarbarianPlayer.prefab";
        const string HumanPrefabPath = "Assets/Prefabs/Actor/Race/HumanPlayer.prefab";
        const string ElfPrefabPath = "Assets/Prefabs/Actor/Race/ElfPlayer.prefab";
        const string GameControlsPath = "Assets/Controls/GameControls.inputactions";
        const string ExperienceCurvePath = "Assets/Resources/Progression/DefaultExperienceCurve.asset";
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("JRogue/Dungeon/Create v0a Test Data")]
        public static void CreateV0aTestData() => CreateV0aTestDataInternal();

        [MenuItem("JRogue/Dungeon/Create DungeonFloorTest Scene")]
        public static void CreateDungeonFloorTestScene()
        {
            CreateV0aTestDataInternal();
            CreateOrUpdateTestScene();
        }

        [MenuItem("JRogue/Dungeon/Fix DungeonFloorTest Scene")]
        public static void FixDungeonFloorTestScene()
        {
            CreateV0aTestDataInternal();
            if (!File.Exists(ScenePath))
            {
                CreateOrUpdateTestScene();
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            FixSceneHierarchyInPlace();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Dungeon] Fixed {ScenePath}. Save scene and press Play.");
        }

        static void FixSceneHierarchyInPlace()
        {
            GameObject systems = GameObject.Find(DungeonFloorTestSceneValidator.SystemsObjectName);
            if (systems == null)
            {
                systems = new GameObject(DungeonFloorTestSceneValidator.SystemsObjectName);
                systems.AddComponent<GridManager>();
                systems.AddComponent<MapManager>();
                TurnManager turn = systems.AddComponent<TurnManager>();
                turn.currentState = GameState.PLAYER_TURN;
                systems.AddComponent<AdjacentMapInteractableService>();
            }

            if (systems.GetComponent<VisibilityManager>() == null)
                systems.AddComponent<VisibilityManager>();

            EnsureLightingOnSystems(systems);
            EnsureGameplayUiFromSampleScene(EditorSceneManager.GetActiveScene());

            DungeonFloorInstanceManager floorManager = systems.GetComponent<DungeonFloorInstanceManager>();
            if (floorManager == null)
                floorManager = systems.AddComponent<DungeonFloorInstanceManager>();

            SerializedObject managerSo = new SerializedObject(floorManager);
            managerSo.FindProperty("useDontDestroyOnLoad").boolValue = false;
            if (managerSo.FindProperty("floorDefinitions").arraySize == 0)
            {
                managerSo.FindProperty("floorDefinitions").arraySize = 2;
                managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(0).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>($"{DataRoot}/Floor_dungeon_floor_01.asset");
                managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(1).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>($"{DataRoot}/Floor_dungeon_floor_02.asset");
            }
            Transform floorsRoot = EnsureFloorsRoot(systems, floorManager);
            EnsureFloorScaffold(floorsRoot, "dungeon_floor_01",
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>($"{DataRoot}/Floor_dungeon_floor_01.asset"));
            EnsureFloorScaffold(floorsRoot, "dungeon_floor_02",
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>($"{DataRoot}/Floor_dungeon_floor_02.asset"));
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            if (systems.GetComponent<DungeonFloorTestController>() == null)
                systems.AddComponent<DungeonFloorTestController>();

            GameObject inputRoot = GameObject.Find(DungeonFloorTestSceneValidator.InputObjectName);
            GameObject party = GameObject.Find(DungeonFloorTestSceneValidator.PartyObjectName);
            PartyManager partyManagerOnParty = party != null ? party.GetComponent<PartyManager>() : null;

            if (inputRoot == null)
            {
                inputRoot = new GameObject(DungeonFloorTestSceneValidator.InputObjectName);
                Undo.RegisterCreatedObjectUndo(inputRoot, "Create InputSystem");
            }

            EnsureComponent<TargetingReticleView>(inputRoot);
            EnsureComponent<InputHandler>(inputRoot);
            PlayerInput playerInput = EnsureComponent<PlayerInput>(inputRoot);
            WirePlayerInput(playerInput);

            PartyManager partyManager = inputRoot.GetComponent<PartyManager>();
            if (partyManagerOnParty != null)
            {
                if (partyManager == null)
                {
                    partyManager = Undo.AddComponent<PartyManager>(inputRoot);
                    EditorUtility.CopySerialized(partyManagerOnParty, partyManager);
                }

                Undo.DestroyObjectImmediate(partyManagerOnParty);
            }

            if (partyManager == null)
                partyManager = inputRoot.AddComponent<PartyManager>();

            SerializedObject partySo = new SerializedObject(partyManager);
            if (partySo.FindProperty("experienceCurve").objectReferenceValue == null)
            {
                partySo.FindProperty("experienceCurve").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<ExperienceCurve>(ExperienceCurvePath);
            }
            partySo.ApplyModifiedPropertiesWithoutUndo();

            if (party == null)
            {
                party = new GameObject(DungeonFloorTestSceneValidator.PartyObjectName);
                Undo.RegisterCreatedObjectUndo(party, "Create Party");
            }

            if (party.GetComponent<PartyManager>() != null && party.GetComponent<PartyManager>() != partyManager)
                Object.DestroyImmediate(party.GetComponent<PartyManager>());

            DungeonRunBootstrap bootstrap = EnsureComponent<DungeonRunBootstrap>(party);
            SerializedObject bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("partyContainer").objectReferenceValue = party.transform;
            bootstrapSo.FindProperty("floorInstanceManager").objectReferenceValue = floorManager;
            if (bootstrapSo.FindProperty("partyMemberPrefabs").arraySize == 0)
            {
                bootstrapSo.FindProperty("partyMemberPrefabs").arraySize = 3;
                bootstrapSo.FindProperty("partyMemberPrefabs").GetArrayElementAtIndex(0).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(BarbarianPrefabPath);
                bootstrapSo.FindProperty("partyMemberPrefabs").GetArrayElementAtIndex(1).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
                bootstrapSo.FindProperty("partyMemberPrefabs").GetArrayElementAtIndex(2).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(ElfPrefabPath);
            }
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            DungeonFloorTestController test = systems.GetComponent<DungeonFloorTestController>();
            SerializedObject testSo = new SerializedObject(test);
            testSo.FindProperty("runBootstrap").objectReferenceValue = bootstrap;
            testSo.FindProperty("floorInstanceManager").objectReferenceValue = floorManager;
            testSo.FindProperty("floorCatalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinitionCatalog>($"{DataRoot}/DungeonV0aCatalog.asset");
            testSo.FindProperty("tryRepairSceneAtRuntime").boolValue = true;
            testSo.FindProperty("autoGenerateOnPlay").boolValue = true;
            testSo.ApplyModifiedPropertiesWithoutUndo();

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                if (cam.orthographicSize < 8f)
                    cam.orthographicSize = 12f;
                if (cam.GetComponent<CameraFollow>() == null)
                    cam.gameObject.AddComponent<CameraFollow>();
            }
        }

        static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing != null ? existing : Undo.AddComponent<T>(go);
        }

        static void EnsureLightingOnSystems(GameObject systems)
        {
            LightingService service = EnsureComponent<LightingService>(systems);
            SerializedObject serviceSo = new SerializedObject(service);
            serviceSo.FindProperty("defaultFloorAmbientRegionId").intValue = 0;
            serviceSo.FindProperty("defaultFloorAmbientLight").intValue = LightLevel.FullDaylightAmbient;
            serviceSo.FindProperty("verboseReceiveLogs").boolValue = false;
            serviceSo.ApplyModifiedPropertiesWithoutUndo();

            LightingBootstrap bootstrap = EnsureComponent<LightingBootstrap>(systems);
            SerializedObject bootstrapSo = new SerializedObject(bootstrap);
            SerializedProperty regions = bootstrapSo.FindProperty("ambientRegions");
            regions.arraySize = 1;
            SerializedProperty region = regions.GetArrayElementAtIndex(0);
            region.FindPropertyRelative("regionId").intValue = 0;
            region.FindPropertyRelative("currentAmbientLight").intValue = LightLevel.FullDaylightAmbient;
            region.FindPropertyRelative("cycleLengthTurns").intValue = 0;
            region.FindPropertyRelative("phases").arraySize = 0;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureGameplayUiFromSampleScene(Scene targetScene)
        {
            if (SceneHasRootNamed(targetScene, "Canvas") && SceneHasRootNamed(targetScene, "EventSystem"))
                return;

            if (!File.Exists(SampleScenePath))
            {
                Debug.LogWarning($"[Dungeon] Cannot copy UI — missing {SampleScenePath}.");
                return;
            }

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
                    Undo.RegisterCreatedObjectUndo(copy, "Copy " + root.name);
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

        static Transform EnsureFloorsRoot(GameObject systems, DungeonFloorInstanceManager floorManager)
        {
            Transform floors = systems.transform.Find("Floors");
            if (floors == null)
            {
                var floorsGo = new GameObject("Floors");
                Undo.RegisterCreatedObjectUndo(floorsGo, "Create Floors");
                floorsGo.transform.SetParent(systems.transform, false);
                floors = floorsGo.transform;
            }

            SerializedObject managerSo = new SerializedObject(floorManager);
            managerSo.FindProperty("floorsRoot").objectReferenceValue = floors;
            managerSo.ApplyModifiedPropertiesWithoutUndo();
            return floors;
        }

        static void EnsureFloorScaffold(Transform floorsRoot, string floorId, DungeonFloorDefinition definition)
        {
            if (floorsRoot == null || string.IsNullOrEmpty(floorId))
                return;

            Transform existing = floorsRoot.Find(floorId);
            GameObject floorGo;
            if (existing != null)
                floorGo = existing.gameObject;
            else
            {
                floorGo = new GameObject(floorId);
                Undo.RegisterCreatedObjectUndo(floorGo, "Create " + floorId);
                floorGo.transform.SetParent(floorsRoot, false);
            }

            DungeonFloorInstance instance = floorGo.GetComponent<DungeonFloorInstance>();
            if (instance == null)
                instance = Undo.AddComponent<DungeonFloorInstance>(floorGo);

            if (definition != null)
            {
                instance.Configure(definition);
                instance.EnsureHierarchyBuilt();
            }

            floorGo.SetActive(false);
        }

        static void WirePlayerInput(PlayerInput playerInput)
        {
            if (playerInput == null)
                return;

            SerializedObject so = new SerializedObject(playerInput);
            so.FindProperty("m_Actions").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(GameControlsPath);
            so.FindProperty("m_NotificationBehavior").enumValueIndex = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateV0aTestDataInternal()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(DataRoot);
            EnsureFolder(SceneFolder);

            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>(FloorTilePath);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            EnemyController enemyPrefab = AssetDatabase.LoadAssetAtPath<EnemyController>(EnemyPrefabPath);

            DungeonLayoutStamp stampF1 = CreateStamp(
                $"{DataRoot}/Stamp_Floor01_30x30.asset",
                30,
                30,
                playerStart: new Vector3Int(15, 8, 0),
                portalMarker: StampMarkerIds.PortalSouth,
                portalCell: new Vector3Int(15, 1, 0));

            DungeonLayoutStamp stampF2 = CreateStamp(
                $"{DataRoot}/Stamp_Floor02_20x20.asset",
                20,
                20,
                playerStart: new Vector3Int(10, 5, 0),
                portalMarker: StampMarkerIds.PortalNorth,
                portalCell: new Vector3Int(10, 18, 0));

            PartyFormationSpawnProfile formation = LoadOrCreate<PartyFormationSpawnProfile>(
                $"{DataRoot}/PartyFormation_Default.asset");
            formation.EnsureDefaultLayouts();
            EditorUtility.SetDirty(formation);

            var spawnDef = LoadOrCreate<EnemySpawnDefinition>($"{DataRoot}/Spawn_DungeonTestSkeleton.asset");
            spawnDef.enemyPrefab = enemyPrefab;
            spawnDef.placementPolicy =
                EnemySpawnPlacementPolicy.NorthOfOriginThenNearestUnoccupiedFloor;
            spawnDef.primaryOffset = new Vector3Int(0, 1, 0);
            EditorUtility.SetDirty(spawnDef);

            DungeonFloorDefinition floor01 = CreateFloorDefinition(
                $"{DataRoot}/Floor_dungeon_floor_01.asset",
                "dungeon_floor_01",
                stampF1,
                floorTile,
                wallTile,
                formation,
                spawnDef,
                enemyMin: 4,
                enemyMax: 6,
                portals: new[]
                {
                    new DungeonPortalSpec
                    {
                        portalLinkId = "link_floor01_to_floor02",
                        targetFloorId = "dungeon_floor_02",
                        portalMarkerId = StampMarkerIds.PortalSouth,
                        listLabel = "Portal (South)",
                    },
                },
                arrivals: new[]
                {
                    new PortalArrivalBinding
                    {
                        portalLinkId = "link_floor02_to_floor01",
                        arrivalAnchor = new Vector3Int(15, 17, 0),
                    },
                });

            DungeonFloorDefinition floor02 = CreateFloorDefinition(
                $"{DataRoot}/Floor_dungeon_floor_02.asset",
                "dungeon_floor_02",
                stampF2,
                floorTile,
                wallTile,
                formation,
                spawnDef,
                enemyMin: 3,
                enemyMax: 5,
                portals: new[]
                {
                    new DungeonPortalSpec
                    {
                        portalLinkId = "link_floor02_to_floor01",
                        targetFloorId = "dungeon_floor_01",
                        portalMarkerId = StampMarkerIds.PortalNorth,
                        listLabel = "Portal (North)",
                    },
                },
                arrivals: new[]
                {
                    new PortalArrivalBinding
                    {
                        portalLinkId = "link_floor01_to_floor02",
                        arrivalAnchor = new Vector3Int(10, 2, 0),
                    },
                });

            var catalog = LoadOrCreate<DungeonFloorDefinitionCatalog>($"{DataRoot}/DungeonV0aCatalog.asset");
            SerializedObject catalogSo = new SerializedObject(catalog);
            catalogSo.FindProperty("floors").arraySize = 2;
            catalogSo.FindProperty("floors").GetArrayElementAtIndex(0).objectReferenceValue = floor01;
            catalogSo.FindProperty("floors").GetArrayElementAtIndex(1).objectReferenceValue = floor02;
            catalogSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Dungeon] v0a data at {DataRoot}. Floors: {floor01.FloorId}, {floor02.FloorId}");
        }

        static void CreateOrUpdateTestScene()
        {
            EnsureFolder(SceneFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject camera = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camera.tag = "MainCamera";
            Camera cam = camera.GetComponent<Camera>();
            if (cam == null)
                cam = camera.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 12f;
            if (camera.GetComponent<CameraFollow>() == null)
                camera.AddComponent<CameraFollow>();

            GameObject systems = new GameObject(DungeonFloorTestSceneValidator.SystemsObjectName);
            systems.AddComponent<GridManager>();
            systems.AddComponent<MapManager>();
            TurnManager turn = systems.AddComponent<TurnManager>();
            turn.currentState = GameState.PLAYER_TURN;
            systems.AddComponent<AdjacentMapInteractableService>();
            systems.AddComponent<VisibilityManager>();
            EnsureLightingOnSystems(systems);

            DungeonFloorInstanceManager floorManager = systems.AddComponent<DungeonFloorInstanceManager>();
            SerializedObject managerSo = new SerializedObject(floorManager);
            managerSo.FindProperty("useDontDestroyOnLoad").boolValue = false;
            managerSo.FindProperty("floorDefinitions").arraySize = 2;
            managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(0).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>($"{DataRoot}/Floor_dungeon_floor_01.asset");
            managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(1).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>($"{DataRoot}/Floor_dungeon_floor_02.asset");
            Transform floorsRoot = EnsureFloorsRoot(systems, floorManager);
            EnsureFloorScaffold(floorsRoot, "dungeon_floor_01",
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>($"{DataRoot}/Floor_dungeon_floor_01.asset"));
            EnsureFloorScaffold(floorsRoot, "dungeon_floor_02",
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>($"{DataRoot}/Floor_dungeon_floor_02.asset"));
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject inputRoot = new GameObject(DungeonFloorTestSceneValidator.InputObjectName);
            inputRoot.AddComponent<TargetingReticleView>();
            inputRoot.AddComponent<InputHandler>();
            PlayerInput playerInput = inputRoot.AddComponent<PlayerInput>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(GameControlsPath);
            if (actions != null)
            {
                SerializedObject inputSo = new SerializedObject(playerInput);
                inputSo.FindProperty("m_Actions").objectReferenceValue = actions;
                inputSo.FindProperty("m_NotificationBehavior").enumValueIndex = 2;
                inputSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PartyManager party = inputRoot.AddComponent<PartyManager>();
            SerializedObject partySo = new SerializedObject(party);
            partySo.FindProperty("experienceCurve").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ExperienceCurve>(ExperienceCurvePath);
            partySo.ApplyModifiedPropertiesWithoutUndo();

            GameObject partyRoot = new GameObject(DungeonFloorTestSceneValidator.PartyObjectName);
            DungeonRunBootstrap bootstrap = partyRoot.AddComponent<DungeonRunBootstrap>();
            SerializedObject bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("applyOnAwake").boolValue = true;
            bootstrapSo.FindProperty("partyContainer").objectReferenceValue = partyRoot.transform;
            bootstrapSo.FindProperty("partyMemberPrefabs").arraySize = 3;
            bootstrapSo.FindProperty("partyMemberPrefabs").GetArrayElementAtIndex(0).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(BarbarianPrefabPath);
            bootstrapSo.FindProperty("partyMemberPrefabs").GetArrayElementAtIndex(1).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
            bootstrapSo.FindProperty("partyMemberPrefabs").GetArrayElementAtIndex(2).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(ElfPrefabPath);
            bootstrapSo.FindProperty("floorInstanceManager").objectReferenceValue = floorManager;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            EnsureGameplayUiFromSampleScene(scene);

            DungeonFloorTestController test = systems.AddComponent<DungeonFloorTestController>();
            SerializedObject testSo = new SerializedObject(test);
            testSo.FindProperty("runBootstrap").objectReferenceValue = bootstrap;
            testSo.FindProperty("floorInstanceManager").objectReferenceValue = floorManager;
            testSo.FindProperty("floorCatalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinitionCatalog>($"{DataRoot}/DungeonV0aCatalog.asset");
            testSo.FindProperty("startFloorId").stringValue = "dungeon_floor_01";
            testSo.FindProperty("autoGenerateOnPlay").boolValue = true;
            testSo.FindProperty("validateSceneOnPlay").boolValue = true;
            testSo.FindProperty("tryRepairSceneAtRuntime").boolValue = false;
            testSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[Dungeon] Saved {ScenePath}. Hierarchy: Main Camera, {DungeonFloorTestSceneValidator.SystemsObjectName}, " +
                $"{DungeonFloorTestSceneValidator.InputObjectName}, {DungeonFloorTestSceneValidator.PartyObjectName}.");
        }

        static DungeonLayoutStamp CreateStamp(
            string path,
            int width,
            int height,
            Vector3Int playerStart,
            string portalMarker,
            Vector3Int portalCell)
        {
            var stamp = LoadOrCreate<DungeonLayoutStamp>(path);
            stamp.InitializeGrid(width, height, borderWalls: true);
            stamp.SetMarker(StampMarkerIds.PlayerStart, playerStart);
            stamp.SetCell(portalCell.x, portalCell.y, floor: true, wall: false);
            stamp.SetMarker(portalMarker, portalCell);
            EditorUtility.SetDirty(stamp);
            return stamp;
        }

        static DungeonFloorDefinition CreateFloorDefinition(
            string path,
            string floorId,
            DungeonLayoutStamp stamp,
            TileBase floorTile,
            TileBase wallTile,
            PartyFormationSpawnProfile formation,
            EnemySpawnDefinition spawnDef,
            int enemyMin,
            int enemyMax,
            DungeonPortalSpec[] portals,
            PortalArrivalBinding[] arrivals)
        {
            var def = LoadOrCreate<DungeonFloorDefinition>(path);
            SerializedObject so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = floorId;
            so.FindProperty("layoutStamp").objectReferenceValue = stamp;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("playerSafeRadius").intValue = 5;
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("enemyPopulation").arraySize = 1;
            SerializedProperty entry = so.FindProperty("enemyPopulation").GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("spawnDefinition").objectReferenceValue = spawnDef;
            entry.FindPropertyRelative("minCount").intValue = enemyMin;
            entry.FindPropertyRelative("maxCount").intValue = enemyMax;

            SerializedProperty portalProp = so.FindProperty("portals");
            portalProp.arraySize = portals.Length;
            for (int i = 0; i < portals.Length; i++)
            {
                SerializedProperty element = portalProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("portalLinkId").stringValue = portals[i].portalLinkId;
                element.FindPropertyRelative("targetFloorId").stringValue = portals[i].targetFloorId;
                element.FindPropertyRelative("portalMarkerId").stringValue = portals[i].portalMarkerId;
                element.FindPropertyRelative("portalCell").vector3IntValue = portals[i].portalCell;
                element.FindPropertyRelative("listLabel").stringValue = portals[i].listLabel;
            }

            SerializedProperty arrivalProp = so.FindProperty("arrivalBindings");
            arrivalProp.arraySize = arrivals.Length;
            for (int i = 0; i < arrivals.Length; i++)
            {
                SerializedProperty element = arrivalProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("portalLinkId").stringValue = arrivals[i].portalLinkId;
                element.FindPropertyRelative("arrivalAnchor").vector3IntValue = arrivals[i].arrivalAnchor;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
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
