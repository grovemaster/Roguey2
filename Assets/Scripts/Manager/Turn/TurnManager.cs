using System.Collections;
using System.Collections.Generic;
using JRogue.Controller.Enemy;
using JRogue.Manager.Combat;
using JRogue.Manager.Essence;
using JRogue.Manager.Party;
using JRogue.Hazards;
using JRogue.Traps;
using JRogue.Racial;
using JRogue.Status;
using UnityEngine;
public enum GameState { PLAYER_TURN, ENEMY_TURN, BUSY }

namespace JRogue.Manager.Turn
{
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance; // Singleton for easy access
        public GameState currentState;

        // Tracks which party members have acted this turn
        private HashSet<GameObject> charactersWhoActed = new HashSet<GameObject>();

        void Awake()
        {
            // Simple Singleton pattern
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // The game starts waiting for the player
            currentState = GameState.PLAYER_TURN;
        }

        public void OnPlayerActionComplete(GameObject actor)
        {
            if (currentState != GameState.PLAYER_TURN)
            {
                return;
            }

            // Prevent double-counting if an actor is manually moved then Rushed
            if (charactersWhoActed.Contains(actor))
            {
                Debug.Log($"[TurnManager] {actor.name} has already acted this turn. Skipping.");
                return;
            }

            // DEBUG: Print who is acting and how many are in the list
            Debug.Log($"[TurnManager] {actor.name} has acted. Total acted: {charactersWhoActed.Count + 1}");

            // Mark this specific character as done
            charactersWhoActed.Add(actor);

            FindAnyObjectByType<VisibilityManager>()?.RefreshPartyVision();
            CombatThreatCoordinator.Instance?.EvaluateThreat();

            // Check if the WHOLE party is done
            if (IsPartyDone())
            {
                // Switch to Enemy Turn
                StartCoroutine(EnemyTurnSequence());
            }
            // Switch to Enemy Turn
            //StartCoroutine(EnemyTurnSequence());
        }

        public void ForceEndPlayerTurn()
        {
            if (currentState == GameState.PLAYER_TURN)
            {
                StartCoroutine(EnemyTurnSequence());
            }
        }

        private IEnumerator EnemyTurnSequence()
        {
            currentState = GameState.ENEMY_TURN;

            // Clear the set for the next player turn
            charactersWhoActed.Clear();

            // Find all enemies (In the future, use a List for better performance)
            EnemyController[] enemies = FindObjectsByType<EnemyController>();

            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.TakeTurn();
                    yield return new WaitForSeconds(0.05f); // Slight delay for visual clarity
                }
            }

            CombatThreatCoordinator.Instance?.ApplyPursuitDecayAfterEnemyWave();
            CombatThreatCoordinator.Instance?.EvaluateThreat();

            currentState = GameState.PLAYER_TURN;
            Debug.Log("--- New Player Turn ---");
            NotifyPartyTurnStart();
        }

        private void NotifyPartyTurnStart()
        {
            HazardService.Instance?.TickOccupancyOnPlayerPhaseStart();
            TrapService.Instance?.EvaluateDetection();

            if (PartyManager.Instance == null) return;
            foreach (var member in PartyManager.Instance.partyMembers)
            {
                if (member == null) continue;
                RacialPassiveHooks.NotifyTurnStart(member.gameObject);
                var slots = member.GetComponent<EssenceSlotManager>();
                if (slots != null) slots.NotifyTurnStart();
                var statuses = member.GetComponent<StatusEffectController>();
                if (statuses != null) statuses.TickStatuses();
            }

            FindAnyObjectByType<VisibilityManager>()?.RefreshPartyVision();
        }

        private bool IsPartyDone()
        {
            var party = PartyManager.Instance.partyMembers;
            foreach (var member in party)
            {
                if (!charactersWhoActed.Contains(member.gameObject)) return false;
            }

            return true;
        }

        public bool CanActorTakeAction(GameObject actor)
        {
            // If it's not the player's turn, or they've already acted, return false
            return currentState == GameState.PLAYER_TURN && !charactersWhoActed.Contains(actor);
        }
    }
}