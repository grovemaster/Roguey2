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
    /// Elf elemental spirit contracts: roster instances, summon/dismiss, upkeep, cumulative level payloads while summoned.
    /// </summary>
    [DefaultExecutionOrder(51)]
    public class ElementalSpiritContractsRuntime : MonoBehaviour
    {
        [SerializeField] List<ElementalSpiritContractPreset> contractedSpirits = new List<ElementalSpiritContractPreset>();

        [Tooltip("Optional: contract instance ids or legacy spirit ids summoned when the actor enters play.")]
        [SerializeField] List<string> summonedSpiritIdsOnStart = new List<string>();

        [SerializeField] bool requireElfElementalSubsystem = true;

        readonly Dictionary<string, ElementalSpiritModifierSource> _modifierSources =
            new Dictionary<string, ElementalSpiritModifierSource>();

        readonly HashSet<string> _summonedInstanceIds = new HashSet<string>();
        readonly Dictionary<string, bool> _toggleActiveByKey = new Dictionary<string, bool>();

        string _fireImbueInstanceId;
        int _weaponFireImbueBonus;

        CharacterStats _stats;

        public IReadOnlyList<ElementalSpiritContractPreset> ContractedSpirits => contractedSpirits;

        public IReadOnlyCollection<string> SummonedContractInstanceIds => _summonedInstanceIds;

        /// <summary>Legacy: distinct spirit ids among summoned instances.</summary>
        public IReadOnlyCollection<string> SummonedSpiritIds => GetSummonedSpiritIdsSnapshot();

        public int WeaponFireImbueBonus => _weaponFireImbueBonus;

        void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            EnsureAllContractInstanceIds();
        }

        void Start()
        {
            if (summonedSpiritIdsOnStart == null || summonedSpiritIdsOnStart.Count == 0)
                return;

            foreach (string id in summonedSpiritIdsOnStart)
            {
                if (string.IsNullOrEmpty(id))
                    continue;

                if (TryResolveInstanceId(id, out string instanceId))
                    TrySummonInstance(instanceId, out _);
            }
        }

        void OnDestroy()
        {
            foreach (string id in new List<string>(_summonedInstanceIds))
                DismissInstance(id);
        }

        public bool TryFormContract(
            ElementalSpiritDefinition spirit,
            int initialLevel,
            out string contractInstanceId,
            out string failureReason)
        {
            contractInstanceId = null;
            failureReason = null;

            if (!ValidateElfActor(out failureReason))
                return false;

            if (spirit == null || string.IsNullOrEmpty(spirit.spiritId))
            {
                failureReason = "Invalid spirit definition.";
                return false;
            }

            int level = Mathf.Clamp(initialLevel, 1, spirit.maxLevel);
            var preset = new ElementalSpiritContractPreset
            {
                spirit = spirit,
                contractLevel = level,
                contractExperience = 0,
            };
            preset.EnsureInstanceId();
            contractedSpirits.Add(preset);
            contractInstanceId = preset.contractInstanceId;
            return true;
        }

        public bool IsInstanceSummoned(string contractInstanceId) =>
            !string.IsNullOrEmpty(contractInstanceId) && _summonedInstanceIds.Contains(contractInstanceId);

        public bool IsSpiritSummoned(string spiritId)
        {
            if (string.IsNullOrEmpty(spiritId))
                return false;

            foreach (string instanceId in _summonedInstanceIds)
            {
                if (TryGetPreset(instanceId, out ElementalSpiritContractPreset preset)
                    && preset.spirit != null
                    && preset.spirit.spiritId == spiritId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPreset(string contractInstanceId, out ElementalSpiritContractPreset preset)
        {
            preset = null;
            if (string.IsNullOrEmpty(contractInstanceId))
                return false;

            foreach (ElementalSpiritContractPreset candidate in contractedSpirits)
            {
                if (candidate == null)
                    continue;
                candidate.EnsureInstanceId();
                if (candidate.contractInstanceId == contractInstanceId)
                {
                    preset = candidate;
                    return true;
                }
            }

            return false;
        }

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

        public bool TryGetContractLevelForInstance(string contractInstanceId, out int level)
        {
            level = 0;
            if (!TryGetPreset(contractInstanceId, out ElementalSpiritContractPreset preset) || preset.spirit == null)
                return false;

            level = Mathf.Clamp(preset.contractLevel, 1, preset.spirit.maxLevel);
            return true;
        }

        public bool TrySummonInstance(string contractInstanceId, out string failureReason)
        {
            failureReason = null;
            if (!ValidateElfActor(out failureReason))
                return false;

            if (!TryGetPreset(contractInstanceId, out ElementalSpiritContractPreset preset) || preset.spirit == null)
            {
                failureReason = "Spirit instance is not contracted.";
                return false;
            }

            if (_summonedInstanceIds.Contains(contractInstanceId))
            {
                failureReason = "Spirit instance is already summoned.";
                return false;
            }

            ElementalSpiritDefinition def = preset.spirit;
            int contractLevel = Mathf.Clamp(preset.contractLevel, 1, def.maxLevel);

            if (_stats.currentSoulPower < def.summonSoulPowerCost)
            {
                failureReason = "Not enough Soul Power to summon.";
                return false;
            }

            _stats.currentSoulPower -= def.summonSoulPowerCost;
            ApplySummonedPayload(contractInstanceId, def, contractLevel);
            _summonedInstanceIds.Add(contractInstanceId);
            return true;
        }

        public bool TrySummon(string spiritId, out string failureReason)
        {
            failureReason = null;
            foreach (ElementalSpiritContractPreset preset in contractedSpirits)
            {
                if (preset?.spirit == null || preset.spirit.spiritId != spiritId)
                    continue;
                preset.EnsureInstanceId();
                if (_summonedInstanceIds.Contains(preset.contractInstanceId))
                    continue;
                return TrySummonInstance(preset.contractInstanceId, out failureReason);
            }

            failureReason = $"Spirit '{spiritId}' is not contracted.";
            return false;
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

        public bool TryDismissInstance(string contractInstanceId)
        {
            if (string.IsNullOrEmpty(contractInstanceId) || !_summonedInstanceIds.Contains(contractInstanceId))
                return false;
            DismissInstance(contractInstanceId);
            return true;
        }

        public bool TryDismiss(string spiritId)
        {
            if (string.IsNullOrEmpty(spiritId))
                return false;

            foreach (ElementalSpiritContractPreset preset in contractedSpirits)
            {
                if (preset?.spirit == null || preset.spirit.spiritId != spiritId)
                    continue;
                preset.EnsureInstanceId();
                if (_summonedInstanceIds.Contains(preset.contractInstanceId))
                    return TryDismissInstance(preset.contractInstanceId);
            }

            return false;
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
            if (_summonedInstanceIds.Count == 0 || _stats == null)
                return;

            PayUpkeepThenPassivesTurnStart();
        }

        public void RefreshPassives()
        {
            if (_summonedInstanceIds.Count == 0)
                return;

            foreach (string instanceId in _summonedInstanceIds)
            {
                if (!TryGetPreset(instanceId, out ElementalSpiritContractPreset preset) || preset.spirit == null)
                    continue;
                int contractLevel = Mathf.Clamp(preset.contractLevel, 1, preset.spirit.maxLevel);
                RefreshPassivesForSpirit(preset.spirit, contractLevel);
            }
        }

        public bool TryToggleFireWeaponImbue(string spiritId, FireWeaponImbueAbility ability, int fireBonus) =>
            TryToggleFireWeaponImbueForInstance(FindFirstSummonedInstanceForSpirit(spiritId), ability, fireBonus);

        public bool TryToggleFireWeaponImbueForInstance(
            string contractInstanceId,
            FireWeaponImbueAbility ability,
            int fireBonus)
        {
            if (!IsInstanceSummoned(contractInstanceId) || ability == null)
                return false;

            string key = ToggleKey(contractInstanceId, ability);
            bool nowActive = _toggleActiveByKey.TryGetValue(key, out bool active) && active;
            if (nowActive)
            {
                _toggleActiveByKey[key] = false;
                if (_fireImbueInstanceId == contractInstanceId)
                {
                    _fireImbueInstanceId = null;
                    _weaponFireImbueBonus = 0;
                }

                return true;
            }

            if (ability.soulPowerCost > 0 && _stats.currentSoulPower < ability.soulPowerCost)
                return false;

            if (ability.soulPowerCost > 0)
                _stats.currentSoulPower -= ability.soulPowerCost;

            _toggleActiveByKey[key] = true;
            _fireImbueInstanceId = contractInstanceId;
            _weaponFireImbueBonus = fireBonus;
            return true;
        }

        public bool CanExecuteSpiritActive(string spiritId, AbilityAction ability) =>
            CanExecuteSpiritActiveForAbility(ability);

        public bool CanExecuteSpiritActiveForAbility(AbilityAction ability)
        {
            if (ability == null)
                return false;

            return TryFindSummonedInstanceForAbility(ability, out _, out _);
        }

        public bool TryExecuteSpiritActiveForAbility(AbilityAction ability)
        {
            if (ability == null)
                return false;

            if (!TryFindSummonedInstanceForAbility(ability, out string instanceId, out ElementalSpiritDefinition def))
                return false;

            if (!TryGetContractLevelForInstance(instanceId, out int contractLevel))
                return false;

            if (FindActiveEntry(def, contractLevel, ability) == null)
                return false;

            if (ability is FireWeaponImbueAbility imbue)
                return TryToggleFireWeaponImbueForInstance(instanceId, imbue, imbue.fireDamageBonus);

            if (!ability.CanExecute(gameObject))
                return false;

            if (!ability.Execute(gameObject))
                return false;

            return TrySpendAbilitySoulPower(ability);
        }

        bool TrySpendAbilitySoulPower(AbilityAction ability)
        {
            if (_stats == null || ability == null)
                return false;

            int cost = ability.soulPowerCost;
            if (cost <= 0)
                return true;

            if (_stats.currentSoulPower < cost)
                return false;

            _stats.currentSoulPower -= cost;
            return true;
        }

        public bool SpiritActiveConsumesTurn(AbilityAction ability)
        {
            if (ability == null)
                return true;

            foreach (string instanceId in GetSummonedInstancesInContractOrder())
            {
                if (!TryGetPreset(instanceId, out ElementalSpiritContractPreset preset) || preset.spirit == null)
                    continue;

                if (!TryGetContractLevelForInstance(instanceId, out int contractLevel))
                    continue;

                ElementalSpiritActiveEntry entry = FindActiveEntry(preset.spirit, contractLevel, ability);
                if (entry != null)
                    return entry.consumesTurn;
            }

            return true;
        }

        public int ResolveContractLevelUps(
            string contractInstanceId,
            int effectiveCap,
            ElementalSpiritLevelCurve curve)
        {
            if (!TryGetPreset(contractInstanceId, out ElementalSpiritContractPreset preset)
                || preset.spirit == null
                || curve == null)
            {
                return 0;
            }

            int levelsGained = 0;
            int cap = Mathf.Clamp(effectiveCap, 1, preset.spirit.maxLevel);

            while (preset.contractLevel < cap)
            {
                int threshold = curve.GetXpRequiredForNextLevel(preset.contractLevel);
                if (threshold == int.MaxValue || preset.contractExperience < threshold)
                    break;

                preset.contractExperience -= threshold;
                preset.contractLevel = Mathf.Clamp(preset.contractLevel + 1, 1, preset.spirit.maxLevel);
                levelsGained++;

                if (IsInstanceSummoned(contractInstanceId))
                    ReapplySummonedPayload(contractInstanceId);
            }

            return levelsGained;
        }

        public bool TryGetSpiritDefinition(string spiritId, out ElementalSpiritDefinition def)
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

        public bool TryFindSummonedInstanceForAbility(
            AbilityAction ability,
            out string contractInstanceId,
            out ElementalSpiritDefinition definition)
        {
            contractInstanceId = null;
            definition = null;
            if (ability == null)
                return false;

            foreach (string instanceId in GetSummonedInstancesInContractOrder())
            {
                if (!TryGetPreset(instanceId, out ElementalSpiritContractPreset preset) || preset.spirit == null)
                    continue;

                if (!TryGetContractLevelForInstance(instanceId, out int contractLevel))
                    continue;

                if (FindActiveEntry(preset.spirit, contractLevel, ability) == null)
                    continue;

                contractInstanceId = instanceId;
                definition = preset.spirit;
                return true;
            }

            return false;
        }

        void PayUpkeepThenPassivesTurnStart()
        {
            foreach (string instanceId in GetSummonedInstancesInContractOrder())
            {
                if (!TryGetPreset(instanceId, out ElementalSpiritContractPreset preset) || preset.spirit == null)
                    continue;

                ElementalSpiritDefinition def = preset.spirit;
                if (_stats.currentSoulPower >= def.upkeepSoulPowerPerTurn)
                    _stats.currentSoulPower -= def.upkeepSoulPowerPerTurn;
                else
                    DismissInstance(instanceId);
            }

            foreach (string instanceId in _summonedInstanceIds)
            {
                if (!TryGetPreset(instanceId, out ElementalSpiritContractPreset preset) || preset.spirit == null)
                    continue;
                int contractLevel = Mathf.Clamp(preset.contractLevel, 1, preset.spirit.maxLevel);
                NotifyPassivesTurnStartForSpirit(preset.spirit, contractLevel);
            }
        }

        IEnumerable<string> GetSummonedInstancesInContractOrder()
        {
            var yielded = new HashSet<string>();
            foreach (ElementalSpiritContractPreset preset in contractedSpirits)
            {
                if (preset?.spirit == null)
                    continue;
                preset.EnsureInstanceId();
                if (_summonedInstanceIds.Contains(preset.contractInstanceId) && yielded.Add(preset.contractInstanceId))
                    yield return preset.contractInstanceId;
            }

            foreach (string id in _summonedInstanceIds)
            {
                if (yielded.Add(id))
                    yield return id;
            }
        }

        void ApplySummonedPayload(string contractInstanceId, ElementalSpiritDefinition def, int contractLevel)
        {
            if (!_modifierSources.TryGetValue(contractInstanceId, out ElementalSpiritModifierSource src))
            {
                src = new ElementalSpiritModifierSource(def, contractInstanceId);
                _modifierSources[contractInstanceId] = src;
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

        void DismissInstance(string contractInstanceId)
        {
            RemoveSummonedPayload(contractInstanceId);
            _summonedInstanceIds.Remove(contractInstanceId);
        }

        void ReapplySummonedPayload(string contractInstanceId)
        {
            if (!IsInstanceSummoned(contractInstanceId)
                || !TryGetPreset(contractInstanceId, out ElementalSpiritContractPreset preset)
                || preset.spirit == null)
            {
                return;
            }

            RemoveSummonedPayload(contractInstanceId, removeFromSummonedSet: false);
            int contractLevel = Mathf.Clamp(preset.contractLevel, 1, preset.spirit.maxLevel);
            ApplySummonedPayload(contractInstanceId, preset.spirit, contractLevel);
        }

        void RemoveSummonedPayload(string contractInstanceId, bool removeFromSummonedSet = true)
        {
            if (!TryGetPreset(contractInstanceId, out ElementalSpiritContractPreset preset) || preset.spirit == null)
            {
                if (removeFromSummonedSet)
                    _summonedInstanceIds.Remove(contractInstanceId);
                return;
            }

            ElementalSpiritDefinition def = preset.spirit;
            int contractLevel = Mathf.Clamp(preset.contractLevel, 1, def.maxLevel);

            if (_modifierSources.TryGetValue(contractInstanceId, out ElementalSpiritModifierSource src))
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

            ClearToggleStateForInstance(contractInstanceId);

            if (removeFromSummonedSet)
                _summonedInstanceIds.Remove(contractInstanceId);
        }

        void ClearToggleStateForInstance(string contractInstanceId)
        {
            if (_fireImbueInstanceId == contractInstanceId)
            {
                _fireImbueInstanceId = null;
                _weaponFireImbueBonus = 0;
            }

            var keys = new List<string>();
            foreach (string key in _toggleActiveByKey.Keys)
            {
                if (key.StartsWith(contractInstanceId + ":", StringComparison.Ordinal))
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

        void EnsureAllContractInstanceIds()
        {
            if (contractedSpirits == null)
                return;

            foreach (ElementalSpiritContractPreset preset in contractedSpirits)
                preset?.EnsureInstanceId();
        }

        bool TryResolveInstanceId(string idOrSpiritId, out string contractInstanceId)
        {
            contractInstanceId = null;
            if (string.IsNullOrEmpty(idOrSpiritId))
                return false;

            if (TryGetPreset(idOrSpiritId, out _))
            {
                contractInstanceId = idOrSpiritId;
                return true;
            }

            foreach (ElementalSpiritContractPreset preset in contractedSpirits)
            {
                if (preset?.spirit == null || preset.spirit.spiritId != idOrSpiritId)
                    continue;
                preset.EnsureInstanceId();
                contractInstanceId = preset.contractInstanceId;
                return true;
            }

            return false;
        }

        string FindFirstSummonedInstanceForSpirit(string spiritId)
        {
            if (string.IsNullOrEmpty(spiritId))
                return null;

            foreach (string instanceId in GetSummonedInstancesInContractOrder())
            {
                if (TryGetPreset(instanceId, out ElementalSpiritContractPreset preset)
                    && preset.spirit != null
                    && preset.spirit.spiritId == spiritId)
                {
                    return instanceId;
                }
            }

            return null;
        }

        HashSet<string> GetSummonedSpiritIdsSnapshot()
        {
            var ids = new HashSet<string>();
            foreach (string instanceId in _summonedInstanceIds)
            {
                if (TryGetPreset(instanceId, out ElementalSpiritContractPreset preset)
                    && preset.spirit != null
                    && !string.IsNullOrEmpty(preset.spirit.spiritId))
                {
                    ids.Add(preset.spirit.spiritId);
                }
            }

            return ids;
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

        static string ToggleKey(string contractInstanceId, AbilityAction ability) =>
            $"{contractInstanceId}:{ability.name}";
    }
}
