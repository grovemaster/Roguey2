using System;
using JRogue.Manager.Party;
using JRogue.Manager.Progression;
using JRogue.Manager.Turn;
using JRogue.UI.Gameplay;
using JRogue.View;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.World.Generation
{
    public static class DungeonEntryService
    {
        public const string TestDungeonSceneName = "DungeonFloorTest";
        public const string ProductionDungeonSceneName = "DungeonFloor";
        public const string TownTestSceneName = "TownTest";
        public const string StartFloorId = "dungeon_floor_01";

        const string LogPrefix = "[DungeonEntry]";

        static bool _entryScheduled;

        public static bool EntryScheduled => _entryScheduled;

        /// <summary>
        /// Resolves which dungeon scene to load from the active hub/town scene.
        /// <see cref="TownTestSceneName"/> keeps the legacy test pair; all other hubs use production.
        /// </summary>
        public static string ResolveDungeonSceneName(string townSceneName)
        {
            if (string.Equals(townSceneName, TownTestSceneName, StringComparison.Ordinal))
                return TestDungeonSceneName;

            return ProductionDungeonSceneName;
        }

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
            RunPartyPersistence.SetReturnTownSceneName(SceneManager.GetActiveScene().name);
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

            string dungeonSceneName = ResolveDungeonSceneName(RunPartyPersistence.ReturnTownSceneName);

            try
            {
                if (!Application.CanStreamedLevelBeLoaded(dungeonSceneName))
                {
                    Debug.LogError(
                        $"{LogPrefix} Scene '{dungeonSceneName}' is not in Build Settings. " +
                        $"Town '{RunPartyPersistence.ReturnTownSceneName}' → run JRogue/Dungeon/Phase 1 — Setup Production Dungeon.");
                    return;
                }

                Debug.Log(
                    $"{LogPrefix} Loading dungeon scene '{dungeonSceneName}' from town '{RunPartyPersistence.ReturnTownSceneName}' (fresh run).");
                GameLogService.ClearSession();
                SceneManager.LoadScene(dungeonSceneName, LoadSceneMode.Single);
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
