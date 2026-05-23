using System;
using System.Collections.Generic;
using JRogue.Item;
using UnityEngine;

namespace JRogue.Manager.Party
{
    /// <summary>
    /// Party-wide mana stone pool keyed by tier and the species that dropped each stone.
    /// </summary>
    public sealed class PartyManaStoneLedger : MonoBehaviour
    {
        public static PartyManaStoneLedger Instance { get; private set; }

        public event Action Changed;

        readonly Dictionary<ManaStoneStackKey, int> _amounts = new Dictionary<ManaStoneStackKey, int>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Add(int tier, string sourceSpeciesId, int amount)
        {
            if (amount <= 0)
                return;

            tier = Mathf.Clamp(tier, 1, 9);
            string species = string.IsNullOrEmpty(sourceSpeciesId) ? "unknown" : sourceSpeciesId;
            var key = new ManaStoneStackKey(tier, species);
            if (_amounts.TryGetValue(key, out int cur))
                _amounts[key] = cur + amount;
            else
                _amounts[key] = amount;

            Changed?.Invoke();
        }

        public int GetAmount(int tier, string sourceSpeciesId)
        {
            var key = new ManaStoneStackKey(tier, sourceSpeciesId ?? string.Empty);
            return _amounts.TryGetValue(key, out int v) ? v : 0;
        }

        public bool TrySpend(int tier, string sourceSpeciesId, int amount)
        {
            if (amount <= 0)
                return false;

            var key = new ManaStoneStackKey(tier, sourceSpeciesId ?? string.Empty);
            if (!_amounts.TryGetValue(key, out int cur) || cur < amount)
                return false;

            int next = cur - amount;
            if (next <= 0)
                _amounts.Remove(key);
            else
                _amounts[key] = next;

            Changed?.Invoke();
            return true;
        }

        public IReadOnlyDictionary<ManaStoneStackKey, int> Snapshot => _amounts;

        public int GetTotalCount()
        {
            int sum = 0;
            foreach (int v in _amounts.Values)
                sum += v;
            return sum;
        }

        /// <summary>Non-zero tier totals, sorted high tier first.</summary>
        public void CopyTierTotals(List<(int tier, int count)> dest)
        {
            dest.Clear();
            if (dest.Capacity < 9)
                dest.Capacity = 9;

            var sums = new int[10];
            foreach (KeyValuePair<ManaStoneStackKey, int> kv in _amounts)
            {
                if (kv.Value <= 0)
                    continue;
                int t = Mathf.Clamp(kv.Key.Tier, 1, 9);
                sums[t] += kv.Value;
            }

            for (int tier = 9; tier >= 1; tier--)
            {
                if (sums[tier] > 0)
                    dest.Add((tier, sums[tier]));
            }
        }

        /// <summary>Stacks for one tier, sorted by species id.</summary>
        public void CopyStacksForTier(int tier, List<(string speciesId, int count)> dest)
        {
            dest.Clear();
            foreach (KeyValuePair<ManaStoneStackKey, int> kv in _amounts)
            {
                if (kv.Key.Tier != tier || kv.Value <= 0)
                    continue;
                dest.Add((kv.Key.SourceSpeciesId, kv.Value));
            }

            dest.Sort((a, b) => string.Compare(a.speciesId, b.speciesId, StringComparison.OrdinalIgnoreCase));
        }

        public readonly struct ManaStoneStackKey : System.IEquatable<ManaStoneStackKey>
        {
            public readonly int Tier;
            public readonly string SourceSpeciesId;

            public ManaStoneStackKey(int tier, string sourceSpeciesId)
            {
                Tier = tier;
                SourceSpeciesId = sourceSpeciesId ?? string.Empty;
            }

            public bool Equals(ManaStoneStackKey other) =>
                Tier == other.Tier && SourceSpeciesId == other.SourceSpeciesId;

            public override bool Equals(object obj) => obj is ManaStoneStackKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Tier * 397) ^ (SourceSpeciesId != null ? SourceSpeciesId.GetHashCode() : 0);
                }
            }
        }
    }
}
