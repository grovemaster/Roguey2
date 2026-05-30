using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Manager.Progression
{
    /// <summary>HP snapshots and per-session heal budgets for rest. See Docs/Progression/Rest-Requirements.md.</summary>
    public sealed class PartyRestState : MonoBehaviour
    {
        public const float HealBudgetFraction = 0.2f;

        readonly Dictionary<EntityId, int> _hpAtLastSuccessfulRestStart = new Dictionary<EntityId, int>();
        readonly Dictionary<EntityId, int> _sessionHealBudgetRemaining = new Dictionary<EntityId, int>();

        public bool HasActiveSessionBudgets => _sessionHealBudgetRemaining.Count > 0;

        public int GetSessionHealRemaining(EntityId entityId) =>
            _sessionHealBudgetRemaining.TryGetValue(entityId, out int v) ? v : 0;

        public static int ComputeHealBudgetForMember(CharacterStats stats, EntityId entityId, PartyRestState state)
        {
            if (stats == null || stats.currentHP <= 0)
                return 0;

            int maxHp = stats.MaxHP;
            if (maxHp <= 0)
                return 0;

            if (state == null || !state._hpAtLastSuccessfulRestStart.ContainsKey(entityId))
                return Mathf.FloorToInt(maxHp * HealBudgetFraction);

            int hpAtLastStart = state._hpAtLastSuccessfulRestStart[entityId];
            int hpLost = Mathf.Max(0, hpAtLastStart - stats.currentHP);
            return Mathf.FloorToInt(hpLost * HealBudgetFraction);
        }

        public void CommitSuccessfulRestStart(IReadOnlyList<BaseActor> members)
        {
            _sessionHealBudgetRemaining.Clear();

            if (members == null)
                return;

            for (int i = 0; i < members.Count; i++)
            {
                BaseActor member = members[i];
                if (member == null || member.stats == null || member.stats.currentHP <= 0)
                    continue;

                EntityId id = member.gameObject.GetEntityId();
                int budget = ComputeHealBudgetForMember(member.stats, id, this);
                _sessionHealBudgetRemaining[id] = budget;
                _hpAtLastSuccessfulRestStart[id] = member.stats.currentHP;
            }
        }

        public void ClearSessionBudgets() => _sessionHealBudgetRemaining.Clear();

        public int TickRestHeal(BaseActor member)
        {
            if (member == null || member.stats == null)
                return 0;

            CharacterStats stats = member.stats;
            if (stats.currentHP <= 0)
                return 0;

            EntityId id = member.gameObject.GetEntityId();
            if (!_sessionHealBudgetRemaining.TryGetValue(id, out int remaining) || remaining <= 0)
                return 0;

            int hpRoom = stats.MaxHP - stats.currentHP;
            if (hpRoom <= 0)
                return 0;

            int gain = HealthRegenerationService.ComputeEffectiveHpRegenPerStep(member.gameObject);
            int actual = Mathf.Min(gain, remaining, hpRoom);
            if (actual <= 0)
                return 0;

            stats.currentHP += actual;
            _sessionHealBudgetRemaining[id] = remaining - actual;
            return actual;
        }

#if UNITY_EDITOR
        public void ResetForTests()
        {
            _hpAtLastSuccessfulRestStart.Clear();
            _sessionHealBudgetRemaining.Clear();
        }

        public void SetHpSnapshotForTests(EntityId entityId, int hp) =>
            _hpAtLastSuccessfulRestStart[entityId] = hp;
#endif
    }
}
