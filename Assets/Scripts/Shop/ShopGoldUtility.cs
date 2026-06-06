using JRogue.Item;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Shop
{
    public static class ShopGoldUtility
    {
        const string GoldCoinResourcePath = "Item/Currency/GoldCoin";

        static ItemData _goldCoinDefinition;

        public static ItemData GoldCoinDefinition
        {
            get
            {
                if (_goldCoinDefinition == null)
                    _goldCoinDefinition = Resources.Load<ItemData>(GoldCoinResourcePath);
                return _goldCoinDefinition;
            }
        }

        public static int GetPartyGoldTotal()
        {
            PartyCurrencyLedger ledger = PartyCurrencyLedger.Instance;
            return ledger != null ? ledger.GetTotalCount() : 0;
        }

        public static bool TrySpendPartyGold(int amount)
        {
            if (amount <= 0)
                return true;

            PartyCurrencyLedger ledger = PartyCurrencyLedger.Instance;
            if (ledger == null)
                return false;

            if (ledger.GetTotalCount() < amount)
                return false;

            int remaining = amount;
            var snapshot = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ItemData, int>>();
            foreach (var kv in ledger.Snapshot)
                snapshot.Add(kv);

            for (int i = 0; i < snapshot.Count && remaining > 0; i++)
            {
                ItemData currency = snapshot[i].Key;
                int available = snapshot[i].Value;
                if (currency == null || available <= 0)
                    continue;

                int spend = Mathf.Min(available, remaining);
                if (!ledger.TrySpend(currency, spend))
                    return false;

                remaining -= spend;
            }

            return remaining <= 0;
        }

        public static void AddPartyGold(int amount)
        {
            if (amount <= 0)
                return;

            ItemData gold = GoldCoinDefinition;
            if (gold == null)
            {
                Debug.LogWarning("[Shop] Missing GoldCoin ItemData at Resources/Item/Currency/GoldCoin.");
                return;
            }

            PartyCurrencyLedger ledger = PartyCurrencyLedger.Instance;
            ledger?.Add(gold, amount);
        }
    }
}
