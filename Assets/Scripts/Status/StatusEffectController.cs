using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Status
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class StatusEffectController : MonoBehaviour
    {
        readonly List<StatusEffectInstance> _activeStatuses = new List<StatusEffectInstance>();

        CharacterStats _stats;
        HealthComponent _health;
        BaseActor _actor;

        public CharacterStats Stats => _stats;
        public string DisplayName => _actor != null ? _actor.DisplayName : gameObject.name;

        void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _health = GetComponent<HealthComponent>();
            _actor = GetComponent<BaseActor>();
        }

        public bool HasStatus(StatusEffectId id) => TryGetStatus(id, out _);

        /// <summary>True if any active status has <see cref="StatusPolarity.Negative"/>.</summary>
        public bool HasNegativeStatus()
        {
            for (int i = 0; i < _activeStatuses.Count; i++)
            {
                StatusEffectDefinition def = _activeStatuses[i]?.definition;
                if (def != null && def.IsNegative)
                    return true;
            }

            return false;
        }

        /// <summary>True if any active status has <see cref="StatusPolarity.Positive"/>.</summary>
        public bool HasPositiveStatus()
        {
            for (int i = 0; i < _activeStatuses.Count; i++)
            {
                StatusEffectDefinition def = _activeStatuses[i]?.definition;
                if (def != null && def.IsPositive)
                    return true;
            }

            return false;
        }

        public bool TryApply(StatusEffectDefinition definition, GameObject source = null) =>
            StatusEffectService.TryApply(this, definition, source);

        public void TickStatuses()
        {
            for (int i = _activeStatuses.Count - 1; i >= 0; i--)
            {
                StatusEffectInstance status = _activeStatuses[i];
                if (status?.definition == null || status.turnsRemaining <= 0)
                {
                    _activeStatuses.RemoveAt(i);
                    continue;
                }

                switch (status.definition.statusId)
                {
                    case StatusEffectId.Poisoned:
                        TickPoisoned(status);
                        break;
                    default:
                        status.turnsRemaining--;
                        break;
                }

                if (status.turnsRemaining <= 0)
                {
                    Debug.Log($"[Status] {status.definition.displayName} expired on {DisplayName}.");
                    _activeStatuses.RemoveAt(i);
                }
            }
        }

        public void ClearAll() => _activeStatuses.Clear();

        public int GetTurnsRemaining(StatusEffectId id) =>
            TryGetStatus(id, out StatusEffectInstance status) ? status.turnsRemaining : 0;

        internal bool TryGetStatus(StatusEffectId id, out StatusEffectInstance instance)
        {
            for (int i = 0; i < _activeStatuses.Count; i++)
            {
                if (_activeStatuses[i] != null && _activeStatuses[i].StatusId == id)
                {
                    instance = _activeStatuses[i];
                    return true;
                }
            }

            instance = null;
            return false;
        }

        internal void AddStatus(StatusEffectInstance instance)
        {
            if (instance != null)
                _activeStatuses.Add(instance);
        }

        void TickPoisoned(StatusEffectInstance status)
        {
            PoisonStatusEffectDefinition poison = status.definition as PoisonStatusEffectDefinition;
            if (poison == null || _health == null || _stats == null)
            {
                status.turnsRemaining--;
                return;
            }

            _health.TakeDamage(poison.damagePerTick, poison.damageType, ArmorInteraction.None, status.source);
            Debug.Log($"[Status] {DisplayName} takes {poison.damagePerTick} {poison.damageType} from {poison.displayName}.");

            // Player party members only: CON check after poison damage.
            if (IsPlayerPartyMember())
            {
                int roll = StatusEffectService.RollD20();
                int total = roll + _stats.Constitution.GetValue();
                if (total >= poison.escapeDifficulty)
                {
                    Debug.Log($"[Status] {DisplayName} shook off {poison.displayName}.");
                    status.turnsRemaining = 0;
                    return;
                }
            }

            status.turnsRemaining--;
        }

        bool IsPlayerPartyMember()
        {
            PartyManager party = PartyManager.Instance;
            return _actor != null && party != null && party.partyMembers.Contains(_actor);
        }
    }
}
