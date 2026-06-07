using JRogue.Manager.Progression;
using JRogue.Manager.Turn;
using JRogue.UI.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.World.Generation
{
    public static class DungeonEntryService
    {
        public const string DungeonSceneName = "DungeonFloorTest";
        public const string StartFloorId = "dungeon_floor_01";

        const string LogPrefix = "[DungeonEntry]";

        static bool _entryScheduled;

        public static bool EntryScheduled => _entryScheduled;

        public static void RequestEnterDungeonFromTown()
        {
            if (_entryScheduled || DungeonExitService.ExitScheduled)
                return;

            string body =
                "You are about to enter the dungeon.\n\n" +
                "A <b>new expedition</b> begins — the dungeon is fully recreated and " +
                "your day–night cycle limit resets.\n\n" +
                "<size=14><color=#9bbdff>Enter</color> to continue   ·   " +
                "<color=#ffb28a>Stay</color> to remain in town</size>";

            EnterDungeonDialogUI.EnsureInstance().Show(body, OnEnterDungeonConfirmed);
        }

        static void OnEnterDungeonConfirmed()
        {
            if (_entryScheduled)
                return;

            _entryScheduled = true;
            Debug.Log($"{LogPrefix} Player confirmed dungeon entry.");

            PauseTownForExit();
            RunPartyPersistence.EnsurePartySurvivesSceneLoad();
            RunPartyPersistence.MarkEnteringDungeonFromTown();

            DungeonTimeService time = DungeonTimeService.Instance;
            if (time != null)
                time.ScheduleDungeonEntryCoroutine();
            else
                ExecuteDungeonEntryAfterFrame();
        }

        internal static void ExecuteDungeonEntryAfterFrame()
        {
            if (!_entryScheduled)
                return;

            try
            {
                if (!Application.CanStreamedLevelBeLoaded(DungeonSceneName))
                {
                    Debug.LogError(
                        $"{LogPrefix} Scene '{DungeonSceneName}' is not in Build Settings. " +
                        "Add Assets/Scenes/Dungeon/DungeonFloorTest.unity.");
                    return;
                }

                Debug.Log($"{LogPrefix} Loading dungeon scene '{DungeonSceneName}' (fresh run).");
                GameLogService.ClearSession();
                SceneManager.LoadScene(DungeonSceneName, LoadSceneMode.Single);
            }
            finally
            {
                _entryScheduled = false;
                EnterDungeonDialogUI.ForceClose();
            }
        }

        static void PauseTownForExit()
        {
            RestSessionService.CancelForForcedDungeonExit();

            TurnManager turn = TurnManager.Instance;
            if (turn != null)
            {
                turn.StopAllCoroutines();
                turn.currentState = GameState.BUSY;
            }
        }
    }
}
