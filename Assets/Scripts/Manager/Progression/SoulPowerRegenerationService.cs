using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Data.Enemy;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Manager.Progression
{
    /// <summary>Soul Power regeneration per turn boundary. See Docs/Progression/Soul-Power-Regeneration-Requirements.md.</summary>
    public static class SoulPowerRegenerationService
    {
        public const float PartyBaseRate = 1f;
        public const float EnemyBaseRate = 0.5f;
        public const float RegenScale = 100f;
        public const float NoSpeciesOverride = -1f;

        static readonly Dictionary<GameObject, float> FlatModifiersByActor = new Dictionary<GameObject, float>();

        public static void RegisterFlatModifier(GameObject actor, float delta, object source)
        {
            if (actor == null || source == null)
                return;

            // v0: single bucket per actor; replace by source map when multiple sources ship.
            if (!FlatModifiersByActor.ContainsKey(actor))
                FlatModifiersByActor[actor] = 0f;
            FlatModifiersByActor[actor] += delta;
        }

        public static void UnregisterModifiersFromSource(GameObject actor, object source)
        {
            if (actor == null)
                return;
            FlatModifiersByActor.Remove(actor);
        }

        public static float ComputeEffectiveRate(GameObject actor)
        {
            float baseRate = ResolveBaseRate(actor);
            float mods = FlatModifiersByActor.TryGetValue(actor, out float m) ? m : 0f;
            return Mathf.Max(0f, baseRate + mods);
        }

        public static void TickRegeneration(GameObject actor)
        {
            if (actor == null)
                return;

            if (TurnManager.Instance != null
                && TurnManager.Instance.currentState == GameState.GAME_OVER)
                return;

            if (!TryGetStats(actor, out CharacterStats stats))
                return;

            if (!IsAlive(stats))
                return;

            if (!HumanClassRules.UsesSoulPower(stats.humanClass))
                return;

            int max = stats.MaxSoulPower;
            if (max <= 0)
                return;

            if (stats.currentSoulPower >= max)
            {
                ResetAccumulator(actor);
                return;
            }

            SoulPowerRegenerationState state = GetOrCreateState(actor);
            float effectiveRate = ComputeEffectiveRate(actor);
            state.Accumulator += effectiveRate * RegenScale;

            while (state.Accumulator >= RegenScale && stats.currentSoulPower < max)
            {
                stats.currentSoulPower++;
                state.Accumulator -= RegenScale;
            }

            if (stats.currentSoulPower >= max)
            {
                stats.currentSoulPower = max;
                ResetAccumulator(actor);
            }
        }

        public static void GrantSoulPower(GameObject actor, int amount, bool allowOverMax = false)
        {
            if (actor == null || amount <= 0 || !TryGetStats(actor, out CharacterStats stats))
                return;

            if (!HumanClassRules.UsesSoulPower(stats.humanClass))
                return;

            int max = stats.MaxSoulPower;
            if (max <= 0)
                return;

            if (allowOverMax)
                stats.currentSoulPower += amount;
            else
                stats.currentSoulPower = Mathf.Min(stats.currentSoulPower + amount, max);
        }

        static float ResolveBaseRate(GameObject actor)
        {
            if (IsPartyMember(actor))
                return PartyBaseRate;

            EnemyController enemy = actor.GetComponent<EnemyController>();
            if (enemy != null && enemy.Species != null)
            {
                float speciesRate = enemy.Species.soulPowerRegenRate;
                if (speciesRate >= 0f)
                    return speciesRate;
            }

            return EnemyBaseRate;
        }

        static bool IsPartyMember(GameObject actor)
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return false;

            BaseActor baseActor = actor.GetComponent<BaseActor>();
            if (baseActor == null)
                return false;

            return party.partyMembers.Contains(baseActor);
        }

        static bool IsAlive(CharacterStats stats) => stats != null && stats.currentHP > 0;

        static bool TryGetStats(GameObject actor, out CharacterStats stats)
        {
            stats = actor.GetComponent<CharacterStats>();
            return stats != null;
        }

        static SoulPowerRegenerationState GetOrCreateState(GameObject actor)
        {
            if (!actor.TryGetComponent(out SoulPowerRegenerationState state))
                state = actor.AddComponent<SoulPowerRegenerationState>();
            return state;
        }

        static void ResetAccumulator(GameObject actor)
        {
            if (actor.TryGetComponent(out SoulPowerRegenerationState state))
                state.Accumulator = 0f;
        }
    }
}
