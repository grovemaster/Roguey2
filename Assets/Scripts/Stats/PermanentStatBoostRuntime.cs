using System;
using System.Collections.Generic;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Stats
{
    /// <summary>
    /// Ledger + re-apply for permanent consumable stat/resistance boosts (Pill of …).
    /// </summary>
    public sealed class PermanentStatBoostRuntime : MonoBehaviour
    {
        public const string LogPrefix = "[PermanentStat]";

        [Serializable]
        public struct AttributeTotal
        {
            public StatType attribute;
            public int amount;
        }

        [Serializable]
        public struct ResistanceTotal
        {
            public DamageType resistance;
            public int amount;
        }

        [SerializeField] List<AttributeTotal> attributeTotals = new List<AttributeTotal>();
        [SerializeField] List<ResistanceTotal> resistanceTotals = new List<ResistanceTotal>();

        readonly Dictionary<StatType, object> _attributeSources = new Dictionary<StatType, object>();
        readonly Dictionary<DamageType, object> _resistanceSources = new Dictionary<DamageType, object>();

        CharacterStats _stats;

        void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            ReapplyAll();
        }

        void OnEnable() => ReapplyAll();

        public static PermanentStatBoostRuntime Ensure(GameObject actor)
        {
            if (actor == null)
                return null;

            PermanentStatBoostRuntime runtime = actor.GetComponent<PermanentStatBoostRuntime>();
            if (runtime == null)
                runtime = actor.AddComponent<PermanentStatBoostRuntime>();
            return runtime;
        }

        public bool TryApplyAttribute(StatType attribute, int amount, out string targetLabel)
        {
            targetLabel = attribute.ToString();
            if (amount == 0)
                return false;

            CharacterStats stats = ResolveStats();
            if (stats == null || stats.GetStatByType(attribute) == null)
                return false;

            int index = IndexOfAttribute(attribute);
            if (index < 0)
            {
                attributeTotals.Add(new AttributeTotal { attribute = attribute, amount = amount });
            }
            else
            {
                AttributeTotal entry = attributeTotals[index];
                entry.amount += amount;
                attributeTotals[index] = entry;
            }

            ReapplyAttribute(attribute);
            return true;
        }

        public bool TryApplyResistance(DamageType resistance, int amount, out string targetLabel)
        {
            targetLabel = $"{resistance} resistance";
            if (amount == 0)
                return false;

            CharacterStats stats = ResolveStats();
            if (stats == null)
                return false;

            int index = IndexOfResistance(resistance);
            if (index < 0)
            {
                resistanceTotals.Add(new ResistanceTotal { resistance = resistance, amount = amount });
            }
            else
            {
                ResistanceTotal entry = resistanceTotals[index];
                entry.amount += amount;
                resistanceTotals[index] = entry;
            }

            ReapplyResistance(resistance);
            return true;
        }

        public int GetAttributeTotal(StatType attribute)
        {
            int index = IndexOfAttribute(attribute);
            return index < 0 ? 0 : attributeTotals[index].amount;
        }

        public int GetResistanceTotal(DamageType resistance)
        {
            int index = IndexOfResistance(resistance);
            return index < 0 ? 0 : resistanceTotals[index].amount;
        }

        public bool HasAnyBoosts()
        {
            for (int i = 0; i < attributeTotals.Count; i++)
            {
                if (attributeTotals[i].amount != 0)
                    return true;
            }

            for (int i = 0; i < resistanceTotals.Count; i++)
            {
                if (resistanceTotals[i].amount != 0)
                    return true;
            }

            return false;
        }

        /// <summary>Player-facing lines for the character sheet (no header).</summary>
        public void CopyDisplayLines(List<string> lines)
        {
            if (lines == null)
                return;

            lines.Clear();
            for (int i = 0; i < attributeTotals.Count; i++)
            {
                AttributeTotal entry = attributeTotals[i];
                if (entry.amount == 0)
                    continue;
                lines.Add($"{entry.attribute} {FormatSigned(entry.amount)}");
            }

            for (int i = 0; i < resistanceTotals.Count; i++)
            {
                ResistanceTotal entry = resistanceTotals[i];
                if (entry.amount == 0)
                    continue;
                lines.Add($"{entry.resistance} resistance {FormatSigned(entry.amount)}");
            }
        }

        public void ReapplyAll()
        {
            CharacterStats stats = ResolveStats();
            if (stats == null)
                return;

            for (int i = 0; i < attributeTotals.Count; i++)
                ReapplyAttribute(attributeTotals[i].attribute);

            for (int i = 0; i < resistanceTotals.Count; i++)
                ReapplyResistance(resistanceTotals[i].resistance);
        }

        void ReapplyAttribute(StatType attribute)
        {
            CharacterStats stats = ResolveStats();
            Stat stat = stats?.GetStatByType(attribute);
            if (stat == null)
                return;

            object source = GetAttributeSource(attribute);
            stat.RemoveModifiersFromSource(source);
            int total = GetAttributeTotal(attribute);
            if (total != 0)
                stat.AddModifier(total, source, ModifierSourceLayer.PermanentConsumable);
        }

        void ReapplyResistance(DamageType resistance)
        {
            CharacterStats stats = ResolveStats();
            if (stats == null)
                return;

            object source = GetResistanceSource(resistance);
            stats.RemoveResistanceModifier(resistance, source);
            int total = GetResistanceTotal(resistance);
            if (total != 0)
            {
                stats.AddResistanceModifier(
                    resistance,
                    total,
                    source,
                    ModifierSourceLayer.PermanentConsumable);
            }
        }

        CharacterStats ResolveStats()
        {
            if (_stats == null)
                _stats = GetComponent<CharacterStats>();
            return _stats;
        }

        int IndexOfAttribute(StatType attribute)
        {
            for (int i = 0; i < attributeTotals.Count; i++)
            {
                if (attributeTotals[i].attribute == attribute)
                    return i;
            }

            return -1;
        }

        int IndexOfResistance(DamageType resistance)
        {
            for (int i = 0; i < resistanceTotals.Count; i++)
            {
                if (resistanceTotals[i].resistance == resistance)
                    return i;
            }

            return -1;
        }

        object GetAttributeSource(StatType attribute)
        {
            if (!_attributeSources.TryGetValue(attribute, out object source))
            {
                source = new PermanentBoostSource($"attr:{attribute}");
                _attributeSources[attribute] = source;
            }

            return source;
        }

        object GetResistanceSource(DamageType resistance)
        {
            if (!_resistanceSources.TryGetValue(resistance, out object source))
            {
                source = new PermanentBoostSource($"res:{resistance}");
                _resistanceSources[resistance] = source;
            }

            return source;
        }

        static string FormatSigned(int amount) => amount > 0 ? $"+{amount}" : amount.ToString();

        sealed class PermanentBoostSource
        {
            readonly string _key;
            public PermanentBoostSource(string key) => _key = key;
            public override string ToString() => _key;
        }
    }
}
