using System;
using UnityEngine;

namespace JRogue.Racial
{
    public enum PriestVowScope
    {
        Personal = 0,
        Party = 1,
    }

    public enum PriestVowRuleKind
    {
        NoBladedWeapons = 0,
        InvokeOnlyAtFullHealth = 1,
        PartyNoEssenceConsumption = 2,
        InventoryMaxItems = 3,
        NoLightSources = 4,
        NoCarryEssenceItems = 5,
    }

    [CreateAssetMenu(fileName = "PriestVow", menuName = "JRogue/Racial/Priest Vow")]
    public sealed class PriestVowDefinition : ScriptableObject
    {
        public string vowId;
        public string displayName;
        [TextArea] public string description;
        public PriestVowScope scope = PriestVowScope.Personal;
        public PriestVowRuleKind ruleKind;
        [Min(0)] public int ruleParam;
        [Min(0)] public int minFloorIndex = 2;
        [Min(0)] public int minDayNightInDungeon = 2;
        [Min(0)] public int pietyRewardOnSuccess = 10;
        public string grantSealId;
    }

    public static class PriestVowCatalogService
    {
        const string ResourceFolder = "Racial/Human/Vows";
        static System.Collections.Generic.Dictionary<string, PriestVowDefinition> _lookup;

        public static bool TryGetVow(string vowId, out PriestVowDefinition vow)
        {
            vow = null;
            if (string.IsNullOrWhiteSpace(vowId))
                return false;

            EnsureLookup();
            return _lookup.TryGetValue(vowId.Trim(), out vow);
        }

        static void EnsureLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new System.Collections.Generic.Dictionary<string, PriestVowDefinition>(
                StringComparer.OrdinalIgnoreCase);
            PriestVowDefinition[] vows = Resources.LoadAll<PriestVowDefinition>(ResourceFolder);
            for (int i = 0; i < vows.Length; i++)
            {
                PriestVowDefinition vow = vows[i];
                if (vow != null && !string.IsNullOrWhiteSpace(vow.vowId))
                    _lookup[vow.vowId.Trim()] = vow;
            }
        }

        public static void ResetCacheForTests() => _lookup = null;
    }
}
