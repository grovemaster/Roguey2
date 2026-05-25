using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;
using UnityEngine.Serialization;

namespace JRogue.Racial
{
    /// <summary>
    /// Data-driven racial package: stat/resistance modifiers and passives, mirroring <see cref="EssenceData"/> apply/remove.
    /// Actives are listed for future UI / execution; Phase 1 does not invoke them automatically.
    /// </summary>
    [CreateAssetMenu(fileName = "RacialLoadout", menuName = "JRogue/Racial Loadout")]
    public class RacialLoadoutDefinition : ScriptableObject
    {
        [Tooltip("Unset = any race may use this loadout. Otherwise actor Race must match.")]
        [FormerlySerializedAs("requiredFolk")]
        public Race requiredRace = Race.Unset;

        public string loadoutName;
        [TextArea] public string description;

        [Header("Stat & resistance modifiers")]
        public List<AttributeModifier> statModifiers;
        public List<DamageResistanceModifier> resistanceModifiers;

        [Header("Passive effects")]
        public List<PassiveEffect> passiveEffects;

        [Header("Active abilities (execution wired later)")]
        public List<AbilityAction> activeAbilities;

        public bool CanApplyTo(CharacterStats stats)
        {
            if (stats == null) return false;
            if (requiredRace == Race.Unset) return true;
            return stats.race == requiredRace;
        }

        public void Apply(GameObject target)
        {
            var stats = target.GetComponent<CharacterStats>();
            if (stats == null || !CanApplyTo(stats)) return;

            statModifiers ??= new List<AttributeModifier>();
            resistanceModifiers ??= new List<DamageResistanceModifier>();
            passiveEffects ??= new List<PassiveEffect>();
            activeAbilities ??= new List<AbilityAction>();

            foreach (var mod in statModifiers)
            {
                Stat targetStat = stats.GetStatByType(mod.attribute);
                targetStat?.AddModifier(mod.value, this, ModifierSourceLayer.RacialLoadout);
            }

            foreach (var res in resistanceModifiers)
                stats.AddResistanceModifier(res.type, res.value, this);

            foreach (var passive in passiveEffects)
                passive?.OnApply(target);
        }

        public void Remove(GameObject target)
        {
            var stats = target.GetComponent<CharacterStats>();
            if (stats == null) return;

            if (statModifiers != null)
            {
                foreach (var mod in statModifiers)
                {
                    Stat targetStat = stats.GetStatByType(mod.attribute);
                    targetStat?.RemoveModifiersFromSource(this);
                }
            }

            if (resistanceModifiers != null)
            {
                foreach (var res in resistanceModifiers)
                    stats.RemoveResistanceModifier(res.type, this);
            }

            if (passiveEffects != null)
            {
                foreach (var passive in passiveEffects)
                    passive?.OnRemove(target);
            }
        }

        public void RefreshPassives(GameObject target)
        {
            if (passiveEffects == null) return;
            foreach (var passive in passiveEffects)
                passive?.Refresh(target);
        }

        public void NotifyPassivesTurnStart(GameObject target)
        {
            if (passiveEffects == null) return;
            foreach (var passive in passiveEffects)
                passive?.OnTurnStart(target);
        }
    }
}
