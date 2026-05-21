using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;

namespace JRogue.Racial
{
    /// <summary>
    /// Full progression-node payload shared by Tiefling cyborg implants and Undead skill-tree nodes.
    /// Folk <see cref="RacialLoadoutDefinition"/> uses stats/passives/actives only (no benefit/restriction lists).
    /// </summary>
    public interface IRacialProgressionPayload
    {
        IReadOnlyList<RacialRestrictionDefinition> RacialRestrictions { get; }
        IReadOnlyList<RacialBenefitDefinition> RacialBenefits { get; }
        IReadOnlyList<AttributeModifier> StatModifiers { get; }
        IReadOnlyList<DamageResistanceModifier> ResistanceModifiers { get; }
        IReadOnlyList<PassiveEffect> PassiveEffects { get; }
        IReadOnlyList<AbilityAction> ActiveAbilities { get; }
    }
}
