using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Applies stat/resistance/passive lists with a stable source object (Pattern B).
    /// Shared by Dwarf common abilities, Ancestor path nodes, and similar racial payloads.
    /// </summary>
    public static class RacialAbilityPayloadApplicator
    {
        public static void Apply(
            GameObject target,
            CharacterStats stats,
            object source,
            IReadOnlyList<AttributeModifier> statModifiers,
            IReadOnlyList<DamageResistanceModifier> resistanceModifiers,
            IReadOnlyList<PassiveEffect> passiveEffects)
        {
            if (stats == null || source == null)
                return;

            if (statModifiers != null)
            {
                foreach (AttributeModifier mod in statModifiers)
                {
                    Stat targetStat = stats.GetStatByType(mod.attribute);
                    targetStat?.AddModifier(mod.value, source);
                }
            }

            if (resistanceModifiers != null)
            {
                foreach (DamageResistanceModifier res in resistanceModifiers)
                    stats.AddResistanceModifier(res.type, res.value, source);
            }

            if (passiveEffects != null && target != null)
            {
                foreach (PassiveEffect passive in passiveEffects)
                    passive?.OnApply(target);
            }
        }

        public static void Remove(
            GameObject target,
            CharacterStats stats,
            object source,
            IReadOnlyList<AttributeModifier> statModifiers,
            IReadOnlyList<DamageResistanceModifier> resistanceModifiers,
            IReadOnlyList<PassiveEffect> passiveEffects)
        {
            if (stats == null || source == null)
                return;

            if (statModifiers != null)
            {
                foreach (AttributeModifier mod in statModifiers)
                {
                    Stat targetStat = stats.GetStatByType(mod.attribute);
                    targetStat?.RemoveModifiersFromSource(source);
                }
            }

            if (resistanceModifiers != null)
            {
                foreach (DamageResistanceModifier res in resistanceModifiers)
                    stats.RemoveResistanceModifier(res.type, source);
            }

            if (passiveEffects != null && target != null)
            {
                for (int i = passiveEffects.Count - 1; i >= 0; i--)
                    passiveEffects[i]?.OnRemove(target);
            }
        }

        public static void RefreshPassives(
            GameObject target,
            IReadOnlyList<PassiveEffect> passiveEffects)
        {
            if (target == null || passiveEffects == null)
                return;
            foreach (PassiveEffect passive in passiveEffects)
                passive?.Refresh(target);
        }

        public static void NotifyPassivesTurnStart(
            GameObject target,
            IReadOnlyList<PassiveEffect> passiveEffects)
        {
            if (target == null || passiveEffects == null)
                return;
            foreach (PassiveEffect passive in passiveEffects)
                passive?.OnTurnStart(target);
        }

        public static void ApplyNodePayload(
            GameObject target,
            CharacterStats stats,
            object source,
            SpiritImprintNodeData node)
        {
            if (node == null)
                return;
            Apply(target, stats, source, node.statModifiers, node.resistanceModifiers, node.passiveEffects);
        }

        public static void RemoveNodePayload(
            GameObject target,
            CharacterStats stats,
            object source,
            SpiritImprintNodeData node)
        {
            if (node == null)
                return;
            Remove(target, stats, source, node.statModifiers, node.resistanceModifiers, node.passiveEffects);
        }
    }
}
