using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Scene entry point for production dungeon floors — runs the v0b generation pipeline on first visit.
    /// </summary>
    public sealed class DungeonFloorRuntime : MonoBehaviour
    {
        [SerializeField] DungeonFloorInstanceManager floorInstanceManager;
        [SerializeField] DungeonFloorDefinitionCatalog floorCatalog;
        [SerializeField] string startFloorId = "dungeon_floor_01";
        [SerializeField] int runSeed = 424242;
        [SerializeField] bool beginRunOnStart = true;

        void Start()
        {
            if (!beginRunOnStart)
                return;

            BeginRun();
        }

        public void BeginRun()
        {
            DungeonFloorInstanceManager manager = ResolveManager();
            if (manager == null)
            {
                DungeonGenerationLog.Error($"{nameof(DungeonFloorRuntime)}: missing {nameof(DungeonFloorInstanceManager)}.");
                return;
            }

            if (floorCatalog != null && floorCatalog.Floors != null && floorCatalog.Floors.Length > 0)
                manager.ConfigureFloors(floorCatalog.Floors);

            if (!manager.TryBeginRunAtFloor(startFloorId, runSeed))
                DungeonGenerationLog.Error($"Failed to begin run at '{startFloorId}'.");
        }

        public void ExitDungeon()
        {
            DungeonFloorInstanceManager manager = ResolveManager();
            manager?.ExitDungeon();
        }

        DungeonFloorInstanceManager ResolveManager()
        {
            if (floorInstanceManager != null)
                return floorInstanceManager;

            return DungeonFloorInstanceManager.Instance;
        }
    }
}
