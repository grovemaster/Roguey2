using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Per-Knight-skill rank pxp and mastery progress. Tree rank lives on
    /// <see cref="HumanClassSkillTreeRuntime"/>; this component stores practice only.
    /// </summary>
    public sealed class KnightSkillMasteryRuntime : MonoBehaviour
    {
        [SerializeField] List<KnightSkillMasteryEntry> entries = new List<KnightSkillMasteryEntry>();

        readonly Dictionary<string, KnightSkillMasteryEntry> _bySkillId =
            new Dictionary<string, KnightSkillMasteryEntry>(StringComparer.Ordinal);

        void OnEnable() => RebuildIndex();

        public static KnightSkillMasteryRuntime EnsureOn(GameObject actor)
        {
            if (actor == null)
                return null;

            KnightSkillMasteryRuntime runtime = actor.GetComponent<KnightSkillMasteryRuntime>();
            if (runtime == null)
                runtime = actor.AddComponent<KnightSkillMasteryRuntime>();

            runtime.RebuildIndex();
            return runtime;
        }

        public void RebuildIndex()
        {
            _bySkillId.Clear();
            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                KnightSkillMasteryEntry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.skillId))
                    continue;

                _bySkillId[entry.skillId] = entry;
            }
        }

        public bool TryGetEntry(string skillId, out KnightSkillMasteryEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(skillId))
                return false;

            return _bySkillId.TryGetValue(skillId, out entry);
        }

        public KnightSkillMasteryEntry GetOrCreateEntry(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return null;

            if (_bySkillId.TryGetValue(skillId, out KnightSkillMasteryEntry existing))
                return existing;

            var created = new KnightSkillMasteryEntry { skillId = skillId };
            entries ??= new List<KnightSkillMasteryEntry>();
            entries.Add(created);
            _bySkillId[skillId] = created;
            return created;
        }

        public int GetRankPxp(string skillId) =>
            TryGetEntry(skillId, out KnightSkillMasteryEntry entry) ? entry.rankPxp : 0;

        public int GetMasteryLevel(string skillId) =>
            TryGetEntry(skillId, out KnightSkillMasteryEntry entry) ? entry.masteryLevel : 0;

        public int GetMasteryPxp(string skillId) =>
            TryGetEntry(skillId, out KnightSkillMasteryEntry entry) ? entry.masteryPxp : 0;

        public void SetRankPxp(string skillId, int value)
        {
            KnightSkillMasteryEntry entry = GetOrCreateEntry(skillId);
            if (entry != null)
                entry.rankPxp = Mathf.Max(0, value);
        }

        public void SetMastery(string skillId, int level, int pxp)
        {
            KnightSkillMasteryEntry entry = GetOrCreateEntry(skillId);
            if (entry == null)
                return;

            entry.masteryLevel = Mathf.Max(0, level);
            entry.masteryPxp = Mathf.Max(0, pxp);
        }

        public IReadOnlyList<KnightSkillMasteryEntry> Entries => entries;
    }
}
