using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Mage known/equipped spell library and equip-budget validation.
    /// </summary>
    [DefaultExecutionOrder(53)]
    public class HumanMageSpellsRuntime : MonoBehaviour
    {
        [SerializeField] List<MageSpellDefinition> knownSpells = new List<MageSpellDefinition>();
        [SerializeField] List<string> presetEquippedSpellIds = new List<string>();

        readonly List<MageSpellDefinition> _equipped = new List<MageSpellDefinition>();

        CharacterStats _stats;

        public IReadOnlyList<MageSpellDefinition> EquippedSpells => _equipped;
        public int RemainingEquipCapacity =>
            _stats != null ? Mathf.Max(0, _stats.MaxMagicPower - GetTotalEquippedCost()) : 0;

        void Awake() => _stats = GetComponent<CharacterStats>();

        void Start() => RebuildEquippedFromPreset();

        public void SetKnownAndEquipped(
            IReadOnlyList<MageSpellDefinition> known,
            IReadOnlyList<string> equippedIds)
        {
            knownSpells = known == null
                ? new List<MageSpellDefinition>()
                : new List<MageSpellDefinition>(known);
            presetEquippedSpellIds = equippedIds == null
                ? new List<string>()
                : new List<string>(equippedIds);
            RebuildEquippedFromPreset();
        }

        public void RebuildEquippedFromPreset()
        {
            _equipped.Clear();
            if (!ValidateMageActor(out _))
                return;

            if (presetEquippedSpellIds == null)
                return;

            foreach (string id in presetEquippedSpellIds)
            {
                if (TryFindKnown(id, out MageSpellDefinition spell))
                    TryEquipInternal(spell, logFailure: false);
            }
        }

        public bool TryEquip(string spellId, out string failureReason)
        {
            failureReason = null;
            if (!ValidateMageActor(out failureReason))
                return false;

            if (!TryFindKnown(spellId, out MageSpellDefinition spell))
            {
                failureReason = $"Unknown spell '{spellId}'.";
                return false;
            }

            return TryEquipInternal(spell, logFailure: true, out failureReason);
        }

        public bool TryUnequip(string spellId)
        {
            for (int i = 0; i < _equipped.Count; i++)
            {
                if (_equipped[i] != null && _equipped[i].spellId == spellId)
                {
                    _equipped.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public AbilityAction GetEquippedAbility(int equippedIndex)
        {
            if (equippedIndex < 0 || equippedIndex >= _equipped.Count)
                return null;
            return _equipped[equippedIndex]?.ability;
        }

        public MageSpellDefinition GetEquippedSpell(int equippedIndex)
        {
            if (equippedIndex < 0 || equippedIndex >= _equipped.Count)
                return null;
            return _equipped[equippedIndex];
        }

        public bool CanAffordCast(int equippedIndex)
        {
            MageSpellDefinition spell = GetEquippedSpell(equippedIndex);
            if (spell == null || _stats == null)
                return false;
            return _stats.currentMagicPower >= spell.magicPowerCost;
        }

        public bool TryExecuteEquipped(int equippedIndex, GameObject user, Vector3Int targetTile)
        {
            MageSpellDefinition spell = GetEquippedSpell(equippedIndex);
            if (spell?.ability == null || user == null || _stats == null)
                return false;

            if (!ValidateMageActor(out _))
                return false;

            if (_stats.currentMagicPower < spell.magicPowerCost)
            {
                Debug.Log("Not enough Magic Power!");
                return false;
            }

            if (!spell.ability.CanExecute(user))
                return false;

            if (!spell.ability.Execute(user, targetTile))
                return false;

            _stats.currentMagicPower -= spell.magicPowerCost;
            return true;
        }

        public bool TryExecuteEquipped(int equippedIndex, GameObject user)
        {
            MageSpellDefinition spell = GetEquippedSpell(equippedIndex);
            if (spell?.ability == null || user == null || _stats == null)
                return false;

            if (!ValidateMageActor(out _))
                return false;

            if (_stats.currentMagicPower < spell.magicPowerCost)
            {
                Debug.Log("Not enough Magic Power!");
                return false;
            }

            if (!spell.ability.CanExecute(user))
                return false;

            if (!spell.ability.Execute(user))
                return false;

            _stats.currentMagicPower -= spell.magicPowerCost;
            return true;
        }

        int GetTotalEquippedCost()
        {
            int total = 0;
            for (int i = 0; i < _equipped.Count; i++)
            {
                if (_equipped[i] != null)
                    total += _equipped[i].EquipCost;
            }

            return total;
        }

        bool TryEquipInternal(MageSpellDefinition spell, bool logFailure)
        {
            return TryEquipInternal(spell, logFailure, out _);
        }

        bool TryEquipInternal(MageSpellDefinition spell, bool logFailure, out string failureReason)
        {
            failureReason = null;
            if (spell == null)
                return false;

            foreach (MageSpellDefinition e in _equipped)
            {
                if (e != null && e.spellId == spell.spellId)
                    return true;
            }

            if (RemainingEquipCapacity < spell.EquipCost)
            {
                failureReason =
                    $"Cannot equip {spell.displayName}: need {spell.EquipCost} capacity, have {RemainingEquipCapacity}.";
                if (logFailure)
                    Debug.LogWarning($"[Mage] {failureReason}");
                return false;
            }

            _equipped.Add(spell);
            return true;
        }

        bool TryFindKnown(string spellId, out MageSpellDefinition spell)
        {
            spell = null;
            if (string.IsNullOrEmpty(spellId) || knownSpells == null)
                return false;

            for (int i = 0; i < knownSpells.Count; i++)
            {
                MageSpellDefinition s = knownSpells[i];
                if (s != null && s.spellId == spellId)
                {
                    spell = s;
                    return true;
                }
            }

            return false;
        }

        bool ValidateMageActor(out string failureReason)
        {
            failureReason = null;
            if (_stats == null)
            {
                failureReason = "No CharacterStats.";
                return false;
            }

            if (_stats.race != Race.Human || _stats.humanClass != HumanClass.Mage)
            {
                failureReason = "HumanMageSpellsRuntime requires Human Mage.";
                return false;
            }

            return true;
        }
    }
}
