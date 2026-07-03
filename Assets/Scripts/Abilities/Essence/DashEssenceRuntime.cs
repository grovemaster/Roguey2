using JRogue.Actors;
using JRogue.Effects.Timed;
using UnityEngine;

namespace JRogue.Ability.Essence
{
    public sealed class DashEssenceRuntime : ActorTimedEffectRuntime
    {
        int _movementTilesPerTurn;
        int _movesRemainingThisPhase;

        public bool IsActive => IsApplied && TurnsRemaining > 0;
        public int MovesRemainingThisPhase => _movesRemainingThisPhase;

        public void Apply(EssenceDesignAbility ability)
        {
            _movementTilesPerTurn = ability != null ? Mathf.Max(1, ability.movementTilesPerTurn) : 2;
            Initialize(ability != null ? ability.effectDurationTurns : 1);
            ResetMovesForPhase();
            Debug.Log(
                $"[Dash] Active on {gameObject.name} for {TurnsRemaining} player phases " +
                $"({_movementTilesPerTurn} tiles/phase).");
        }

        public void ResetMovesForPhase()
        {
            if (!IsActive)
                return;

            _movesRemainingThisPhase = _movementTilesPerTurn;
        }

        public bool ConsumeMoveStep()
        {
            if (_movesRemainingThisPhase <= 0)
                return false;

            _movesRemainingThisPhase--;
            return _movesRemainingThisPhase > 0;
        }

        public static bool AllowsMoveWhileActed(BaseActor actor)
        {
            if (actor == null)
                return false;

            DashEssenceRuntime dash = actor.GetComponent<DashEssenceRuntime>();
            return dash != null && dash.IsActive && dash._movesRemainingThisPhase > 0;
        }

        public static bool ShouldCompleteTurnAfterMove(BaseActor actor)
        {
            if (actor == null)
                return true;

            DashEssenceRuntime dash = actor.GetComponent<DashEssenceRuntime>();
            if (dash == null || !dash.IsActive)
                return true;

            return !dash.ConsumeMoveStep();
        }

        protected override void ApplyEffect()
        {
        }

        protected override void RemoveEffect()
        {
        }

        public override void OnPlayerPhaseStart()
        {
            base.OnPlayerPhaseStart();
            ResetMovesForPhase();
        }

        protected override void OnEffectExpired()
        {
            _movesRemainingThisPhase = 0;
            Debug.Log($"[Dash] Expired on {gameObject.name}.");
        }
    }
}
