using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item;
using UnityEngine;

namespace JRogue.Manager.Inventory
{
    /// <summary>
    /// Extensible post-use ability cooldown tracking keyed by item instance + ability asset.
    /// Helmet of Light v1 uses <see cref="ItemInstance"/> helmet fields; this service covers future abilities.
    /// </summary>
    public static class AbilityCooldownService
    {
        public const string LogPrefix = "[Ability:Cooldown]";

        readonly struct CooldownKey
        {
            public readonly string InstanceId;
            public readonly string AbilityId;

            public CooldownKey(string instanceId, string abilityId)
            {
                InstanceId = instanceId;
                AbilityId = abilityId;
            }
        }

        static readonly Dictionary<CooldownKey, int> RemainingCooldownTurns = new Dictionary<CooldownKey, int>();

        public static int GetRemainingCooldown(ItemInstance instance, AbilityAction ability)
        {
            if (instance == null || ability == null || ability.cooldownTurns <= 0)
                return 0;

            var key = new CooldownKey(instance.Id, ability.name);
            return RemainingCooldownTurns.TryGetValue(key, out int turns) ? turns : 0;
        }

        public static bool IsOnCooldown(ItemInstance instance, AbilityAction ability) =>
            GetRemainingCooldown(instance, ability) > 0;

        public static void StartCooldown(ItemInstance instance, AbilityAction ability)
        {
            if (instance == null || ability == null || ability.cooldownTurns <= 0)
                return;

            var key = new CooldownKey(instance.Id, ability.name);
            RemainingCooldownTurns[key] = ability.cooldownTurns;
            Debug.Log($"{LogPrefix} Started {ability.cooldownTurns} turn(s) for {ability.abilityName}.");
        }

        public static void TickInstanceCooldowns(ItemInstance instance)
        {
            if (instance == null || RemainingCooldownTurns.Count == 0)
                return;

            var keysToTick = new List<CooldownKey>();
            foreach (KeyValuePair<CooldownKey, int> pair in RemainingCooldownTurns)
            {
                if (pair.Key.InstanceId == instance.Id)
                    keysToTick.Add(pair.Key);
            }

            for (int i = 0; i < keysToTick.Count; i++)
            {
                CooldownKey key = keysToTick[i];
                int next = RemainingCooldownTurns[key] - 1;
                if (next <= 0)
                    RemainingCooldownTurns.Remove(key);
                else
                    RemainingCooldownTurns[key] = next;
            }
        }

        /// <summary>Clears service state (tests).</summary>
        public static void ResetForTests() => RemainingCooldownTurns.Clear();
    }
}
