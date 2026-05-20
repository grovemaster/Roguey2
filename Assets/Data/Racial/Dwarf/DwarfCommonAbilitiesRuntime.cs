using System;
using System.Collections.Generic;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    sealed class DwarfCommonAbilityModifierSource : IEquatable<DwarfCommonAbilityModifierSource>
    {
        public int SlotIndex { get; }
        public DwarfCommonAbilityDefinition Ability { get; }

        public DwarfCommonAbilityModifierSource(int slotIndex, DwarfCommonAbilityDefinition ability)
        {
            SlotIndex = slotIndex;
            Ability = ability;
        }

        public bool Equals(DwarfCommonAbilityModifierSource other) =>
            other != null && SlotIndex == other.SlotIndex && Ability == other.Ability;

        public override bool Equals(object obj) => obj is DwarfCommonAbilityModifierSource o && Equals(o);

        public override int GetHashCode() =>
            HashCode.Combine(SlotIndex, Ability != null ? Ability.GetEntityId().GetHashCode() : 0);
    }

    /// <summary>
    /// Applies up to three preset common racial abilities (Pattern B). v0: Inspector assignment until leveling ships.
    /// </summary>
    [DefaultExecutionOrder(51)]
    public class DwarfCommonAbilitiesRuntime : MonoBehaviour
    {
        public const int SlotCount = 3;

        [SerializeField] List<DwarfCommonSlotPreset> presetCommonAbilities = new List<DwarfCommonSlotPreset>();
        [SerializeField] bool requireDwarfAncestrySubsystem = true;

        readonly Dictionary<int, DwarfCommonAbilityDefinition> _abilitiesBySlot = new Dictionary<int, DwarfCommonAbilityDefinition>();
        readonly Dictionary<int, DwarfCommonAbilityModifierSource> _sources = new Dictionary<int, DwarfCommonAbilityModifierSource>();

        CharacterStats _stats;
        bool _applied;

        public IReadOnlyDictionary<int, DwarfCommonAbilityDefinition> InstalledSnapshot => _abilitiesBySlot;

        /// <summary>Assign preset slots before <see cref="TryApplyPresetFromSerialized"/> (tests, editor tooling).</summary>
        public void SetPresetCommonAbilities(IReadOnlyList<DwarfCommonSlotPreset> presets)
        {
            presetCommonAbilities = presets == null ? new List<DwarfCommonSlotPreset>() : new List<DwarfCommonSlotPreset>(presets);
        }

        void Awake() => _stats = GetComponent<CharacterStats>();

        void Start() => TryApplyPresetFromSerialized();

        void OnDestroy() => RemoveAll();

        public void TryApplyPresetFromSerialized()
        {
            if (!ValidateDwarfActor(out _))
                return;

            RemoveAll();

            if (presetCommonAbilities == null)
                return;

            foreach (DwarfCommonSlotPreset preset in presetCommonAbilities)
            {
                if (preset?.ability == null)
                    continue;
                if (preset.slotIndex < 0 || preset.slotIndex >= SlotCount)
                {
                    Debug.LogWarning($"[DwarfCommon] {name}: slot index {preset.slotIndex} out of range 0–{SlotCount - 1}.");
                    continue;
                }

                if (_abilitiesBySlot.ContainsKey(preset.slotIndex))
                {
                    Debug.LogWarning($"[DwarfCommon] {name}: duplicate preset for slot {preset.slotIndex}; skipping.");
                    continue;
                }

                ApplySlot(preset.slotIndex, preset.ability);
                _abilitiesBySlot[preset.slotIndex] = preset.ability;
            }

            _applied = _abilitiesBySlot.Count > 0;
        }

        public void RefreshPassives()
        {
            foreach (DwarfCommonAbilityDefinition ability in _abilitiesBySlot.Values)
            {
                RacialAbilityPayloadApplicator.RefreshPassives(gameObject, ability.passiveEffects);
            }
        }

        public void NotifyPassivesTurnStart()
        {
            foreach (DwarfCommonAbilityDefinition ability in _abilitiesBySlot.Values)
            {
                RacialAbilityPayloadApplicator.NotifyPassivesTurnStart(gameObject, ability.passiveEffects);
            }
        }

        void ApplySlot(int slotIndex, DwarfCommonAbilityDefinition ability)
        {
            if (!_sources.TryGetValue(slotIndex, out DwarfCommonAbilityModifierSource src))
            {
                src = new DwarfCommonAbilityModifierSource(slotIndex, ability);
                _sources[slotIndex] = src;
            }

            RacialAbilityPayloadApplicator.Apply(
                gameObject,
                _stats,
                src,
                ability.statModifiers,
                ability.resistanceModifiers,
                ability.passiveEffects);
        }

        void RemoveAll()
        {
            if (_stats == null)
                _stats = GetComponent<CharacterStats>();

            foreach (int slot in new List<int>(_abilitiesBySlot.Keys))
                RemoveSlot(slot);

            _abilitiesBySlot.Clear();
            _applied = false;
        }

        void RemoveSlot(int slotIndex)
        {
            if (!_abilitiesBySlot.TryGetValue(slotIndex, out DwarfCommonAbilityDefinition ability))
                return;

            if (_sources.TryGetValue(slotIndex, out DwarfCommonAbilityModifierSource src) && _stats != null)
            {
                RacialAbilityPayloadApplicator.Remove(
                    gameObject,
                    _stats,
                    src,
                    ability.statModifiers,
                    ability.resistanceModifiers,
                    ability.passiveEffects);
            }

            _abilitiesBySlot.Remove(slotIndex);
        }

        bool ValidateDwarfActor(out string failureReason)
        {
            failureReason = null;
            if (_stats == null)
            {
                failureReason = "No CharacterStats.";
                return false;
            }

            if (_stats.race != Race.Dwarf)
            {
                failureReason = "Not a Dwarf.";
                return false;
            }

            if (requireDwarfAncestrySubsystem &&
                _stats.racialSubsystem != RacialSubsystemKind.DwarfAncestry)
            {
                failureReason = "Racial subsystem is not DwarfAncestry.";
                return false;
            }

            return true;
        }
    }
}
