using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Manager.Progression;
using JRogue.Manager.Turn;
using JRogue.Status;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.World.Generation
{
    public static class DungeonExitService
    {
        public const string TownSceneName = "TownTest";

        const string LogPrefix = "[DungeonExit]";

        static bool _exitScheduled;

        public static bool ExitScheduled => _exitScheduled;

        /// <summary>
        /// Pauses the dungeon, shows the end dialog, then loads town after OK.
        /// </summary>
        public static void RequestForcedExitToTown()
        {
            if (_exitScheduled)
                return;

            _exitScheduled = true;
            Debug.Log($"{LogPrefix} Dungeon time expired — showing exit dialog.");

            PauseDungeonForExit();
            ShowDungeonEndedDialog();
        }

        static void ShowDungeonEndedDialog()
        {
            int highestFloor = ResolveHighestFloorReached();
            string body =
                $"Your time in the dungeon is over. You reached Floor {highestFloor} before returning to town.";

            Debug.Log($"{LogPrefix} Forced exit — highest floor {highestFloor}.");
            DungeonEndedDialogUI.EnsureInstance().Show(
                title: "The Dungeon Has Ended",
                bodyMessage: body,
                buttonLabel: "Continue",
                onOk: OnDungeonEndedDialogAcknowledged);
        }

        static int ResolveHighestFloorReached()
        {
            DungeonRunState run = DungeonRunState.Instance;
            if (run != null && run.DeepestFloorNumberReached > 0)
                return run.DeepestFloorNumberReached;

            if (run != null && !string.IsNullOrEmpty(run.ActiveFloorId))
            {
                int active = DungeonRunState.ParseFloorNumber(run.ActiveFloorId);
                if (active > 0)
                    return active;
            }

            return 1;
        }

        static void OnDungeonEndedDialogAcknowledged()
        {
            Debug.Log($"{LogPrefix} Player acknowledged dungeon end — preparing town transition.");
            PrepareSceneTransition();

            DungeonTimeService timeService = DungeonTimeService.Instance;
            if (timeService != null)
                timeService.ScheduleForcedExitCoroutine();
            else
                ExecuteForcedExitAfterFrame();
        }

        internal static void ExecuteForcedExitAfterFrame()
        {
            if (!_exitScheduled)
                return;

            try
            {
                string townSceneName = RunPartyPersistence.ReturnTownSceneName;
                if (!Application.CanStreamedLevelBeLoaded(townSceneName))
                {
                    Debug.LogError(
                        $"{LogPrefix} Scene '{townSceneName}' is not in Build Settings. " +
                        "Add the town hub scene via File → Build Profiles.");
                    return;
                }

                Debug.Log($"{LogPrefix} Loading town scene '{townSceneName}'.");
                GameLogService.ClearSession();
                SceneManager.LoadScene(townSceneName, LoadSceneMode.Single);
            }
            finally
            {
                _exitScheduled = false;
                DungeonEndedDialogUI.ForceClose();
            }
        }

        static void PauseDungeonForExit()
        {
            RestSessionService.CancelForForcedDungeonExit();

            TurnManager turn = TurnManager.Instance;
            if (turn != null)
            {
                turn.StopAllCoroutines();
                turn.currentState = GameState.BUSY;
            }

            DungeonTimeService time = DungeonTimeService.Instance;
            time?.EndDungeonRun();
        }

        static void PrepareSceneTransition()
        {
            ApplySurvivorRules();
            RunPartyPersistence.EnsurePartySurvivesSceneLoad();
            RunPartyPersistence.MarkAwaitingTownArrival();
            RunPartyPersistence.MarkForcedDungeonExpiryPending();

            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            manager?.ExitDungeon();
        }

        public static void ApplySurvivorRules()
        {
            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member.stats == null)
                    continue;

                CharacterStats stats = member.stats;
                stats.currentHP = stats.MaxHP;

                if (HumanClassRules.UsesSoulPower(stats.humanClass))
                    stats.currentSoulPower = stats.MaxSoulPower;

                StatusEffectController statuses = member.GetComponent<StatusEffectController>();
                statuses?.ClearAll();
            }

            Debug.Log($"{LogPrefix} Survivors refreshed (HP, Soul Power, statuses cleared). Inventory unchanged.");
        }
    }
}
