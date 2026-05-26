using JRogue.Effects.Timed;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Ability.SuddenStrength
{
    /// <summary>
    /// Sudden Strength specialization using the shared timed stat-modifier runtime.
    /// </summary>
    public sealed class SuddenStrengthBuffRuntime : ActorTimedStatModifierRuntime
    {
        public int StrengthBonus => ModifierAmount;

        public void Apply(int bonus, int duration)
        {
            Configure(StatType.Strength, bonus, ModifierSourceLayer.Temporary);
            Initialize(duration);
        }

        protected override void OnEffectExpired()
        {
            Debug.Log($"[Sudden Strength] Expired on {gameObject.name}.");
        }
    }
}
