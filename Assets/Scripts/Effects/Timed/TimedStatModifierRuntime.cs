using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Effects.Timed
{
    /// <summary>
    /// Timed stat modifier runtime that applies/removes one stat modifier source.
    /// </summary>
    public abstract class ActorTimedStatModifierRuntime : ActorTimedEffectRuntime
    {
        StatType _statType;
        int _modifierAmount;
        ModifierSourceLayer _layer;

        protected int ModifierAmount => _modifierAmount;

        protected void Configure(StatType statType, int modifierAmount, ModifierSourceLayer layer)
        {
            _statType = statType;
            _modifierAmount = modifierAmount;
            _layer = layer;
        }

        protected override void ApplyEffect()
        {
            Stat stat = Stats?.GetStatByType(_statType);
            stat?.AddModifier(_modifierAmount, this, _layer);
        }

        protected override void RemoveEffect()
        {
            Stat stat = Stats?.GetStatByType(_statType);
            stat?.RemoveModifiersFromSource(this);
        }
    }
}
