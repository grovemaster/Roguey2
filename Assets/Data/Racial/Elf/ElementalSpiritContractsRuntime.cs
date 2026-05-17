using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Elf elemental spirit contracts: preset roster, summon/dismiss, upkeep, cumulative level payloads while summoned.
    /// </summary>
    [DefaultExecutionOrder(51)]
    public class ElementalSpiritContractsRuntime : MonoBehaviour
    {
        [SerializeField] List<ElementalSpiritContractPreset> contractedSpirits = new List<ElementalSpiritContractPreset>();

        [Tooltip("Optional: spirits summoned when the actor enters play.")]
        [SerializeField] List<string> summonedSpiritIdsOnStart = new List<string>();

        [SerializeField] bool requireElfElementalSubsystem = true;

        readonly Dictionary<string, ElementalSpiritModifierSource> _modifierSources =
            new Dictionary<string, ElementalSpiritModifierSource>();

        readonly HashSet<string> _summonedSpiritIds = new HashSet<string>();
        readonly Dictionary<string, bool> _toggleActiveByKey = new Dictionary<string, bool>();

        string _fireImbueSpiritId;
        int _weaponFireImbueBonus;

        CharacterStats _stats;

        public IReadOnlyCollection<string> SummonedSpiritIds => _summonedSpiritIds;

        public int WeaponFireImbueBonus => _weaponFireImbueBonus;

        void Awake()
        {
            _stats = GetComponent<CharacterStats>();
        }

        void Start()
        {
            if (summonedSpiritIdsOnStart == null || summonedSpiritIdsOnStart.Count == 0)
                return;
            foreach (string id in summonedSpiritIdsOnStart)
            {
                if (!string.IsNullOrEmpty(id))
                    TrySummon(id, out _);
            }
        }

        void OnDestroy()
        {
            foreach (string id in new List<string>(_summonedSpiritIds))
                DismissSpirit(id);
        }

        public bool IsSpiritSummoned(string spiritId) =>
            !string.IsNullOrEmpty(spiritId) && _summonedSpiritIds.Contains(spiritId);

        public bool TryGetContractLevel(string spiritId, out int level)
        {
            level = 0;
            if (string.IsNullOrEmpty(spiritId))
                return false;
            foreach (ElementalSpiritContractPreset preset in contractedSpirits)
            {
                if (preset?.spirit == null || preset.spirit.spiritId != spiritId)
                    continue;
                level = Mathf.Clamp(preset.contractLevel, 1, preset.spirit.maxLevel);
                return true;
            }

            return false;
        }

        public bool TrySummon(string spiritId, out string failureReason)
        {
            failureReason = null;
            if (!ValidateElfActor(out failureReason))
                return false;

            if (!TryGetContractLevel(spiritId, out int contractLevel))
            {
                failureReason = $"Spirit '{spiritId}' is not contracted.";
                return false;
            }

            if (_summonedSpiritIds.Contains(spiritId))
            {
                failureReason = $"Spirit '{spiritId}' is already summoned.";
                return false;
            }

            if (!TryGetSpiritDefinition(spiritId, out ElementalSpiritDefinition def))
            {
                failureReason = $"Unknown spirit '{spiritId}'.";
                return false;
            }

            if (_stats.currentSoulPower < def.summonSoulPowerCost)
            {
                failureReason = "Not enough Soul Power to summon.";
                return false;
            }

            _stats.currentSoulPower -= def.summonSoulPowerCost;
            ApplySummonedPayload(def, contractLevel);
            _summonedSpiritIds.Add(spiritId);
            return true;
        }

        public int TrySummonBatch(IReadOnlyList<string> spiritIdsInOrder)
        {
            if (spiritIdsInOrder == null)
                return 0;
            int count = 0;
            foreach (string id in spiritIdsInOrder)
            {
                if (TrySummon(id, out _))
                    count++;
            }

            return count;
        }

        public bool TryDismiss(string spiritId)
        {
            if (string.IsNullOrEmpty(spiritId) || !_summonedSpiritIds.Contains(spiritId))
                return false;
            DismissSpirit(spiritId);
            return true;
        }

        public int TryDismissBatch(IReadOnlyList<string> spiritIds)
        {
            if (spiritIds == null)
                return 0;
            int count = 0;
            foreach (string id in spiritIds)
            {
                if (TryDismiss(id))
                    count++;
            }

            return count;
        }

        public void NotifyTurnStart()
        {
            if (_summonedSpiritIds.Count == 0 || _stats == null)
                return;

            PayUpkeepThenPassivesTurnStart();
        }

        public void RefreshPassives()
        {
            if (_summonedSpiritIds.Count == 0)
                return;

            foreach (string spiritId in _summonedSpiritIds)
            {
                if (!TryGetSpiritDefinition(spiritId, out ElementalSpiritDefinition def))
                    continue;
                if (!TryGetContractLevel(spiritId, out int contractLevel))
                    continue;
                RefreshPassivesForSpirit(def, contractLevel);
            }
        }

        public bool TryToggleFireWeaponImbue(string spiritId, FireWeaponImbueAbility ability, int fireBonus)
        {
            if (!IsSpiritSummoned(spiritId) || ability == null)
                return false;

            string key = ToggleKey(spiritId, ability);
            bool nowActive = _toggleActiveByKey.TryGetValue(key, out bool active) && active;
            if (nowActive)
            {
                _toggleActiveByKey[key] = false;
                if (_fireImbueSpiritId == spiritId)
                {
                    _fireImbueSpiritId = null;
                    _weaponFireImbueBonus = 0;
                }

                return true;
            }

            if (ability.soulPowerCost > 0 && _stats.currentSoulPower < ability.soulPowerCost)
                return false;

            if (ability.soulPowerCost > 0)
                _stats.currentSoulPower -= ability.soulPowerCost;

            _toggleActiveByKey[key] = true;
            _fireImbueSpiritId = spiritId;
            _weaponFireImbueBonus = fireBonus;
            return true;
        }

        public bool CanExecuteSpiritActive(string spiritId, AbilityAction ability)
        {
            if (ability == null || !IsSpiritSummoned(spiritId))
                return false;
            if (!TryGetSpiritDefinition(spiritId, out ElementalSpiritDefinition def))
                return false;
            if (!TryGetContractLevel(spiritId, out int contractLevel))
                return false;
            return FindActiveEntry(def, contractLevel, ability) != null && ability.CanExecute(gameObject);
        }

        void PayUpkeepThenPassivesTurnStart()
        {
            foreach (string spiritId in GetSummonedInContractOrder())
            {
                if (!TryGetSpiritDefinition(spiritId, out ElementalSpiritDefinition def))
                    continue;

                if (_stats.currentSoulPower >= def.upkeepSoulPowerPerTurn)
                    _stats.currentSoulPower -= def.upkeepSoulPowerPerTurn;
                else
                    DismissSpirit(spiritId);
            }

            foreach (string spiritId in _summonedSpiritIds)
            {
                if (!TryGetSpiritDefinition(spiritId, out ElementalSpiritDefinition def))
                    continue;
                if (!TryGetContractLevel(spiritId, out int contractLevel))
                    continue;
                NotifyPassivesTurnStartForSpirit(def, contractLevel);
            }
        }

        IEnumerable<string> GetSummonedInContractOrder()
        {
            var yielded = new HashSet<string>();
            foreach (ElementalSpiritContractPreset preset in contractedSpirits)
            {
                if (preset?.spirit == null)
                    continue;
                string id = preset.spirit.spiritId;
                if (_summonedSpiritIds.Contains(id) && yielded.Add(id))
                    yield return id;
            }

            foreach (string id in _summonedSpiritIds)
            {
                if (yielded.Add(id))
                    yield return id;
            }
        }

        void ApplySummonedPayload(ElementalSpiritDefinition def, int contractLevel)
        {
            if (!_modifierSources.TryGetValue(def.spiritId, out ElementalSpiritModifierSource src))
            {
                src = new ElementalSpiritModifierSource(def);
                _modifierSources[def.spiritId] = src;
            }

            for (int level = 1; level <= contractLevel; level++)
            {
                if (!def.TryGetLevelRow(level, out ElementalSpiritLevelData row))
                    continue;

                if (row.statModifiers != null)
                {
                    foreach (AttributeModifier mod in row.statModifiers)
                    {
                        Stat targetStat = _stats.GetStatByType(mod.attribute);
                        targetStat?.AddModifier(mod.value, src);
                    }
                }

                if (row.resistanceModifiers != null)
                {
                    foreach (DamageResistanceModifier res in row.resistanceModifiers)
                        _stats.AddResistanceModifier(res.type, res.value, src);
                }

                if (row.passiveEffects != null)
                {
                    foreach (PassiveEffect passive in row.passiveEffects)
                        passive?.OnApply(gameObject);
                }
            }
        }

        void DismissSpirit(string spiritId)
        {
            if (!TryGetSpiritDefinition(spiritId, out ElementalSpiritDefinition def))
            {
                _summonedSpiritIds.Remove(spiritId);
                return;
            }

            if (!TryGetContractLevel(spiritId, out int contractLevel))
                contractLevel = def.maxLevel;

            if (_modifierSources.TryGetValue(spiritId, out ElementalSpiritModifierSource src))
            {
                for (int level = 1; level <= contractLevel; level++)
                {
                    if (!def.TryGetLevelRow(level, out ElementalSpiritLevelData row))
                        continue;

                    if (row.statModifiers != null)
                    {
                        foreach (AttributeModifier mod in row.statModifiers)
                        {
                            Stat targetStat = _stats.GetStatByType(mod.attribute);
                            targetStat?.RemoveModifiersFromSource(src);
                        }
                    }

                    if (row.resistanceModifiers != null)
                    {
                        foreach (DamageResistanceModifier res in row.resistanceModifiers)
                            _stats.RemoveResistanceModifier(res.type, src);
                    }
                }
            }

            for (int level = 1; level <= contractLevel; level++)
            {
                if (!def.TryGetLevelRow(level, out ElementalSpiritLevelData row) || row.passiveEffects == null)
                    continue;
                for (int i = row.passiveEffects.Count - 1; i >= 0; i--)
                    row.passiveEffects[i]?.OnRemove(gameObject);
            }

            ClearToggleStateForSpirit(spiritId);
            _summonedSpiritIds.Remove(spiritId);
        }

        void ClearToggleStateForSpirit(string spiritId)
        {
            if (_fireImbueSpiritId == spiritId)
            {
                _fireImbueSpiritId = null;
                _weaponFireImbueBonus = 0;
            }

            var keys = new List<string>();
            foreach (string key in _toggleActiveByKey.Keys)
            {
                if (key.StartsWith(spiritId + ":", StringComparison.Ordinal))
                    keys.Add(key);
            }

            foreach (string key in keys)
                _toggleActiveByKey.Remove(key);
        }

        void RefreshPassivesForSpirit(ElementalSpiritDefinition def, int contractLevel)
        {
            for (int level = 1; level <= contractLevel; level++)
            {
                if (!def.TryGetLevelRow(level, out ElementalSpiritLevelData row) || row.passiveEffects == null)
                    continue;
                foreach (PassiveEffect passive in row.passiveEffects)
                    passive?.Refresh(gameObject);
            }
        }

        void NotifyPassivesTurnStartForSpirit(ElementalSpiritDefinition def, int contractLevel)
        {
            for (int level = 1; level <= contractLevel; level++)
            {
                if (!def.TryGetLevelRow(level, out ElementalSpiritLevelData row) || row.passiveEffects == null)
                    continue;
                foreach (PassiveEffect passive in row.passiveEffects)
                    passive?.OnTurnStart(gameObject);
            }
        }

        static ElementalSpiritActiveEntry FindActiveEntry(
            ElementalSpiritDefinition def,
            int contractLevel,
            AbilityAction ability)
        {
            for (int level = 1; level <= contractLevel; level++)
            {
                if (!def.TryGetLevelRow(level, out ElementalSpiritLevelData row) || row.activeEntries == null)
                    continue;
                foreach (ElementalSpiritActiveEntry entry in row.activeEntries)
                {
                    if (entry?.ability == ability)
                        return entry;
                }
            }

            return null;
        }

        bool TryGetSpiritDefinition(string spiritId, out ElementalSpiritDefinition def)
        {
            def = null;
            foreach (ElementalSpiritContractPreset preset in contractedSpirits)
            {
                if (preset?.spirit != null && preset.spirit.spiritId == spiritId)
                {
                    def = preset.spirit;
                    return true;
                }
            }

            return false;
        }

        bool ValidateElfActor(out string failureReason)
        {
            failureReason = null;
            if (_stats == null)
            {
                failureReason = "No CharacterStats.";
                return false;
            }

            if (_stats.race != Race.Elf)
            {
                failureReason = "Not an Elf.";
                return false;
            }

            if (requireElfElementalSubsystem &&
                _stats.racialSubsystem != RacialSubsystemKind.ElfElementalContracts)
            {
                failureReason = "Racial subsystem is not ElfElementalContracts.";
                return false;
            }

            return true;
        }

        static string ToggleKey(string spiritId, AbilityAction ability) =>
            $"{spiritId}:{ability.name}";
    }
}
