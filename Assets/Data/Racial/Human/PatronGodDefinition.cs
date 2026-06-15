using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    public enum DivineConductKind
    {
        PietyGain = 0,
        PietyLoss = 1,
        Taboo = 2,
    }

    [Serializable]
    public sealed class DivineConductRuleData
    {
        public string conductId;
        public DivineConductKind kind = DivineConductKind.PietyGain;
        public string triggerId;
        [Min(0)] public int pietyDelta = 1;
        [Min(0)] public int cooldownTurns;
        [TextArea] public string description;
    }

    [CreateAssetMenu(fileName = "PatronGod", menuName = "JRogue/Racial/Patron God")]
    public sealed class PatronGodDefinition : ScriptableObject
    {
        public string godId;
        public string displayName;
        [TextArea] public string description;
        public List<DivineConductRuleData> conductRules = new();
        public List<string> invocationIds = new();
        public List<string> vowIds = new();
    }

    public static class PatronGodCatalogService
    {
        const string ResourceFolder = "Racial/Human/Patrons";

        static Dictionary<string, PatronGodDefinition> _lookup;

        public static bool TryGetGod(string godId, out PatronGodDefinition god)
        {
            god = null;
            if (string.IsNullOrWhiteSpace(godId))
                return false;

            EnsureLookup();
            return _lookup.TryGetValue(godId.Trim(), out god);
        }

        static void EnsureLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<string, PatronGodDefinition>(StringComparer.OrdinalIgnoreCase);
            PatronGodDefinition[] gods = Resources.LoadAll<PatronGodDefinition>(ResourceFolder);
            for (int i = 0; i < gods.Length; i++)
            {
                PatronGodDefinition god = gods[i];
                if (god != null && !string.IsNullOrWhiteSpace(god.godId))
                    _lookup[god.godId.Trim()] = god;
            }
        }

        public static void ResetCacheForTests() => _lookup = null;

        public static void RegisterForTests(PatronGodDefinition god)
        {
            if (god == null || string.IsNullOrWhiteSpace(god.godId))
                return;

            EnsureLookup();
            _lookup[god.godId.Trim()] = god;
        }
    }
}
