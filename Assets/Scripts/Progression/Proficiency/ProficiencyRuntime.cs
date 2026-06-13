using System;
using System.Collections.Generic;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Progression.Proficiency
{
    [Serializable]
    public struct ProficiencyEntry
    {
        public ProficiencyKind kind;
        public int level;
        public int pxp;
    }

    /// <summary>Per-actor proficiency levels and XP progress.</summary>
    [DisallowMultipleComponent]
    public sealed class ProficiencyRuntime : MonoBehaviour
    {
        [SerializeField] List<ProficiencyEntry> entries = new();

        readonly Dictionary<ProficiencyKind, int> _indexByKind = new();

        void Awake()
        {
            RebuildIndex();
            ProficiencyLegacyMigration.MigrateFromWeaponProficiencies(GetComponent<CharacterStats>(), this);
        }

        public int GetLevel(ProficiencyKind kind)
        {
            if (kind == ProficiencyKind.None || !_indexByKind.TryGetValue(kind, out int index))
                return 0;

            return entries[index].level;
        }

        public int GetPxp(ProficiencyKind kind)
        {
            if (kind == ProficiencyKind.None || !_indexByKind.TryGetValue(kind, out int index))
                return 0;

            return entries[index].pxp;
        }

        public void SetLevelForTests(ProficiencyKind kind, int level, int pxp = 0)
        {
            if (kind == ProficiencyKind.None)
                return;

            ProficiencyEntry entry = GetOrCreateEntry(kind);
            entry.level = Mathf.Clamp(level, 0, ProficiencyRules.MaxLevel);
            entry.pxp = Mathf.Max(0, pxp);
            WriteEntry(kind, entry);
        }

        public void AddPxp(CharacterStats stats, ProficiencyKind kind, int pxpAmount)
        {
            if (stats == null || kind == ProficiencyKind.None || pxpAmount <= 0)
                return;

            if (!ProficiencyEligibility.CanTrain(stats, kind))
                return;

            ProficiencyEntry entry = GetOrCreateEntry(kind);
            if (entry.level >= ProficiencyRules.MaxLevel)
                return;

            int trainingCap = ProficiencyRules.GetTrainingCap(stats.level);
            entry.pxp += pxpAmount;

            while (entry.level < ProficiencyRules.MaxLevel && entry.level < trainingCap)
            {
                int aptitude = ProficiencyAptitudeService.GetAptitude(stats, kind);
                int needed = ProficiencyRules.GetXpToNextLevel(entry.level, aptitude);
                if (entry.pxp < needed)
                    break;

                entry.pxp -= needed;
                entry.level++;
            }

            WriteEntry(kind, entry);
        }

        ProficiencyEntry GetOrCreateEntry(ProficiencyKind kind)
        {
            if (_indexByKind.TryGetValue(kind, out int index))
                return entries[index];

            var entry = new ProficiencyEntry { kind = kind, level = 0, pxp = 0 };
            _indexByKind[kind] = entries.Count;
            entries.Add(entry);
            return entry;
        }

        void WriteEntry(ProficiencyKind kind, ProficiencyEntry entry)
        {
            if (!_indexByKind.TryGetValue(kind, out int index))
            {
                _indexByKind[kind] = entries.Count;
                entries.Add(entry);
                return;
            }

            entries[index] = entry;
        }

        void RebuildIndex()
        {
            _indexByKind.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].kind == ProficiencyKind.None)
                    continue;

                _indexByKind[entries[i].kind] = i;
            }
        }

        public static ProficiencyRuntime EnsureOn(GameObject target)
        {
            if (target == null)
                return null;

            ProficiencyRuntime runtime = target.GetComponent<ProficiencyRuntime>();
            if (runtime != null)
                return runtime;

            return target.AddComponent<ProficiencyRuntime>();
        }
    }
}
