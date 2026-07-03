using JRogue.Effects.Timed;
using UnityEngine;

namespace JRogue.Ability.Essence
{
    public sealed class PoisonWeaponEssenceRuntime : ActorTimedEffectRuntime
    {
        float _procChance;

        public bool IsActive => IsApplied && TurnsRemaining > 0;

        public void Apply(EssenceDesignAbility ability)
        {
            _procChance = ability != null ? ability.procChance : 0f;
            Initialize(ability != null ? ability.effectDurationTurns : 1);
            Debug.Log(
                $"[Poison Weapon] Active on {gameObject.name} for {TurnsRemaining} player phases " +
                $"({_procChance:P0} proc chance).");
        }

        public bool RollProc() =>
            _procChance >= 1f || Random.value <= _procChance;

        protected override void ApplyEffect()
        {
        }

        protected override void RemoveEffect()
        {
        }

        protected override void OnEffectExpired()
        {
            Debug.Log($"[Poison Weapon] Expired on {gameObject.name}.");
        }
    }
}
