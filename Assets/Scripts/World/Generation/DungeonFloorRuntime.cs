using JRogue.Manager.Party;
using JRogue.View;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Scene entry point for production dungeon floors — runs the generation pipeline on town portal entry or direct play.
    /// </summary>
    public sealed class DungeonFloorRuntime : MonoBehaviour
    {
        [SerializeField] DungeonRunBootstrap runBootstrap;
        [SerializeField] DungeonFloorInstanceManager floorInstanceManager;
        [SerializeField] DungeonFloorDefinitionCatalog floorCatalog;
        [SerializeField] string startFloorId = DungeonEntryService.StartFloorId;
        [SerializeField] int runSeed = 424242;
        [SerializeField] bool beginRunOnStart = true;
        [SerializeField] bool useRandomSeedOnTownEntry = true;

        void Start()
        {
            if (TryStartFromTownPortal())
                return;

            if (!beginRunOnStart)
                return;

            BeginRun(runSeed);
        }

        bool TryStartFromTownPortal()
        {
            if (!RunPartyPersistence.ConsumeEnteringDungeonFromTown())
                return false;

            EnsureRunBootstrap();
            int seed = useRandomSeedOnTownEntry ? CreateTownEntryRunSeed() : runSeed;
            bool ok = BeginRun(seed);
            if (ok)
            {
                EnsurePlayCamera();
                DungeonGenerationLog.Info("New dungeon expedition started from town portal.");
            }

            return true;
        }

        public bool BeginRun(int seed)
        {
            DungeonFloorInstanceManager manager = ResolveManager();
            if (manager == null)
            {
                DungeonGenerationLog.Error($"{nameof(DungeonFloorRuntime)}: missing {nameof(DungeonFloorInstanceManager)}.");
                return false;
            }

            EnsureFloorCatalog();
            if (floorCatalog != null && floorCatalog.Floors != null && floorCatalog.Floors.Length > 0)
                manager.ConfigureFloors(floorCatalog.Floors);

            string floorId = string.IsNullOrEmpty(startFloorId)
                ? DungeonEntryService.StartFloorId
                : startFloorId;

            if (!manager.TryBeginRunAtFloor(floorId, seed))
            {
                DungeonGenerationLog.Error($"Failed to begin run at '{floorId}'.");
                return false;
            }

            EnsurePlayCamera();
            return true;
        }

        public void ExitDungeon()
        {
            DungeonFloorInstanceManager manager = ResolveManager();
            manager?.ExitDungeon();
        }

        void EnsureRunBootstrap()
        {
            if (runBootstrap == null)
                runBootstrap = FindAnyObjectByType<DungeonRunBootstrap>();

            runBootstrap?.EnsureDungeonRunObjects();
        }

        void EnsureFloorCatalog()
        {
            if (floorCatalog != null)
                return;

            floorCatalog = Resources.Load<DungeonFloorDefinitionCatalog>("Dungeon/DungeonV0aCatalog");
            if (floorCatalog == null)
                DungeonGenerationLog.Warn("Resources/Dungeon/DungeonV0aCatalog not found — run JRogue → Dungeon → Create v0a Test Data.");
        }

        DungeonFloorInstanceManager ResolveManager()
        {
            if (floorInstanceManager != null)
                return floorInstanceManager;

            return DungeonFloorInstanceManager.Instance;
        }

        static int CreateTownEntryRunSeed() =>
            unchecked(System.Environment.TickCount * 397 ^ System.Guid.NewGuid().GetHashCode());

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
    }
}
