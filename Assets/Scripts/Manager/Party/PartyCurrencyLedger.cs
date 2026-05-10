using System.Collections.Generic;
using JRogue.Item;
using UnityEngine;

namespace JRogue.Manager.Party
{
    /// <summary>
    /// Party-wide currency pool (shared wallet). Non-currency items never live here.
    /// </summary>
    public sealed class PartyCurrencyLedger : MonoBehaviour
    {
        public static PartyCurrencyLedger Instance { get; private set; }

        readonly Dictionary<ItemData, int> _amounts = new Dictionary<ItemData, int>();

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

        public void Add(ItemData currencyDefinition, int amount)
        {
            if (currencyDefinition == null || amount <= 0)
                return;

            if (currencyDefinition.category != ItemCategory.Currency)
            {
                Debug.LogWarning(
                    $"[Currency] {currencyDefinition.itemName} is not ItemCategory.Currency — not pooled.");
                return;
            }

            if (_amounts.TryGetValue(currencyDefinition, out int cur))
                _amounts[currencyDefinition] = cur + amount;
            else
                _amounts[currencyDefinition] = amount;
        }

        public int GetAmount(ItemData currencyDefinition)
        {
            if (currencyDefinition == null)
                return 0;
            return _amounts.TryGetValue(currencyDefinition, out int v) ? v : 0;
        }

        public bool TrySpend(ItemData currencyDefinition, int amount)
        {
            if (currencyDefinition == null || amount <= 0)
                return false;

            if (!_amounts.TryGetValue(currencyDefinition, out int cur) || cur < amount)
                return false;

            int next = cur - amount;
            if (next <= 0)
                _amounts.Remove(currencyDefinition);
            else
                _amounts[currencyDefinition] = next;

            return true;
        }

        /// <summary>A read-only view for UI iteration (small dict; currency types are few).</summary>
        public IReadOnlyDictionary<ItemData, int> Snapshot => _amounts;
    }
}
