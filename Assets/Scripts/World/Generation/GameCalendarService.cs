using System;
using JRogue.World.Generation.Phases;
using JRogue.World.Town;
using UnityEngine;

namespace JRogue.World.Generation
{
    public sealed class GameCalendarService : MonoBehaviour
    {
        public const string LogPrefix = "[GameCalendar]";

        public static GameCalendarService Instance { get; private set; }

        [SerializeField] bool calendarEnabled;
        [SerializeField] GameCalendarDate currentDate = GameCalendarLogic.CreateNewRunStartDate();
        [SerializeField] int dungeonPortalIntervalDays = GameCalendarLogic.DefaultPortalIntervalDays;
        [SerializeField] int dungeonPortalStartDay = GameCalendarLogic.DefaultPortalStartDay;
        [SerializeField] int portalReminderShownForAbsoluteDay = int.MinValue;
        [SerializeField] bool runInitialized;

        public bool IsEnabled => calendarEnabled;
        public GameCalendarDate CurrentDate => currentDate;
        public int DungeonPortalIntervalDays => dungeonPortalIntervalDays;
        public int DungeonPortalStartDay => dungeonPortalStartDay;

        public event Action<GameCalendarDate> DateChanged;

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

        public static GameCalendarService EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject(nameof(GameCalendarService));
            return go.AddComponent<GameCalendarService>();
        }

        public void EnsureRunInitialized()
        {
            if (runInitialized)
                return;

            ResetForNewRun();
        }

        public void ResetForNewRun()
        {
            currentDate = GameCalendarLogic.CreateNewRunStartDate();
            portalReminderShownForAbsoluteDay = int.MinValue;
            runInitialized = true;
            Debug.Log($"{LogPrefix} New run — {FormatCurrentDate()}.");
        }

        public void ConfigureAndEnable(int portalIntervalDays, int portalStartDay)
        {
            dungeonPortalIntervalDays = Mathf.Max(1, portalIntervalDays);
            dungeonPortalStartDay = Mathf.Max(1, portalStartDay);
            calendarEnabled = true;
            EnsureRunInitialized();
            Debug.Log(
                $"{LogPrefix} District calendar enabled — portal every {dungeonPortalIntervalDays} days " +
                $"(starting day {dungeonPortalStartDay}).");
            DateChanged?.Invoke(currentDate);
        }

        public bool IsDungeonPortalOpen() =>
            calendarEnabled
            && GameCalendarLogic.IsDungeonPortalDay(
                currentDate,
                dungeonPortalIntervalDays,
                dungeonPortalStartDay);

        public string GetPortalClosedMessage() =>
            GameCalendarLogic.BuildPortalClosedMessage(
                currentDate,
                dungeonPortalIntervalDays,
                dungeonPortalStartDay);

        public string FormatCurrentDate() => GameCalendarLogic.FormatDisplayDate(currentDate);

        public void AdvanceDay(GameCalendarDayAdvanceSource source)
        {
            if (!calendarEnabled)
                return;

            currentDate = GameCalendarLogic.AdvanceOneDay(currentDate);
            Debug.Log($"{LogPrefix} Advanced to {FormatCurrentDate()} (source={source}).");

            RefreshTownPortalVisual();
            DateChanged?.Invoke(currentDate);
            DistrictCalendarShopResetService.TryResetForPostPortalDay(
                currentDate,
                dungeonPortalIntervalDays,
                dungeonPortalStartDay);
            DungeonPortalReminderService.TryShowPortalDayReminder();
        }

        public void OnTownHubFloorActivated(string floorId)
        {
            if (!calendarEnabled || !TownPortalSetupPhase.IsHubFloor(floorId))
                return;

            RefreshTownPortalVisual();
            DungeonPortalReminderService.TryShowPortalDayReminder();
        }

        public bool TryMarkPortalReminderShown()
        {
            int absoluteDay = GameCalendarLogic.ToAbsoluteDayIndex(currentDate);
            if (portalReminderShownForAbsoluteDay == absoluteDay)
                return false;

            portalReminderShownForAbsoluteDay = absoluteDay;
            return true;
        }

        void RefreshTownPortalVisual()
        {
            TownTimeService.Instance?.RefreshTownPortalVisual();
        }
    }
}
