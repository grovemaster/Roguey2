using System.Collections;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Manager.Combat;
using JRogue.Manager.Essence;
using JRogue.Manager.Party;
using JRogue.Manager.Progression;
using JRogue.Hazards;
using JRogue.Traps;
using JRogue.Racial;
using JRogue.Status;
using JRogue.World.Lighting;
using UnityEngine;
public enum GameState { PLAYER_TURN, ENEMY_TURN, BUSY, GAME_OVER }

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
            LightingService.Instance?.OnPartyVisionActivity();
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
            if (currentState == GameState.GAME_OVER)
                return;

            if (currentState == GameState.PLAYER_TURN)
            {
                StartCoroutine(EnemyTurnSequence());
            }
        }

        /// <summary>One rest step player-phase boundary (SP regen → upkeep → statuses → rest HP → hazards).</summary>
        public void ExecuteRestPlayerPhaseStep(PartyRestState restState)
        {
            LightingService.Instance?.OnPlayerPhaseBoundary();
            TrapService.Instance?.EvaluateDetection();

            PartyManager party = PartyManager.Instance;
            if (party == null)
                return;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member.stats == null || member.stats.currentHP <= 0)
                    continue;

                SoulPowerRegenerationService.TickRegeneration(member.gameObject);
                RacialPassiveHooks.NotifyTurnStart(member.gameObject);
                EssenceSlotManager slots = member.GetComponent<EssenceSlotManager>();
                if (slots != null)
                    slots.NotifyTurnStart();
                StatusEffectController statuses = member.GetComponent<StatusEffectController>();
                if (statuses != null)
                    statuses.TickStatuses();

                if (restState != null)
                    restState.TickRestHeal(member);
            }

            HazardService.Instance?.TickOccupancyOnPlayerPhaseStart();

            FindAnyObjectByType<VisibilityManager>()?.RefreshPartyVision();
            LightingService.Instance?.OnPartyVisionActivity();
        }

        public IEnumerator RunEnemyWaveDuringRest()
        {
            yield return RunEnemyWave(endWithPlayerTurn: false);
        }

        public void EnterGameOver()
        {
            currentState = GameState.GAME_OVER;
            charactersWhoActed.Clear();
            Debug.Log("--- Game Over ---");
        }

        private IEnumerator EnemyTurnSequence()
        {
            yield return RunEnemyWave(endWithPlayerTurn: true);
        }

        IEnumerator RunEnemyWave(bool endWithPlayerTurn)
        {
            if (currentState == GameState.GAME_OVER)
                yield break;

            currentState = GameState.ENEMY_TURN;
            charactersWhoActed.Clear();

            EnemyController[] enemies = FindObjectsByType<EnemyController>();

            foreach (EnemyController enemy in enemies)
            {
                if (currentState == GameState.GAME_OVER)
                    yield break;

                if (enemy != null)
                {
                    enemy.TakeTurn();
                    yield return new WaitForSeconds(0.05f);
                }
            }

            if (currentState == GameState.GAME_OVER)
                yield break;

            CombatThreatCoordinator.Instance?.ApplyPursuitDecayAfterEnemyWave();
            CombatThreatCoordinator.Instance?.EvaluateThreat();

            if (endWithPlayerTurn)
            {
                currentState = GameState.PLAYER_TURN;
                Debug.Log("--- New Player Turn ---");
                NotifyPartyTurnStart();
            }
            else
            {
                currentState = GameState.BUSY;
            }
        }

        private void NotifyPartyTurnStart()
        {
            PartyRestState restState = PartyManager.Instance != null
                ? PartyManager.Instance.GetComponent<PartyRestState>()
                : null;

            LightingService.Instance?.OnPlayerPhaseBoundary();
            TrapService.Instance?.EvaluateDetection();

            if (PartyManager.Instance == null)
                return;

            foreach (BaseActor member in PartyManager.Instance.partyMembers)
            {
                if (member == null || member.stats == null || member.stats.currentHP <= 0)
                    continue;

                SoulPowerRegenerationService.TickRegeneration(member.gameObject);
                RacialPassiveHooks.NotifyTurnStart(member.gameObject);
                EssenceSlotManager slots = member.GetComponent<EssenceSlotManager>();
                if (slots != null)
                    slots.NotifyTurnStart();
                StatusEffectController statuses = member.GetComponent<StatusEffectController>();
                if (statuses != null)
                    statuses.TickStatuses();
            }

            HazardService.Instance?.TickOccupancyOnPlayerPhaseStart();

            FindAnyObjectByType<VisibilityManager>()?.RefreshPartyVision();
            LightingService.Instance?.OnPartyVisionActivity();
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
            if (currentState == GameState.GAME_OVER)
                return false;

            if (currentState != GameState.PLAYER_TURN || charactersWhoActed.Contains(actor))
                return false;

            if (actor != null
                && actor.TryGetComponent(out StatusEffectController statuses)
                && statuses.HasStatus(StatusEffectId.Stunned))
                return false;

            return true;
        }
    }
}