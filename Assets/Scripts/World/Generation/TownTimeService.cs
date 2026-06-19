using JRogue.Interactables;
using UnityEngine;

namespace JRogue.World.Generation
{
    public sealed class TownTimeService : MonoBehaviour
    {
        public const string LogPrefix = "[TownTime]";

        public static TownTimeService Instance { get; private set; }

        public static InteractableTileId TownTimeLeverA => InteractableTileId.TownTimeLeverA;
        public static InteractableTileId TownTimeLeverB => InteractableTileId.TownTimeLeverB;

        [SerializeField] int calendarDayIndex = 1;
        [SerializeField] TownTimePhase currentPhase = TownTimePhase.Morning;
        [SerializeField] InteractableTileId activeTimeLeverId = InteractableTileId.None;
        [SerializeField] int totalPhaseAdvances;
        [SerializeField] bool runInitialized;

        public int CalendarDayIndex => calendarDayIndex;
        public TownTimePhase CurrentPhase => currentPhase;
        public InteractableTileId ActiveTimeLeverId => activeTimeLeverId;
        public int TotalPhaseAdvances => totalPhaseAdvances;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureRunInitialized();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static TownTimeService EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject(nameof(TownTimeService));
            return go.AddComponent<TownTimeService>();
        }

        public static void EnsureRunService()
        {
            EnsureInstance().EnsureRunInitialized();
        }

        void EnsureRunInitialized()
        {
            if (runInitialized)
                return;

            ResetForNewRun();
        }

        public void ResetForNewRun()
        {
            calendarDayIndex = 1;
            currentPhase = TownTimePhase.Morning;
            activeTimeLeverId = InteractableTileId.None;
            totalPhaseAdvances = 0;
            runInitialized = true;

            Debug.Log($"{LogPrefix} New run — day {calendarDayIndex}, {currentPhase}.");
        }

        public bool IsDungeonPortalOpen()
        {
            GameCalendarService calendar = GameCalendarService.Instance;
            if (calendar != null && calendar.IsEnabled)
                return calendar.IsDungeonPortalOpen();

            return TownTimeLogic.IsDungeonPortalOpen(calendarDayIndex, currentPhase);
        }

        public string GetPortalClosedMessage()
        {
            GameCalendarService calendar = GameCalendarService.Instance;
            if (calendar != null && calendar.IsEnabled)
                return calendar.GetPortalClosedMessage();

            return TownTimeLogic.BuildPortalClosedMessage(calendarDayIndex, currentPhase);
        }

        public bool TryAdvancePhase(TownPhaseAdvanceSource source)
        {
            TownTimeAdvanceResult result = TownTimeLogic.AdvancePhase(
                calendarDayIndex,
                currentPhase,
                out int newDay,
                out TownTimePhase newPhase);

            if (!result.Advanced)
                return false;

            TownTimePhase previousPhase = currentPhase;
            currentPhase = newPhase;
            calendarDayIndex = newDay;
            totalPhaseAdvances++;

            Debug.Log(
                $"{LogPrefix} Phase {previousPhase}→{currentPhase} (day {calendarDayIndex}, source={source}).");

            if (result.CalendarDayChanged)
                Debug.Log($"{LogPrefix} Calendar day advanced to {calendarDayIndex}.");

            if (result.PortalWindowClosed)
                Debug.Log($"{LogPrefix} Portal window closed — morning ended on day {calendarDayIndex}.");

            RefreshTownPortalVisual();
            JRogue.World.Lighting.TownLightingSync.ApplyForPhase(currentPhase);
            return true;
        }

        public void ApplyDungeonReturnPhase()
        {
            EnsureRunInitialized();

            if (currentPhase == TownTimePhase.Day)
            {
                RefreshTownPortalVisual();
                JRogue.World.Lighting.TownLightingSync.ApplyForPhase(currentPhase);
                return;
            }

            TownTimePhase previousPhase = currentPhase;
            currentPhase = TownTimePhase.Day;
            Debug.Log(
                $"{LogPrefix} Dungeon return — phase set to {currentPhase} (day {calendarDayIndex}, was {previousPhase}).");
            RefreshTownPortalVisual();
            JRogue.World.Lighting.TownLightingSync.ApplyForPhase(currentPhase);
        }

        public void OnTimeLeverActivated(InteractableTileId leverId, InteractableTileService service)
        {
            if (leverId != TownTimeLeverA && leverId != TownTimeLeverB)
                return;

            TryAdvancePhase(TownPhaseAdvanceSource.TimeLever);
            activeTimeLeverId = leverId;
            SyncTimeLeverVisuals(service);
        }

        public void SyncTimeLeverVisuals(InteractableTileService service = null)
        {
            service ??= InteractableTileService.Instance;
            if (service == null)
                return;

            service.ForceSetLeverState(TownTimeLeverA, activeTimeLeverId == TownTimeLeverA);
            service.ForceSetLeverState(TownTimeLeverB, activeTimeLeverId == TownTimeLeverB);
        }

        public void OnTownFloorActivated(DungeonFloorInstance instance)
        {
            EnsureRunInitialized();
            SyncTimeLeverVisuals();
            RefreshTownPortalVisual(instance);

            string floorId = instance?.Definition?.FloorId;
            GameCalendarService.Instance?.OnTownHubFloorActivated(floorId);

            if (floorId == Phases.TownTorchSetupPhase.TownFloorId)
                JRogue.World.Lighting.TownLightingSync.ApplyForCurrentPhase();
        }

        public void RefreshTownPortalVisual(DungeonFloorInstance instance = null)
        {
            if (instance == null)
            {
                DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
                if (manager == null || !manager.TryGetActiveTownFloor(out instance))
                    return;
            }

            instance.RefreshTownDungeonPortalVisual(IsDungeonPortalOpen());
            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            instance.ApplyPortalVisibility(visibility);
        }
    }
}
