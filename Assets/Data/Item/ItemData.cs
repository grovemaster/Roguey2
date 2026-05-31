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
        Weapon,
        /// <summary>Must remain last — preserves serialized <c>category</c> ints on existing item assets.</summary>
        Key,
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

        [Header("Economy")]
        [Tooltip("Base gold value per unit. Stack value = goldValue × quantity when appraised.")]
        public int goldValue;

        [Tooltip("When true, list/inspect show ? until ItemInstance.IsAppraised.")]
        public bool requiresAppraisal = true;

        [Header("Floor pickup")]
        [Tooltip("When true, party members auto-collect this item on tile entry.")]
        public bool autoPickupOnStep;

        [Tooltip("When true with autoPickupOnStep, entering the tile requires confirmation before pickup.")]
        public bool requiresAutoPickupConfirmation;

        [Tooltip("Unclaimed floor item despawns after this many player phases. 0 = indefinite.")]
        [Min(0)]
        public int floorLifetimePlayerPhases;

        /// <summary>Walk-over auto-pickup without a confirmation dialog (e.g. mana stones).</summary>
        public bool ParticipatesInSilentAutoPickupOnStep =>
            autoPickupOnStep && !requiresAutoPickupConfirmation;

        /// <summary>Walk-over auto-pickup that must be confirmed before the move resolves.</summary>
        public bool RequiresConfirmBeforeAutoPickupOnStep =>
            autoPickupOnStep && requiresAutoPickupConfirmation;

        /// <summary>Item participates in value column (worth showing gold or ?).</summary>
        public bool HasMonetaryValue => goldValue > 0 || requiresAppraisal;

        [Header("Phase 4 — Anatomy / equip rules")]
        [Tooltip("Actor effective flags must include all these bits (after intrinsic + essence OR-masks).")]
        public BodyCapabilityFlags equipRequiresAllFlags = BodyCapabilityFlags.None;

        [Tooltip("Conflict when actor has any of these bits, unless masked by essence exclusion bypass.")]
        public BodyCapabilityFlags equipExcludesActorFlags = BodyCapabilityFlags.None;

        [Header("Weapon / ammo (ranged)")]
        public WeaponType weaponType;
        [Min(1)] public int handsRequired = 1;
        [Tooltip("Missile ammo that may only be used or equipped with a bow.")]
        public bool requiresBow;
        [Tooltip("When false, item cannot be thrown from inventory (e.g. arrows).")]
        public bool isThrowable = true;

        public bool IsBowWeapon =>
            category == ItemCategory.Weapon && weaponType == WeaponType.Bow;

        public bool IsBowAmmo =>
            category == ItemCategory.Missile && requiresBow;

        /// <summary>
        /// Inventory equip policy: only gear categories may use equipment slots
        /// (missiles limited to bow ammo such as arrows).
        /// </summary>
        public bool IsEquippableByCategory =>
            category switch
            {
                ItemCategory.Accessory or ItemCategory.Armor or ItemCategory.Staff
                    or ItemCategory.Wand or ItemCategory.Weapon => true,
                ItemCategory.Missile => IsBowAmmo,
                _ => false,
            };

        [Header("Combat Modules")]
        // Support for multi-damage (e.g. 5 Blunt + 2 Fire)
        public List<DamageEntry> damageModules = new List<DamageEntry>();

        [Header("Stats & Passives")]
        public List<StatModifierEffect> statModifiers;
        public List<PassiveEffect> passiveEffects; // Run OnEquip/OnUnequip
        [Header("Activated Ability")]
        public List<AbilityAction> activeAbilities;  // Run OnActivate

        [Header("Inventory use")]
        [Tooltip("Optional debug log prefix when using from inventory with targeting (e.g. Scroll:Fireball).")]
        public string inventoryTargetedUseLogTag;

        void Awake()
        {
            damageModules ??= new List<DamageEntry>();
            statModifiers ??= new List<StatModifierEffect>();
            passiveEffects ??= new List<PassiveEffect>();
            activeAbilities ??= new List<AbilityAction>();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (requiresAutoPickupConfirmation && !autoPickupOnStep)
            {
                Debug.LogWarning(
                    $"{name}: requiresAutoPickupConfirmation is ignored without autoPickupOnStep.",
                    this);
            }
        }
#endif

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