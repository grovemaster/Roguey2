using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Enables the global game calendar for DistrictTownTest (date HUD, portal cadence, inn rest, dungeon return).
    /// </summary>
    public sealed class DistrictTownCalendarBootstrap : MonoBehaviour
    {
        [SerializeField] int dungeonPortalIntervalDays = GameCalendarLogic.DefaultPortalIntervalDays;
        [SerializeField] int dungeonPortalStartDay = GameCalendarLogic.DefaultPortalStartDay;

        void Awake()
        {
            GameCalendarService calendar = GameCalendarService.EnsureInstance();
            calendar.ConfigureAndEnable(dungeonPortalIntervalDays, dungeonPortalStartDay);
            GameCalendarHudUI.EnsureInstance();
        }
    }
}
