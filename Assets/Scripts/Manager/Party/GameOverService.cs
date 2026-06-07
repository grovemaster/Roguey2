using JRogue.Actors;
using JRogue.Input;
using JRogue.Manager.Turn;
using JRogue.UI.Gameplay;
using JRogue.UI.Inventory;
using UnityEngine;

namespace JRogue.Manager.Party
{
    /// <summary>Terminal game-over state when the main character dies.</summary>
    public static class GameOverService
    {
        const string LogPrefix = "[GameOver]";

        public static bool IsGameOver =>
            TurnManager.Instance != null
            && TurnManager.Instance.currentState == GameState.GAME_OVER;

        public static void TriggerMainCharacterDeath(BaseActor main)
        {
            if (main == null)
                return;

            if (IsGameOver)
            {
                Debug.Log($"{LogPrefix} Ignored — already in game over.");
                return;
            }

            string displayName = main.DisplayName;
            Debug.Log($"{LogPrefix} Main character {main.gameObject.name} ({displayName}) has died. Game over.");

            PartyMemberDeathService.CancelAllPendingRecruitDeaths();

            InputHandler input = Object.FindAnyObjectByType<InputHandler>();
            input?.CommandProcessor.ForceExitTargeting();
            InventoryUI.ForceCloseForGameOver();
            JRogue.UI.Racial.RacialAbilitiesUI.ForceCloseIfOpen();

            if (TurnManager.Instance != null)
                TurnManager.Instance.EnterGameOver();

            GameOverModalUI.EnsureInstance().ShowTerminal(displayName);
        }

#if UNITY_EDITOR
        public static void ResetForTests()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.currentState = GameState.PLAYER_TURN;
        }
#endif
    }
}
