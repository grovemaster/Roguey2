using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Dragonian known/memorized spell library and memory-budget validation.
    /// </summary>
    [DefaultExecutionOrder(53)]
    public class DragonianSpellsRuntime : MonoBehaviour
    {
        [SerializeField] List<DragonianSpellDefinition> knownSpells = new List<DragonianSpellDefinition>();
        [SerializeField] List<string> presetMemorizedSpellIds = new List<string>();

        readonly List<DragonianSpellDefinition> _memorized = new List<DragonianSpellDefinition>();

        CharacterStats _stats;

        public IReadOnlyList<DragonianSpellDefinition> KnownSpells => knownSpells;
        public IReadOnlyList<DragonianSpellDefinition> MemorizedSpells => _memorized;
        public int RemainingMemoryCapacity =>
            _stats != null ? Mathf.Max(0, _stats.MaxSoulPower - GetTotalMemorizedCost()) : 0;

        void Awake() => _stats = GetComponent<CharacterStats>();

        void Start() => RebuildMemorizedFromPreset();

        public void SetKnownAndMemorized(
            IReadOnlyList<DragonianSpellDefinition> known,
            IReadOnlyList<string> memorizedIds)
        {
            knownSpells = known == null
                ? new List<DragonianSpellDefinition>()
                : new List<DragonianSpellDefinition>(known);
            presetMemorizedSpellIds = memorizedIds == null
                ? new List<string>()
                : new List<string>(memorizedIds);
            RebuildMemorizedFromPreset();
        }

        public void RebuildMemorizedFromPreset()
        {
            _memorized.Clear();
            if (!ValidateDragonianActor(out _))
                return;

            if (presetMemorizedSpellIds == null)
                return;

            foreach (string id in presetMemorizedSpellIds)
            {
                if (TryFindKnown(id, out DragonianSpellDefinition spell))
                    TryMemorizeInternal(spell, logFailure: false);
            }
        }

        public bool TryMemorize(string spellId, out string failureReason)
        {
            failureReason = null;
            if (!ValidateDragonianActor(out failureReason))
                return false;

            if (!TryFindKnown(spellId, out DragonianSpellDefinition spell))
            {
                failureReason = $"Unknown spell '{spellId}'.";
                return false;
            }

            return TryMemorizeInternal(spell, logFailure: true, out failureReason);
        }

        public bool TryUnmemorize(string spellId)
        {
            for (int i = 0; i < _memorized.Count; i++)
            {
                if (_memorized[i] != null && _memorized[i].spellId == spellId)
                {
                    _memorized.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public AbilityAction GetMemorizedAbility(int memorizedIndex)
        {
            if (memorizedIndex < 0 || memorizedIndex >= _memorized.Count)
                return null;
            return _memorized[memorizedIndex]?.ability;
        }

        public DragonianSpellDefinition GetMemorizedSpell(int memorizedIndex)
        {
            if (memorizedIndex < 0 || memorizedIndex >= _memorized.Count)
                return null;
            return _memorized[memorizedIndex];
        }

        public bool CanAffordCast(int memorizedIndex)
        {
            DragonianSpellDefinition spell = GetMemorizedSpell(memorizedIndex);
            if (spell == null || _stats == null)
                return false;
            return _stats.currentSoulPower >= spell.soulPowerCastCost;
        }

        public bool TryExecuteMemorized(int memorizedIndex, GameObject user, Vector3Int targetTile)
        {
            DragonianSpellDefinition spell = GetMemorizedSpell(memorizedIndex);
            if (spell?.ability == null || user == null || _stats == null)
                return false;

            if (!ValidateDragonianActor(out _))
                return false;

            if (_stats.currentSoulPower < spell.soulPowerCastCost)
            {
                Debug.Log("Not enough Soul Power!");
                return false;
            }

            if (!spell.ability.CanExecute(user))
                return false;

            if (!spell.ability.Execute(user, targetTile))
                return false;

            _stats.currentSoulPower -= spell.soulPowerCastCost;
            return true;
        }

        public bool TryExecuteMemorized(int memorizedIndex, GameObject user)
        {
            DragonianSpellDefinition spell = GetMemorizedSpell(memorizedIndex);
            if (spell?.ability == null || user == null || _stats == null)
                return false;

            if (!ValidateDragonianActor(out _))
                return false;

            if (_stats.currentSoulPower < spell.soulPowerCastCost)
            {
                Debug.Log("Not enough Soul Power!");
                return false;
            }

            if (!spell.ability.CanExecute(user))
                return false;

            if (!spell.ability.Execute(user))
                return false;

            _stats.currentSoulPower -= spell.soulPowerCastCost;
            return true;
        }

        int GetTotalMemorizedCost()
        {
            int total = 0;
            for (int i = 0; i < _memorized.Count; i++)
            {
                if (_memorized[i] != null)
                    total += _memorized[i].memorizeCost;
            }

            return total;
        }

        bool TryMemorizeInternal(DragonianSpellDefinition spell, bool logFailure)
        {
            return TryMemorizeInternal(spell, logFailure, out _);
        }

        bool TryMemorizeInternal(DragonianSpellDefinition spell, bool logFailure, out string failureReason)
        {
            failureReason = null;
            if (spell == null)
                return false;

            foreach (DragonianSpellDefinition memorized in _memorized)
            {
                if (memorized != null && memorized.spellId == spell.spellId)
                    return true;
            }

            if (RemainingMemoryCapacity < spell.memorizeCost)
            {
                failureReason =
                    $"Cannot memorize {spell.displayName}: need {spell.memorizeCost} capacity, have {RemainingMemoryCapacity}.";
                if (logFailure)
                    Debug.LogWarning($"[Dragonian] {failureReason}");
                return false;
            }

            _memorized.Add(spell);
            return true;
        }

        bool TryFindKnown(string spellId, out DragonianSpellDefinition spell)
        {
            spell = null;
            if (string.IsNullOrEmpty(spellId) || knownSpells == null)
                return false;

            for (int i = 0; i < knownSpells.Count; i++)
            {
                DragonianSpellDefinition s = knownSpells[i];
                if (s != null && s.spellId == spellId)
                {
                    spell = s;
                    return true;
                }
            }

            return false;
        }

        bool ValidateDragonianActor(out string failureReason)
        {
            failureReason = null;
            if (_stats == null)
            {
                failureReason = "No CharacterStats.";
                return false;
            }

            if (_stats.race != Race.Dragonian)
            {
                failureReason = "DragonianSpellsRuntime requires Dragonian.";
                return false;
            }

            if (_stats.racialSubsystem != RacialSubsystemKind.DragonianSpells)
            {
                failureReason = "DragonianSpellsRuntime requires DragonianSpells subsystem.";
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        [ContextMenu("Dev/List Memory Budget")]
        void DevListMemoryBudget()
        {
            Debug.Log(
                $"[Dragonian] MaxSoulPower={_stats?.MaxSoulPower ?? 0}, "
                + $"memorized cost={GetTotalMemorizedCost()}, "
                + $"remaining={RemainingMemoryCapacity}, "
                + $"memorized count={_memorized.Count}");
        }
#endif
    }
}
