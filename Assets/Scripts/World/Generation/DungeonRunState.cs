using JRogue.Shop;
using JRogue.World.LotF;
using JRogue.World.MapPresence;
using JRogue.World.Rift;
using UnityEngine;

namespace JRogue.World.Generation
{
    public sealed class DungeonRunState : MonoBehaviour
    {
        public static DungeonRunState Instance { get; private set; }

        [SerializeField] int runSeed = 12345;
        [SerializeField] string activeFloorId;
        [SerializeField] int deepestFloorNumberReached;

        public int RunSeed => runSeed;
        public string ActiveFloorId => activeFloorId;
        public int DeepestFloorNumberReached => deepestFloorNumberReached;

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
            deepestFloorNumberReached = 0;
            JRogue.Quest.QuestService.Instance?.ResetForNewRun();
            LordOfTheFloorService.ResetForNewRun();
            MistOfTheAbyssService.ResetForNewRun();
            RiftSessionMeta.EnsureInstance().OnDungeonRunBegun();
            RiftPortalService.OnDungeonRunBegun(null);
            RiftService.ResetForNewRun();
        }

        public void SetActiveFloor(string floorId)
        {
            activeFloorId = floorId;
            RecordFloorVisit(floorId);
        }

        public static int ParseFloorNumber(string floorId)
        {
            if (string.IsNullOrEmpty(floorId))
                return 0;

            int lastUnderscore = floorId.LastIndexOf('_');
            if (lastUnderscore < 0 || lastUnderscore >= floorId.Length - 1)
                return 0;

            string suffix = floorId.Substring(lastUnderscore + 1);
            return int.TryParse(suffix, out int floorNumber) ? floorNumber : 0;
        }

        void RecordFloorVisit(string floorId)
        {
            int floorNumber = ParseFloorNumber(floorId);
            if (floorNumber > deepestFloorNumberReached)
                deepestFloorNumberReached = floorNumber;
        }

        public void ExitDungeon()
        {
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            manager?.DestroyAllFloors();
            activeFloorId = null;
            TownShopStateService.Instance?.ClearAll();
            MistOfTheAbyssService.ResetForNewRun();
        }
    }
}
