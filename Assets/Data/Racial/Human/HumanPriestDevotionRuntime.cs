using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>Unlocked and prepared priest invocations (devotion loadout).</summary>
    [DefaultExecutionOrder(53)]
    public sealed class HumanPriestDevotionRuntime : MonoBehaviour
    {
        [SerializeField] List<string> equippedInvocationIds = new();

        readonly List<PriestInvocationDefinition> _equipped = new();

        CharacterStats _stats;
        HumanPriestCovenantRuntime _covenant;

        public IReadOnlyList<PriestInvocationDefinition> EquippedInvocations => _equipped;

        void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _covenant = GetComponent<HumanPriestCovenantRuntime>();
        }

        void Start() => RebuildEquipped();

        public int GetDevotionSlotCap() =>
            PriestPietyLogic.ResolveDevotionSlotCap(_covenant);

        public void RebuildEquipped()
        {
            _equipped.Clear();
            if (!ValidatePriest(out _))
                return;

            if (equippedInvocationIds == null)
                return;

            foreach (string id in equippedInvocationIds)
            {
                if (PriestInvocationCatalogService.TryGetInvocation(id, out PriestInvocationDefinition invocation)
                    && PriestPietyLogic.IsInvocationUnlocked(_stats, _covenant, invocation))
                {
                    TryEquipInternal(invocation, logFailure: false, out _);
                }
            }
        }

        public bool TryEquip(string invocationId, out string failureReason)
        {
            failureReason = null;
            if (!ValidatePriest(out failureReason))
                return false;

            if (!PriestInvocationCatalogService.TryGetInvocation(invocationId, out PriestInvocationDefinition invocation))
            {
                failureReason = $"Unknown invocation '{invocationId}'.";
                return false;
            }

            if (!PriestPietyLogic.IsInvocationUnlocked(_stats, _covenant, invocation))
            {
                failureReason = PriestPietyLogic.BuildLockedReason(_stats, _covenant, invocation);
                return false;
            }

            return TryEquipInternal(invocation, logFailure: true, out failureReason);
        }

        public bool TryUnequip(string invocationId)
        {
            for (int i = 0; i < _equipped.Count; i++)
            {
                if (_equipped[i] != null && _equipped[i].invocationId == invocationId)
                {
                    _equipped.RemoveAt(i);
                    equippedInvocationIds?.Remove(invocationId);
                    return true;
                }
            }

            return false;
        }

        public void SetEquippedIds(IReadOnlyList<string> ids)
        {
            equippedInvocationIds = ids == null
                ? new List<string>()
                : new List<string>(ids);
            RebuildEquipped();
        }

        public AbilityAction GetEquippedAbility(int equippedIndex)
        {
            if (equippedIndex < 0 || equippedIndex >= _equipped.Count)
                return null;

            return _equipped[equippedIndex]?.ability;
        }

        public PriestInvocationDefinition GetEquippedInvocation(int equippedIndex)
        {
            if (equippedIndex < 0 || equippedIndex >= _equipped.Count)
                return null;

            return _equipped[equippedIndex];
        }

        bool TryEquipInternal(
            PriestInvocationDefinition invocation,
            bool logFailure,
            out string failureReason)
        {
            failureReason = null;
            if (invocation == null)
                return false;

            foreach (PriestInvocationDefinition e in _equipped)
            {
                if (e != null && e.invocationId == invocation.invocationId)
                    return true;
            }

            int cap = GetDevotionSlotCap();
            if (_equipped.Count >= cap)
            {
                failureReason =
                    $"Cannot prepare {invocation.displayName}: devotion slots { _equipped.Count}/{cap}.";
                if (logFailure)
                    Debug.LogWarning($"[Priest] {failureReason}");
                return false;
            }

            _equipped.Add(invocation);
            equippedInvocationIds ??= new List<string>();
            if (!equippedInvocationIds.Contains(invocation.invocationId))
                equippedInvocationIds.Add(invocation.invocationId);

            return true;
        }

        bool ValidatePriest(out string failureReason)
        {
            failureReason = null;
            if (_stats == null)
            {
                failureReason = "No CharacterStats.";
                return false;
            }

            if (_stats.race != Race.Human || _stats.humanClass != HumanClass.Priest)
            {
                failureReason = "HumanPriestDevotionRuntime requires Human Priest.";
                return false;
            }

            return true;
        }
    }
}
