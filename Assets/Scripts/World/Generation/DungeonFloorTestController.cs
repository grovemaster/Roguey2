using JRogue.Manager.Party;
using JRogue.View;
using UnityEngine;

namespace JRogue.World.Generation
{
    [DefaultExecutionOrder(200)]
    public sealed class DungeonFloorTestController : MonoBehaviour
    {
        [SerializeField] DungeonRunBootstrap runBootstrap;
        [SerializeField] DungeonFloorInstanceManager floorInstanceManager;
        [SerializeField] DungeonFloorDefinitionCatalog floorCatalog;
        [SerializeField] string startFloorId = "dungeon_floor_01";
        [SerializeField] int runSeed = 424242;
        [SerializeField] bool autoGenerateOnPlay = true;
        [SerializeField] bool showGenerateButton = true;
        [SerializeField] bool validateSceneOnPlay = true;
        [SerializeField] bool tryRepairSceneAtRuntime = true;

        bool _runStarted;

        void Awake()
        {
            if (validateSceneOnPlay)
            {
                var validator = new DungeonFloorTestSceneValidator();
                if (!validator.ValidateScene(tryRepairSceneAtRuntime))
                    DungeonGenerationLog.Warn("Scene validation failed in Awake; Generate will retry repair.");
            }

            if (runBootstrap != null)
                runBootstrap.EnsureDungeonRunObjects();
        }

        void Start()
        {
            if (!autoGenerateOnPlay || _runStarted)
                return;

            GenerateTestFloor();
        }

        void OnGUI()
        {
            if (!showGenerateButton)
                return;

            const int width = 220;
            const int height = 36;
            var rect = new Rect(12f, 12f, width, height);
            if (GUI.Button(rect, _runStarted ? "Regenerate Test Floor" : "Generate Test Floor"))
                GenerateTestFloor();
        }

        [ContextMenu("Validate Scene")]
        public void ValidateScene() =>
            new DungeonFloorTestSceneValidator().ValidateScene(tryRepairSceneAtRuntime);

        [ContextMenu("Generate Test Floor")]
        public void GenerateTestFloor()
        {
            if (validateSceneOnPlay &&
                !new DungeonFloorTestSceneValidator().ValidateScene(tryRepairSceneAtRuntime))
            {
                DungeonGenerationLog.Error("Generate aborted — fix scene hierarchy (JRogue → Dungeon → Fix DungeonFloorTest Scene).");
                return;
            }

            if (runBootstrap == null)
                runBootstrap = FindAnyObjectByType<DungeonRunBootstrap>();

            if (runBootstrap != null)
                runBootstrap.EnsureDungeonRunObjects();
            else
                DungeonGenerationLog.Warn("DungeonRunBootstrap missing on Party — cannot spawn roster.");

            DungeonFloorInstanceManager manager = floorInstanceManager != null
                ? floorInstanceManager
                : DungeonFloorInstanceManager.Instance;

            if (manager == null)
            {
                DungeonGenerationLog.Error("DungeonFloorInstanceManager missing on DungeonTestSystems.");
                return;
            }

            EnsureFloorCatalog();
            if (floorCatalog != null && floorCatalog.Floors != null && floorCatalog.Floors.Length > 0)
                manager.ConfigureFloors(floorCatalog.Floors);
            else
                DungeonGenerationLog.Warn("No floor catalog — using floorDefinitions on DungeonFloorInstanceManager only.");

            if (RunPartyPersistence.ConsumeEnteringDungeonFromTown())
            {
                if (runBootstrap != null)
                    runBootstrap.EnsureDungeonRunObjects();

                bool ok = manager.TryBeginRunAtFloor(
                    string.IsNullOrEmpty(startFloorId) ? DungeonEntryService.StartFloorId : startFloorId,
                    runSeed);
                _runStarted = ok;
                if (!ok)
                    DungeonGenerationLog.Error("Failed to start fresh dungeon run from town portal.");
                else
                {
                    EnsurePlayCamera();
                    LogHierarchyHint(manager);
                    DungeonGenerationLog.Info("New dungeon expedition started from town.");
                }

                return;
            }

            bool fromForcedExit = RunPartyPersistence.ConsumeAwaitingTownArrival();
            if (fromForcedExit)
            {
                bool ok = TownArrivalService.TryCompleteArrival(manager, startFloorId, runSeed);
                _runStarted = ok;
                if (!ok)
                    DungeonGenerationLog.Error($"Failed town arrival at '{startFloorId}'.");
                else
                    LogHierarchyHint(manager);
                return;
            }

            if (runBootstrap != null)
                runBootstrap.EnsurePartyRoster();

            if (_runStarted)
                runSeed = unchecked(runSeed * 48271 + 1);

            bool started = manager.TryBeginRunAtFloor(startFloorId, runSeed);
            _runStarted = started;
            if (!started)
            {
                DungeonGenerationLog.Error($"Failed to start at '{startFloorId}'.");
                return;
            }

            EnsurePlayCamera();
            LogHierarchyHint(manager);
            DungeonGenerationLog.Info("Floor generated. Regenerate Test Floor rebuilds the run.");
        }

        static void EnsurePlayCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                DungeonGenerationLog.Warn("Main Camera missing.");
                return;
            }

            camera.orthographic = true;
            if (camera.orthographicSize < 8f)
                camera.orthographicSize = 12f;

            if (camera.GetComponent<CameraFollow>() == null)
                camera.gameObject.AddComponent<CameraFollow>();

            PartyManager.Instance?.RefreshCameraFollow();
        }

        static void LogHierarchyHint(DungeonFloorInstanceManager manager)
        {
            if (manager == null)
                return;

            Transform floors = manager.transform.Find("Floors");
            if (floors == null)
            {
                DungeonGenerationLog.Warn("No 'Floors' child under DungeonFloorInstanceManager.");
                return;
            }

            DungeonGenerationLog.Info($"Active floor content under '{manager.gameObject.name}/Floors' ({floors.childCount} child floor instance(s)).");
            for (int i = 0; i < floors.childCount; i++)
            {
                Transform child = floors.GetChild(i);
                Transform enemyContainer = child.Find("EnemyContainer");
                string enemyPath = enemyContainer != null
                    ? $"{child.name}/EnemyContainer"
                    : $"{child.name} (no EnemyContainer)";
                DungeonGenerationLog.Info(
                    $"  └─ {child.name} active={child.gameObject.activeSelf} enemies→{enemyPath}");
            }
        }

        void EnsureFloorCatalog()
        {
            if (floorCatalog != null)
                return;

            floorCatalog = Resources.Load<DungeonFloorDefinitionCatalog>("Dungeon/DungeonV0aCatalog");
            if (floorCatalog == null)
                DungeonGenerationLog.Warn("Resources/Dungeon/DungeonV0aCatalog not found — run Create v0a Test Data.");
        }
    }
}
