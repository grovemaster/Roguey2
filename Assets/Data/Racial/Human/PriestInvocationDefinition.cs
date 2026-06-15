using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "PriestInvocation", menuName = "JRogue/Racial/Priest Invocation")]
    public sealed class PriestInvocationDefinition : ScriptableObject
    {
        public string invocationId;
        public string displayName;
        [TextArea] public string description;
        public AbilityAction ability;

        [Min(0)] public int requiredPiety;
        [Min(1)] public int requiredCharacterLevel = 1;
        public string requiredSealId;

        [Min(0)] public int divinePowerCost;
        [Min(0)] public int pietyInvokeCost;
        public List<ProficiencyKind> proficiencyTags = new();
    }

    [CreateAssetMenu(fileName = "PriestInvocationCatalog", menuName = "JRogue/Racial/Priest Invocation Catalog")]
    public sealed class PriestInvocationCatalog : ScriptableObject
    {
        public List<PriestInvocationDefinition> invocations = new();
    }

    public static class PriestInvocationCatalogService
    {
        const string DefaultResourcePath = "Racial/Human/PriestInvocationCatalog";
        const string InvocationResourceFolder = "Racial/Human/Invocations";

        static PriestInvocationCatalog _cached;
        static Dictionary<string, PriestInvocationDefinition> _lookup;

        public static bool TryGetInvocation(string invocationId, out PriestInvocationDefinition invocation)
        {
            invocation = null;
            if (string.IsNullOrWhiteSpace(invocationId))
                return false;

            EnsureLookup();
            return _lookup.TryGetValue(invocationId.Trim(), out invocation);
        }

        public static IReadOnlyList<PriestInvocationDefinition> GetAllInvocations()
        {
            EnsureLookup();
            var list = new List<PriestInvocationDefinition>(_lookup.Values);
            list.Sort((a, b) => string.Compare(a?.invocationId, b?.invocationId, StringComparison.Ordinal));
            return list;
        }

        static void EnsureLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<string, PriestInvocationDefinition>(StringComparer.OrdinalIgnoreCase);

            if (_cached == null)
                _cached = Resources.Load<PriestInvocationCatalog>(DefaultResourcePath);

            if (_cached?.invocations != null)
            {
                for (int i = 0; i < _cached.invocations.Count; i++)
                    Register(_cached.invocations[i]);
            }

            if (_lookup.Count == 0)
            {
                PriestInvocationDefinition[] loaded =
                    Resources.LoadAll<PriestInvocationDefinition>(InvocationResourceFolder);
                for (int i = 0; i < loaded.Length; i++)
                    Register(loaded[i]);
            }
        }

        static void Register(PriestInvocationDefinition invocation)
        {
            if (invocation == null || string.IsNullOrWhiteSpace(invocation.invocationId))
                return;

            _lookup[invocation.invocationId.Trim()] = invocation;
        }

        public static void ResetCacheForTests()
        {
            _cached = null;
            _lookup = null;
        }

        public static void RegisterForTests(PriestInvocationDefinition invocation)
        {
            if (invocation == null || string.IsNullOrWhiteSpace(invocation.invocationId))
                return;

            EnsureLookup();
            _lookup[invocation.invocationId.Trim()] = invocation;
        }
    }
}
