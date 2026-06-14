using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Run-persistent clan-wide prestige values keyed by clan id.
    /// </summary>
    public sealed class DwarfClanWorldState : MonoBehaviour
    {
        public static DwarfClanWorldState Instance { get; private set; }

        [SerializeField] List<DwarfClanPrestigeEntry> prestigeEntries = new List<DwarfClanPrestigeEntry>();

        readonly Dictionary<string, int> _prestigeByClanId =
            new Dictionary<string, int>(StringComparer.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => EnsureInstance();

        public static DwarfClanWorldState EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            var existing = FindAnyObjectByType<DwarfClanWorldState>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Instance = existing;
                existing.RebuildIndex();
                return existing;
            }

            var go = new GameObject(nameof(DwarfClanWorldState));
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<DwarfClanWorldState>();
            return Instance;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            RebuildIndex();
        }

        void RebuildIndex()
        {
            _prestigeByClanId.Clear();
            if (prestigeEntries == null)
                return;

            for (int i = 0; i < prestigeEntries.Count; i++)
            {
                DwarfClanPrestigeEntry entry = prestigeEntries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.clanId))
                    continue;

                _prestigeByClanId[entry.clanId.Trim()] = Mathf.Max(0, entry.prestige);
            }
        }

        public int GetPrestige(string clanId)
        {
            if (string.IsNullOrWhiteSpace(clanId))
                return 0;

            return _prestigeByClanId.TryGetValue(clanId.Trim(), out int prestige) ? prestige : 0;
        }

        public int EnsurePrestige(string clanId, int startingPrestige)
        {
            if (string.IsNullOrWhiteSpace(clanId))
                return 0;

            string trimmed = clanId.Trim();
            if (_prestigeByClanId.TryGetValue(trimmed, out int existing))
                return existing;

            int value = Mathf.Max(0, startingPrestige);
            _prestigeByClanId[trimmed] = value;
            UpsertEntry(trimmed, value);
            return value;
        }

        void UpsertEntry(string clanId, int prestige)
        {
            prestigeEntries ??= new List<DwarfClanPrestigeEntry>();
            for (int i = 0; i < prestigeEntries.Count; i++)
            {
                DwarfClanPrestigeEntry entry = prestigeEntries[i];
                if (entry == null || !string.Equals(entry.clanId, clanId, StringComparison.Ordinal))
                    continue;

                entry.prestige = prestige;
                return;
            }

            prestigeEntries.Add(new DwarfClanPrestigeEntry { clanId = clanId, prestige = prestige });
        }
    }

    [Serializable]
    public sealed class DwarfClanPrestigeEntry
    {
        public string clanId;
        public int prestige;
    }
}
