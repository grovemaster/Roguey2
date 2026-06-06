using JRogue.Shop;
using UnityEngine;

namespace JRogue.World.Generation
{
    public sealed class DungeonRunState : MonoBehaviour
    {
        public static DungeonRunState Instance { get; private set; }

        [SerializeField] int runSeed = 12345;
        [SerializeField] string activeFloorId;

        public int RunSeed => runSeed;
        public string ActiveFloorId => activeFloorId;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void BeginRun(int seed)
        {
            runSeed = seed;
            activeFloorId = null;
        }

        public void SetActiveFloor(string floorId) => activeFloorId = floorId;

        public void ExitDungeon()
        {
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            manager?.DestroyAllFloors();
            activeFloorId = null;
            TownShopStateService.Instance?.ClearAll();
        }
    }
}
