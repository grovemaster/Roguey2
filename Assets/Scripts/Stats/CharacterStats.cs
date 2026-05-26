using System;
using System.Collections.Generic;
using JRogue.Stats.Racial;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace JRogue.Stats
{
    public class CharacterStats : MonoBehaviour
    {
        [Header("Persona & Biography")]
        [FormerlySerializedAs("folk")]
        public Race race = Race.Human;

        public HumanClass humanClass = HumanClass.None;
        public RacialSubsystemKind racialSubsystem = RacialSubsystemKind.None;
        [Tooltip("Intrinsic anatomy / capability flags (saved with identity). Essences add OR-masks and exclusion-bypass masks at runtime — see GetEffectiveBodyCapabilities().")]
        public BodyCapabilityFlags bodyCapabilities = BodyCapabilityFlags.None;

        /// <summary>Phase 4: runtime-only contributions (essence slot keys, etc.). Not serialized.</summary>
        readonly Dictionary<string, BodyEquipmentContribution> _bodyEquipmentContributions = new Dictionary<string, BodyEquipmentContribution>();

        public Gender gender;
        public Alignment alignment;
        public int age;
        public float height; // in meters/cm
        public float weight; // in kg
        public int renown;   // Positive = Hero, Negative = Infamous

        [Header("Physical Senses")]
        public Stat sight = new Stat(8);    // Range in grid tiles
        public Stat hearing = new Stat(10); // Radius for detecting movement
        public Stat smell = new Stat(5);   // Radius for generalized detection

        [Header("Basic Attributes")]
        public Stat Strength = new Stat(10);
        public Stat Dexterity = new Stat(10);
        public Stat Agility = new Stat(10);
        public Stat Constitution = new Stat(10);
        public Stat Intelligence = new Stat(10);
        public Stat Wisdom = new Stat(10);
        public Stat Charisma = new Stat(10);
        public Stat Luck = new Stat(10);

        [Header("Progression")]
        [Min(1)] public int level = 1;
        [Min(0)] public int experience;

        [Tooltip("Flat Max Soul Power bonus from level-ups.")]
        [Min(0)] public int levelSoulPowerBonus;

        [Tooltip("Flat Max Magic Power bonus from level-ups (Mage only).")]
        [Min(0)] public int levelMagicPowerBonus;

        [Tooltip("Flat Max Divine Power bonus from level-ups (Priest only).")]
        [Min(0)] public int levelDivinePowerBonus;

        [Header("Current Status")]
        public int currentHP;
        public int currentSoulPower;
        public int currentMagicPower;
        public int currentDivinePower;

        // [Header("Inspector View (Debug Only)")]
        // // This list will show up in your Inspector!
        // public List<StatMapping<DamageType>> resistanceList = new List<StatMapping<DamageType>>();
        // public List<StatMapping<WeaponType>> seaponProficienciesList = new List<StatMapping<WeaponType>>();
        // public List<StatMapping<SkillType>> skillsList = new List<StatMapping<SkillType>>();


        // Keep your Dictionaries for the fast code lookups
        // The Matrix Dictionaries
        // UPDATED: These now store Stat objects instead of raw integers
        [Header("The Matrix Dictionaries")]
        [ShowInInspector]
        public Dictionary<DamageType, Stat> Resistances = new Dictionary<DamageType, Stat>();
        [ShowInInspector]
        public Dictionary<WeaponType, Stat> WeaponProficiencies = new Dictionary<WeaponType, Stat>();
        [ShowInInspector]
        public Dictionary<SkillType, Stat> Skills = new Dictionary<SkillType, Stat>();

        void Awake()
        {
            InitializeDictionaries();

            // Set current pools (moved from 7a)
            // Set current pools to max at the start of the game
            currentHP = MaxHP;
            RefreshResourcePoolsToMax();
        }

        /// <summary>Clamps current pools to class-appropriate maximums.</summary>
        public void RefreshResourcePoolsToMax()
        {
            currentSoulPower = Mathf.Min(currentSoulPower, MaxSoulPower);
            if (humanClass == HumanClass.Mage)
                currentMagicPower = MaxMagicPower;
            else
                currentMagicPower = 0;

            if (humanClass == HumanClass.Priest)
                currentDivinePower = MaxDivinePower;
            else
                currentDivinePower = 0;
        }

        private void InitializeDictionaries()
        {
            // Initialize each dictionary entry with a new Stat instance (Base 0)
            foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
                Resistances[type] = new Stat(0);

            foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
                WeaponProficiencies[type] = new Stat(0);

            foreach (SkillType type in Enum.GetValues(typeof(SkillType)))
                Skills[type] = new Stat(0);
        }

        // --- MODIFIER HELPERS ---
        // These methods allow Essences to modify specific types easily

        // public void AddResistanceModifier(DamageType type, int amount) => Resistances[type].AddModifier(amount);
        public void AddResistanceModifier(DamageType type, int amount, object source) => Resistances[type].AddModifier(amount, source);
        // public void RemoveResistanceModifier(DamageType type, int amount) => Resistances[type].RemoveModifier(amount);
        public void RemoveResistanceModifier(DamageType type, object source) => Resistances[type].RemoveModifiersFromSource(source);

        public int GetResistance(DamageType type) => Resistances.ContainsKey(type) ? Resistances[type].GetValue() : 0;

        /// <summary>Phase 4 — intrinsic + essence OR-capability bits (for equipment rules).</summary>
        public BodyCapabilityFlags GetEffectiveBodyCapabilities()
        {
            BodyCapabilityFlags f = bodyCapabilities;
            foreach (BodyEquipmentContribution c in _bodyEquipmentContributions.Values)
                f |= c.OrCapabilities;
            return f;
        }

        /// <summary>Phase 4 — actor body bits that equipped essences ignore for item exclusion masks.</summary>
        public BodyCapabilityFlags GetBodyExclusionBypassMask()
        {
            BodyCapabilityFlags m = BodyCapabilityFlags.None;
            foreach (BodyEquipmentContribution c in _bodyEquipmentContributions.Values)
                m |= c.ExclusionBypassMask;
            return m;
        }

        public void RegisterBodyEquipmentContribution(string sourceKey, BodyCapabilityFlags orCapabilities, BodyCapabilityFlags exclusionBypassMask)
        {
            if (string.IsNullOrEmpty(sourceKey))
                return;
            _bodyEquipmentContributions[sourceKey] = new BodyEquipmentContribution(orCapabilities, exclusionBypassMask);
        }

        public void UnregisterBodyEquipmentContribution(string sourceKey)
        {
            if (string.IsNullOrEmpty(sourceKey))
                return;
            _bodyEquipmentContributions.Remove(sourceKey);
        }

        public readonly struct BodyEquipmentContribution
        {
            public BodyEquipmentContribution(BodyCapabilityFlags orCapabilities, BodyCapabilityFlags exclusionBypassMask)
            {
                OrCapabilities = orCapabilities;
                ExclusionBypassMask = exclusionBypassMask;
            }

            public BodyCapabilityFlags OrCapabilities { get; }
            public BodyCapabilityFlags ExclusionBypassMask { get; }
        }

        // --- SYSTEM LOGIC ---

        public bool PerformSkillCheck(SkillType skill, int difficultyClass)
        {
            // A standard D&D-style check: d20 + Skill Level + Luck Modifier
            int roll = UnityEngine.Random.Range(1, 21);
            int luckBonus = Luck.GetValue() / 10;
            // Skills[skill] now requires .GetValue()
            int total = roll + Skills[skill].GetValue() + luckBonus;

            Debug.Log($"Skill Check [{skill}]: Roll({roll}) + Skill({Skills[skill].GetValue()}) + Luck({luckBonus}) = {total} vs DC {difficultyClass}");
            return total >= difficultyClass;
        }

        // --- CALCULATED STATS (Formulas) ---

        // Constitution governs HP & Encumbrance
        public int MaxHP => Constitution.GetValue() * 10;
        public int EncumbranceLimit => Constitution.GetValue() * 5;

        public int MaxSoulPower => HumanClassRules.ComputeMaxSoulPower(this);
        public int MaxMagicPower => HumanClassRules.ComputeMaxMagicPower(this);
        public int MaxDivinePower => HumanClassRules.ComputeMaxDivinePower(this);

        // Dexterity governs Armor Class (Base 10 + Dex bonus)
        public int ArmorClass => 10 + (Dexterity.GetValue() / 4);

        // Agility governs Movement Speed (Lower number = faster tick rate)
        public float MoveSpeed => 1.0f - (Agility.GetValue() * 0.01f);

        public Stat GetStatByType(StatType type)
        {
            return type switch
            {
                StatType.Strength => Strength,
                StatType.Dexterity => Dexterity,
                StatType.Agility => Agility,
                StatType.Constitution => Constitution,
                StatType.Intelligence => Intelligence,
                StatType.Wisdom => Wisdom,
                StatType.Charisma => Charisma,
                StatType.Luck => Luck,
                StatType.Sight => sight,
                StatType.Hearing => hearing,
                StatType.Smell => smell,
                _ => null
            };
        }

        public RacialIdentitySnapshot GetRacialIdentitySnapshot()
        {
            return new RacialIdentitySnapshot
            {
                snapshotVersion = RacialStackingContract.CurrentIdentitySnapshotVersion,
                race = race,
                humanClass = humanClass,
                subsystemKind = racialSubsystem,
                bodyCapabilities = bodyCapabilities
            };
        }

        /// <summary>Applies intrinsic identity fields only (folk, class, subsystem flag, body capabilities).</summary>
        public bool TryApplyRacialIdentitySnapshot(RacialIdentitySnapshot snapshot, out string error)
        {
            if (!RacialIdentityRules.TryValidate(snapshot, out error))
                return false;

            if (!HumanClassRules.CanApplyHumanClassFromSnapshot(humanClass, snapshot.humanClass, out error))
                return false;

            snapshot.ApplyTo(this);
            error = null;
            return true;
        }

        public void PrintCharacterSheet()
        {
            string report = $"--- {gameObject.name} Profile ---\n";
            report += $"Identity: {gender} {race} | Alignment: {alignment}\n";
            report += $"Bio: {age} yrs, {height}m, {weight}kg | Renown: {renown}\n";
            report += $"Senses: Sight({sight.GetValue()}), Hearing({hearing.GetValue()}), Smell({smell.GetValue()})\n";
            report += "-----------------------------";

            Debug.Log(report);
        }
    }

    [Serializable]
    public struct StatMapping<T>
    {
        public T type;
        public Stat stat;
    }
}
