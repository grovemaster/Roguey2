using UnityEngine;
using JRogue.Stats;
using JRogue.Stats.Racial;
using System.Collections.Generic;
using JRogue.Ability;
using static JRogue.Item.Effect.StatModifierEffect;

namespace JRogue.Item.Essence
{
    [CreateAssetMenu(fileName = "New Essence", menuName = "JRogue/Essence")]
    public class EssenceData : ScriptableObject
    {
        public string essenceName;
        [TextArea] public string description;

        [Header("1. Simple Stat Modifiers")]
        public List<AttributeModifier> statModifiers;
        public List<DamageResistanceModifier> resistanceModifiers;

        [Header("2. Complex Passive Effects")]
        public List<PassiveEffect> complexPassives;

        [Header("3. Active Abilities")]
        public List<AbilityAction> activeAbilities;

        [Header("Phase 4 — Anatomy while equipped")]
        [Tooltip("OR-masked onto CharacterStats effective body capabilities while this essence is applied.")]
        public BodyCapabilityFlags bodyCapabilityOrWhileEquipped = BodyCapabilityFlags.None;

        [Tooltip("Actor body bits ignored when checking ItemData.equipExcludesActorFlags.")]
        public BodyCapabilityFlags bodyExclusionBypassMaskWhileEquipped = BodyCapabilityFlags.None;

        // public void Apply(GameObject target)
        // {
        //     var stats = target.GetComponent<CharacterStats>();

        //     // Apply all stat mods
        //     foreach (var mod in statModifiers)
        //         stats.GetStatByType(mod.attribute).AddModifier(mod.value);

        //     // Apply all resistance mods
        //     foreach (var res in resistanceModifiers)
        //         stats.AddResistanceModifier(res.type, res.value);

        //     // Apply all complex behaviors
        //     foreach (var passive in complexPassives)
        //         passive.OnApply(target);
        // }

        public void Apply(GameObject target)
        {
            var stats = target.GetComponent<CharacterStats>();
            if (stats == null) return;

            // This loop now handles Strength, Luck, Sight, etc., all in one go
            foreach (var mod in statModifiers)
            {
                Stat targetStat = stats.GetStatByType(mod.attribute);
                if (targetStat != null)
                {
                    // targetStat.AddModifier(mod.value);
                    // Fix: Added 'this' as the source
                    targetStat.AddModifier(mod.value, this);
                }
            }

            // Handle resistances...
            foreach (var res in resistanceModifiers)
            {
                // stats.AddResistanceModifier(res.type, res.value);
                // Fix: Ensure AddResistanceModifier in CharacterStats 
                // also accepts 'this' as a source
                stats.AddResistanceModifier(res.type, res.value, this);
            }

            // Handle complex passives...
            foreach (var passive in complexPassives)
            {
                passive.OnApply(target);
            }
        }

        public void Remove(GameObject target)
        {
            // Reverse the logic above using .RemoveModifier() and passive.OnRemove()

            var stats = target.GetComponent<CharacterStats>();

            // Apply all stat mods
            // foreach (var mod in statModifiers)
            //     stats.GetStatByType(mod.attribute).RemoveModifier(mod.value);

            // // Apply all resistance mods
            // foreach (var res in resistanceModifiers)
            //     stats.RemoveResistanceModifier(res.type, res.value);

            // 1. Remove Stat Modifiers
            foreach (var mod in statModifiers)
            {
                Stat targetStat = stats.GetStatByType(mod.attribute);
                if (targetStat != null)
                {
                    // Fix: Use the source-based removal
                    targetStat.RemoveModifiersFromSource(this);
                }
            }

            // 2.Remove Resistance Modifiers
            foreach (var res in resistanceModifiers)
            {
                // Fix: Ensure CharacterStats has a matching Remove method
                stats.RemoveResistanceModifier(res.type, this);
            }

            // 3. Handle complex behaviors
            foreach (var passive in complexPassives)
            {
                passive.OnRemove(target);
            }
        }

        // public void Remove(GameObject target)
        // {
        //     var stats = target.GetComponent<CharacterStats>();
        //     if (stats == null) return;

        //     stats.Strength.RemoveModifier(strengthBonus);
        //     stats.Agility.RemoveModifier(agilityBonus);

        //     foreach (var res in resistances)
        //     {
        //         stats.RemoveResistanceModifier(res.type, res.bonusValue);
        //     }
        // }
    }

    [System.Serializable]
    public struct DamageResistanceModifier
    {
        public DamageType type;
        public int value;
    }

    [System.Serializable]
    public struct AttributeModifier
    {
        public StatType attribute; // An enum of Strength, Dexterity, etc.
        public int value;
    }
}