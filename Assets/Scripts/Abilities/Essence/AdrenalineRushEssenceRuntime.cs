using JRogue.Effects.Timed;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Ability.Essence
{
    public sealed class AdrenalineRushEssenceRuntime : ActorTimedEffectRuntime
    {
        int _strengthDelta;
        int _defenseDelta;

        public bool IsActive => IsApplied && TurnsRemaining > 0;

        /// <summary>Flat AC adjustment while active (requirements "Defense −10" maps to AC, not a StatType).</summary>
        public int ArmorClassBonus => IsActive ? _defenseDelta : 0;

        public void Apply(EssenceDesignAbility ability)
        {
            int duration = ability != null && ability.effectDurationTurns > 0 ? ability.effectDurationTurns : 10;
            _strengthDelta = ability != null ? ability.strengthDelta : 0;
            _defenseDelta = ability != null ? ability.defenseDelta : 0;
            Initialize(duration);
            Debug.Log(
                $"[Adrenaline Rush] Active on {gameObject.name}: STR {_strengthDelta:+0;-#}, DEF {_defenseDelta:+0;-#}.");
        }

        protected override void ApplyEffect()
        {
            if (_strengthDelta != 0)
                Stats?.GetStatByType(StatType.Strength)?.AddModifier(_strengthDelta, this, ModifierSourceLayer.Temporary);
        }

        protected override void RemoveEffect()
        {
            Stats?.GetStatByType(StatType.Strength)?.RemoveModifiersFromSource(this);
        }

        protected override void OnEffectExpired()
        {
            Debug.Log($"[Adrenaline Rush] Expired on {gameObject.name}.");
        }
    }
}
