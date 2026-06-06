using System.Collections.Generic;
using JRogue.Item;
using UnityEngine;

namespace JRogue.Shop
{
    public sealed class TownShopStateService : MonoBehaviour
    {
        public static TownShopStateService Instance { get; private set; }

        readonly Dictionary<string, ShopStateSnapshot> _snapshots =
            new Dictionary<string, ShopStateSnapshot>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static TownShopStateService EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject(nameof(TownShopStateService));
            return go.AddComponent<TownShopStateService>();
        }

        public static void EnsureRunService()
        {
            EnsureInstance();
        }

        public void ClearAll() => _snapshots.Clear();

        public ShopStateSnapshot GetSnapshot(string shopNpcId)
        {
            if (string.IsNullOrWhiteSpace(shopNpcId))
                return null;

            _snapshots.TryGetValue(shopNpcId.Trim(), out ShopStateSnapshot snapshot);
            return snapshot?.Clone();
        }

        public ShopStateSnapshot GetOrCreateSnapshot(ShopNpcDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.shopNpcId))
                return null;

            string id = definition.shopNpcId.Trim();
            if (!_snapshots.TryGetValue(id, out ShopStateSnapshot snapshot))
            {
                snapshot = CreateInitialSnapshot(definition);
                _snapshots[id] = snapshot;
            }

            return snapshot.Clone();
        }

        public void SaveSnapshot(ShopStateSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.shopNpcId))
                return;

            _snapshots[snapshot.shopNpcId.Trim()] = snapshot.Clone();
        }

        static ShopStateSnapshot CreateInitialSnapshot(ShopNpcDefinition definition)
        {
            var snapshot = new ShopStateSnapshot
            {
                shopNpcId = definition.shopNpcId.Trim(),
                goldOnHand = definition.initialGold,
            };

            if (definition.initialStock == null)
                return snapshot;

            for (int i = 0; i < definition.initialStock.Length; i++)
            {
                ShopStockEntry entry = definition.initialStock[i];
                if (entry?.item == null || entry.quantity <= 0)
                    continue;

                AddStock(snapshot.stock, entry.item, entry.quantity);
            }

            return snapshot;
        }

        public static void AddStock(List<ShopStockSnapshot> stock, ItemData item, int quantity)
        {
            if (stock == null || item == null || quantity <= 0)
                return;

            for (int i = 0; i < stock.Count; i++)
            {
                if (stock[i].item != item)
                    continue;

                stock[i].quantity += quantity;
                return;
            }

            stock.Add(new ShopStockSnapshot { item = item, quantity = quantity });
        }

        public static bool TryRemoveStock(List<ShopStockSnapshot> stock, ItemData item, int quantity)
        {
            if (stock == null || item == null || quantity <= 0)
                return false;

            for (int i = 0; i < stock.Count; i++)
            {
                if (stock[i].item != item)
                    continue;

                if (stock[i].quantity < quantity)
                    return false;

                stock[i].quantity -= quantity;
                if (stock[i].quantity <= 0)
                    stock.RemoveAt(i);
                return true;
            }

            return false;
        }

        public static int GetStockQuantity(IReadOnlyList<ShopStockSnapshot> stock, ItemData item)
        {
            if (stock == null || item == null)
                return 0;

            for (int i = 0; i < stock.Count; i++)
            {
                if (stock[i].item == item)
                    return stock[i].quantity;
            }

            return 0;
        }
    }
}
