using System.Collections;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Hazards;
using JRogue.Manager.Combat;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Status;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Manager.Progression
{
    /// <summary>DCSS-style rest session. See Docs/Progression/Rest-Requirements.md.</summary>
    public sealed class RestSessionService : MonoBehaviour
    {
        const string LogPrefix = "[Rest]";

        public static RestSessionService Instance { get; private set; }

        bool _resting;
        Coroutine _loop;

        public static bool IsResting => Instance != null && Instance._resting;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void TryStartOrDeny()
        {
            if (Instance == null)
            {
                Debug.LogWarning($"{LogPrefix} No {nameof(RestSessionService)} in scene.");
                return;
            }

            Instance.TryStartOrDenyInternal();
        }

        void TryStartOrDenyInternal()
        {
            if (_resting)
            {
                Debug.Log($"{LogPrefix} Already resting.");
                return;
            }

            if (!CanStartRest(out string denyReason, out bool isNothingToRestore))
            {
                if (isNothingToRestore)
                    Debug.Log($"{LogPrefix} Rest is not necessary.");
                else
                    Debug.Log($"{LogPrefix} {denyReason}");
                return;
            }

            PartyRestState restState = GetPartyRestState();
            if (restState == null)
            {
                Debug.LogWarning($"{LogPrefix} No {nameof(PartyRestState)} on party.");
                return;
            }

            List<BaseActor> living = CollectLivingMembers();
            restState.CommitSuccessfulRestStart(living);
            Debug.Log($"{LogPrefix} Rest started.");
            _loop = StartCoroutine(RestSessionLoop(restState));
        }

        public static bool CanStartRest(out string denyReason, out bool isNothingToRestore)
        {
            isNothingToRestore = false;
            denyReason = null;

            if (IsResting)
            {
                denyReason = "Already resting.";
                return false;
            }

            TurnManager turn = TurnManager.Instance;
            if (turn == null || turn.currentState != GameState.PLAYER_TURN)
            {
                denyReason = "Not the player's turn.";
                return false;
            }

            if (turn.currentState == GameState.GAME_OVER)
            {
                denyReason = "Game over.";
                return false;
            }

            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers.Count == 0)
            {
                denyReason = "No party.";
                return false;
            }

            BaseActor active = party.GetActiveMember();
            if (active != null && !turn.CanActorTakeAction(active.gameObject))
            {
                denyReason = "Active party member cannot act.";
                return false;
            }

            if (CombatThreatCoordinator.Instance != null && CombatThreatCoordinator.Instance.IsInCombat)
            {
                denyReason = "Cannot rest while in combat.";
                return false;
            }

            if (PartyStatusQueries.AnyLivingMemberHasNegativeStatus())
            {
                denyReason = BuildNegativeStatusDenyMessage();
                return false;
            }

            if (TryFindMemberOnDamagingHazard(out string hazardName))
            {
                denyReason = $"Cannot rest while a party member is exposed to hazardous terrain ({hazardName}).";
                return false;
            }

            if (!AnyMemberNeedsRest(GetPartyRestState(), out isNothingToRestore))
            {
                denyReason = "Rest is not necessary.";
                return false;
            }

            return true;
        }

        IEnumerator RestSessionLoop(PartyRestState restState)
        {
            _resting = true;
            TurnManager turn = TurnManager.Instance;
            CombatThreatCoordinator combat = CombatThreatCoordinator.Instance;

            void OnCombatEntered()
            {
                if (_resting)
                    CancelRest("combat started.");
            }

            if (combat != null)
                combat.OnEnterCombat += OnCombatEntered;

            if (turn != null)
                turn.currentState = GameState.BUSY;

            try
            {
                while (_resting && turn != null)
                {
                    if (turn.currentState == GameState.GAME_OVER)
                        break;

                    if (CombatThreatCoordinator.Instance != null
                        && CombatThreatCoordinator.Instance.IsInCombat)
                    {
                        CancelRest("combat started.");
                        break;
                    }

                    Dictionary<EntityId, int> hpBefore = CapturePartyHp();
                    bool hadNegativeBefore = PartyStatusQueries.AnyLivingMemberHasNegativeStatus();

                    turn.ExecuteRestPlayerPhaseStep(restState);

                    if (DetectPartyDamage(hpBefore)
                        || (!hadNegativeBefore && PartyStatusQueries.AnyLivingMemberHasNegativeStatus()))
                    {
                        CancelRest(DetectPartyDamage(hpBefore)
                            ? "party took damage."
                            : "negative status.");
                        break;
                    }

                    if (AllSoulPowerUsersFull())
                    {
                        Debug.Log($"{LogPrefix} Rest complete.");
                        break;
                    }

                    yield return turn.RunEnemyWaveDuringRest();

                    if (turn.currentState == GameState.GAME_OVER)
                        break;

                    CombatThreatCoordinator.Instance?.EvaluateThreat();

                    if (CombatThreatCoordinator.Instance != null
                        && CombatThreatCoordinator.Instance.IsInCombat)
                    {
                        CancelRest("combat started.");
                        break;
                    }
                }
            }
            finally
            {
                if (combat != null)
                    combat.OnEnterCombat -= OnCombatEntered;

                _resting = false;
                _loop = null;
                restState.ClearSessionBudgets();

                if (turn != null && turn.currentState != GameState.GAME_OVER)
                    turn.currentState = GameState.PLAYER_TURN;
            }
        }

        void CancelRest(string reason)
        {
            if (!_resting)
                return;

            Debug.Log($"{LogPrefix} Rest interrupted: {reason}");
            _resting = false;
        }

        static bool AllSoulPowerUsersFull()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return true;

            bool anySoulUser = false;
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member.stats == null || member.stats.currentHP <= 0)
                    continue;

                if (!HumanClassRules.UsesSoulPower(member.stats.humanClass))
                    continue;

                anySoulUser = true;
                if (member.stats.currentSoulPower < member.stats.MaxSoulPower)
                    return false;
            }

            return true;
        }

        static bool AnyMemberNeedsRest(PartyRestState restState, out bool nothingToRestore)
        {
            nothingToRestore = true;
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return false;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member.stats == null || member.stats.currentHP <= 0)
                    continue;

                CharacterStats stats = member.stats;
                if (HumanClassRules.UsesSoulPower(stats.humanClass)
                    && stats.MaxSoulPower > 0
                    && stats.currentSoulPower < stats.MaxSoulPower)
                {
                    nothingToRestore = false;
                    return true;
                }

                int budget = PartyRestState.ComputeHealBudgetForMember(
                    stats,
                    member.gameObject.GetEntityId(),
                    restState);
                if (budget > 0 && stats.currentHP < stats.MaxHP)
                {
                    nothingToRestore = false;
                    return true;
                }
            }

            return false;
        }

        static Dictionary<EntityId, int> CapturePartyHp()
        {
            var map = new Dictionary<EntityId, int>();
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return map;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member?.stats == null || member.stats.currentHP <= 0)
                    continue;

                map[member.gameObject.GetEntityId()] = member.stats.currentHP;
            }

            return map;
        }

        static bool DetectPartyDamage(Dictionary<EntityId, int> hpBefore)
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return false;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member?.stats == null)
                    continue;

                EntityId id = member.gameObject.GetEntityId();
                if (!hpBefore.TryGetValue(id, out int before))
                    continue;

                if (member.stats.currentHP < before)
                    return true;
            }

            return false;
        }

        static List<BaseActor> CollectLivingMembers()
        {
            var list = new List<BaseActor>();
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return list;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member?.stats != null && member.stats.currentHP > 0)
                    list.Add(member);
            }

            return list;
        }

        static PartyRestState GetPartyRestState()
        {
            PartyManager party = PartyManager.Instance;
            return party != null ? party.GetComponent<PartyRestState>() : null;
        }

        static bool TryFindMemberOnDamagingHazard(out string hazardName)
        {
            hazardName = null;
            HazardService hazards = HazardService.Instance;
            PartyManager party = PartyManager.Instance;
            if (hazards == null || party == null)
                return false;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member?.stats == null || member.stats.currentHP <= 0)
                    continue;

                if (!hazards.WouldDealOccupancyDamageTo(member))
                    continue;

                EnvironmentalHazardDefinition def = hazards.GetHazardAt(member.GridPosition);
                hazardName = def != null ? def.displayName : "hazard";
                return true;
            }

            return false;
        }

        static string BuildNegativeStatusDenyMessage()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return "Cannot rest while a party member is under a negative status effect.";

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member?.stats == null || member.stats.currentHP <= 0)
                    continue;

                StatusEffectController statuses = member.GetComponent<StatusEffectController>();
                if (statuses == null || !statuses.HasNegativeStatus())
                    continue;

                return $"Cannot rest while a party member is under a negative status effect ({member.DisplayName}).";
            }

            return "Cannot rest while a party member is under a negative status effect.";
        }

#if UNITY_EDITOR
        public void ResetForTests()
        {
            if (_loop != null)
                StopCoroutine(_loop);
            _loop = null;
            _resting = false;
            GetPartyRestState()?.ResetForTests();
        }
#endif
    }
}
