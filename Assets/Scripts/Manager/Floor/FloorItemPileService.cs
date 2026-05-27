using System;
using System.Collections.Generic;
using JRogue.Item;
using JRogue.Item.World;
using UnityEngine;

namespace JRogue.Manager.Floor
{
    [Serializable]
    public sealed class FloorItemEntry
    {
        public string entryId;
        public ItemInstance instance;
    }

    public sealed class FloorItemPileService : MonoBehaviour
    {
        public static FloorItemPileService Instance { get; private set; }

        [SerializeField] FloorItemWorldView worldViewPrefab;

        readonly Dictionary<Vector3Int, List<FloorItemEntry>> _piles = new Dictionary<Vector3Int, List<FloorItemEntry>>();
        readonly Dictionary<string, FloorItemWorldView> _views = new Dictionary<string, FloorItemWorldView>();

        Transform _viewRoot;

        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _viewRoot = new GameObject("FloorItems").transform;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public IReadOnlyList<FloorItemEntry> GetEntries(Vector3Int tile)
        {
            if (_piles.TryGetValue(tile, out List<FloorItemEntry> list))
                return list;
            return Array.Empty<FloorItemEntry>();
        }

        public IReadOnlyList<FloorItemEntry> GetManaStoneAutoPickupEntries(Vector3Int tile) =>
            GetSilentAutoPickupEntries(tile);

        public IReadOnlyList<FloorItemEntry> GetConfirmGatedAutoPickupEntries(Vector3Int tile) =>
            GetEntriesMatching(tile, def => def.RequiresConfirmBeforeAutoPickupOnStep);

        public IReadOnlyList<FloorItemEntry> GetSilentAutoPickupEntries(Vector3Int tile) =>
            GetEntriesMatching(tile, def => def.ParticipatesInSilentAutoPickupOnStep);

        IReadOnlyList<FloorItemEntry> GetEntriesMatching(Vector3Int tile, System.Func<ItemData, bool> predicate)
        {
            if (!_piles.TryGetValue(tile, out List<FloorItemEntry> list))
                return Array.Empty<FloorItemEntry>();

            var matches = new List<FloorItemEntry>();
            for (int i = 0; i < list.Count; i++)
            {
                FloorItemEntry entry = list[i];
                ItemData def = entry?.instance?.Definition;
                if (def != null && predicate(def))
                    matches.Add(entry);
            }

            return matches;
        }

        public void AddEntry(Vector3Int tile, ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
                return;

            if (!_piles.TryGetValue(tile, out List<FloorItemEntry> list))
            {
                list = new List<FloorItemEntry>();
                _piles[tile] = list;
            }

            var entry = new FloorItemEntry
            {
                entryId = Guid.NewGuid().ToString("N"),
                instance = instance
            };
            instance.StorageLocation = ItemStorageLocation.OnGround;
            list.Add(entry);
            SpawnView(tile, entry);
            Changed?.Invoke();
        }

        public bool RemoveEntry(string entryId)
        {
            if (string.IsNullOrEmpty(entryId))
                return false;

            foreach (KeyValuePair<Vector3Int, List<FloorItemEntry>> kv in _piles)
            {
                List<FloorItemEntry> list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].entryId != entryId)
                        continue;

                    list.RemoveAt(i);
                    if (list.Count == 0)
                        _piles.Remove(kv.Key);

                    DestroyView(entryId);
                    Changed?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public int CountManaStonesAt(Vector3Int tile)
        {
            int count = 0;
            if (!_piles.TryGetValue(tile, out List<FloorItemEntry> list))
                return 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i]?.instance?.Definition is ManaStoneItemData)
                    count++;
            }

            return count;
        }

        void SpawnView(Vector3Int tile, FloorItemEntry entry)
        {
            FloorItemWorldView view = worldViewPrefab != null
                ? Instantiate(worldViewPrefab, _viewRoot)
                : FloorItemWorldView.CreateDefault(_viewRoot);

            view.Bind(entry.entryId, entry.instance);
            view.SetGridCell(tile);
            view.transform.position = TileCenterWorld(tile);
            _views[entry.entryId] = view;
            Debug.Log($"[LOOT] Spawned floor view '{view.name}' at {view.transform.position}.");
        }

        void DestroyView(string entryId)
        {
            if (!_views.TryGetValue(entryId, out FloorItemWorldView view))
                return;

            _views.Remove(entryId);
            if (view != null)
                Destroy(view.gameObject);
        }

        public static Vector3 TileCenterWorld(Vector3Int tile) =>
            new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);

        public void ApplyVisibility(VisibilityManager visibility)
        {
            if (visibility == null)
                return;

            foreach (KeyValuePair<string, FloorItemWorldView> kv in _views)
            {
                FloorItemWorldView view = kv.Value;
                if (view == null)
                    continue;
                view.SetVisible(visibility.IsVisible(view.GridCell));
            }
        }
    }
}
