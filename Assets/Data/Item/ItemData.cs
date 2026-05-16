using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Effect;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Item
{
    /// <summary>UX / inventory policy hints (config can also require confirms by category).</summary>
    [System.Flags]
    public enum ItemInventoryRiskHint
    {
        None = 0,
        StoryTagged = 1 << 0,
        Rare = 1 << 1,
        Cursed = 1 << 2,
        HighValue = 1 << 3
    }

    public enum ItemCategory
    {
        Accessory,
        Armor,
        Artifact,
        Book,
        Currency,
        Essence,
        Evocable,
        Junk,
        Missile,
        PlotItem,
        Potion,
        QuestItem,
        Relic,
        Scroll,
        Spellbook,
        Staff,
        Treasure,
        Wand,
        Weapon
    }
    public enum EquipmentSlot
    {
        MainHand,
        OffHand,
        Head,
        Torso,
        Legs,
        Feet,
        Accessory_MainHand,
        Accessory_OffHand,
        Accessory_Head // Neclace, Amulet, Earring, etc.
    }

    [CreateAssetMenu(fileName = "New Item", menuName = "JRogue/ItemData")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemName;
        public ItemCategory category;
        public ItemInventoryRiskHint inventoryRiskHints;
        public EquipmentSlot slotType; // Head, Body, Ring, etc.
        public float weight;
        public Sprite icon;

        [Header("Phase 4 — Anatomy / equip rules")]
        [Tooltip("Actor effective flags must include all these bits (after intrinsic + essence OR-masks).")]
        public BodyCapabilityFlags equipRequiresAllFlags = BodyCapabilityFlags.None;

        [Tooltip("Conflict when actor has any of these bits, unless masked by essence exclusion bypass.")]
        public BodyCapabilityFlags equipExcludesActorFlags = BodyCapabilityFlags.None;

        [Header("Combat Modules")]
        // Support for multi-damage (e.g. 5 Blunt + 2 Fire)
        public List<DamageEntry> damageModules = new List<DamageEntry>();

        [Header("Stats & Passives")]
        public List<StatModifierEffect> statModifiers;
        public List<PassiveEffect> passiveEffects; // Run OnEquip/OnUnequip
        [Header("Activated Ability")]
        public List<AbilityAction> activeAbilities;  // Run OnActivate

        void Awake()
        {
            damageModules ??= new List<DamageEntry>();
            statModifiers ??= new List<StatModifierEffect>();
            passiveEffects ??= new List<PassiveEffect>();
            activeAbilities ??= new List<AbilityAction>();
        }

        public void OnEquip(GameObject target)
        {
            var stats = target.GetComponent<JRogue.Stats.CharacterStats>();
            if (stats == null) return;

            // Apply Stats (using 'this' as the source)
            foreach (var mod in statModifiers)
            {
                var stat = stats.GetStatByType(mod.targetStat);
                stat?.AddModifier(mod.modifierAmount, this);
                // var stat = stats.GetStatByType(mod.attribute);
                // stat?.AddModifier(mod.value, this);
            }

            // Apply Passives
            foreach (var p in passiveEffects) p.OnApply(target);
        }

        public void OnUnequip(GameObject target)
        {
            var stats = target.GetComponent<JRogue.Stats.CharacterStats>();
            if (stats == null) return;

            foreach (var mod in statModifiers)
            {
                var stat = stats.GetStatByType(mod.targetStat);
                // var stat = stats.GetStatByType(mod.attribute);
                stat?.RemoveModifiersFromSource(this);
            }

            foreach (var p in passiveEffects) p.OnRemove(target);
        }
    }

    [System.Serializable]
    public struct DamageEntry
    {
        public DamageType type;
        public int value;
    }
}