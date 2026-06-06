using System;
using System.Collections.Generic;
using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Grid;
using UnityEngine;

namespace JRogue.Manager.Floor
{
    [Serializable]
    public sealed class FloorItemEntry
    {
        public string entryId;
        public ItemInstance instance;
        /// <summary>0 = never despawn.</summary>
        public int phasesRemaining;
    }

    public sealed class FloorItemPileService : MonoBehaviour
    {
        public static FloorItemPileService Instance { get; private set; }

        [SerializeField] FloorItemWorldView worldViewPrefab;

        readonly Dictionary<Vector3Int, List<FloorItemEntry>> _piles = new Dictionary<Vector3Int, List<FloorItemEntry>>();
        readonly Dictionary<string, FloorItemWorldView> _views = new Dictionary<string, FloorItemWorldView>();

        Transform _viewRoot;
        Transform _boundParent;

        public event Action Changed;

        public void BindViewRoot(Transform parent)
        {
            _boundParent = parent;
            EnsureViewRoot();
            if (_viewRoot != null && parent != null)
                _viewRoot.SetParent(parent, false);
        }

        void EnsureViewRoot()
        {
            if (_viewRoot != null)
                return;

            _viewRoot = new GameObject("FloorItems").transform;
            if (_boundParent != null)
                _viewRoot.SetParent(_boundParent, false);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureViewRoot();
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

            int lifetime = instance.Definition != null
                ? Mathf.Max(0, instance.Definition.floorLifetimePlayerPhases)
                : 0;

            var entry = new FloorItemEntry
            {
                entryId = Guid.NewGuid().ToString("N"),
                instance = instance,
                phasesRemaining = lifetime,
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
            EnsureViewRoot();
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
            GridCellWorld.GetCellCenter(tile);

        public void ClearAllPiles()
        {
            foreach (KeyValuePair<string, FloorItemWorldView> kv in _views)
            {
                if (kv.Value != null)
                    Destroy(kv.Value.gameObject);
            }

            _views.Clear();
            _piles.Clear();
            Changed?.Invoke();
        }

        public void CaptureSnapshot(List<JRogue.World.Generation.FloorItemSnapshotEntry> dest)
        {
            if (dest == null)
                return;

            dest.Clear();
            foreach (KeyValuePair<Vector3Int, List<FloorItemEntry>> kv in _piles)
            {
                List<FloorItemEntry> list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    FloorItemEntry entry = list[i];
                    if (entry?.instance?.Definition == null)
                        continue;

                    dest.Add(new JRogue.World.Generation.FloorItemSnapshotEntry
                    {
                        tile = kv.Key,
                        entryId = entry.entryId,
                        definition = entry.instance.Definition,
                        quantity = entry.instance.Quantity,
                        phasesRemaining = entry.phasesRemaining,
                    });
                }
            }
        }

        public void RestoreSnapshot(IReadOnlyList<JRogue.World.Generation.FloorItemSnapshotEntry> src)
        {
            ClearAllPiles();
            if (src == null)
                return;

            for (int i = 0; i < src.Count; i++)
                RestoreSnapshotEntry(src[i]);
        }

        void RestoreSnapshotEntry(JRogue.World.Generation.FloorItemSnapshotEntry snap)
        {
            if (snap.definition == null || string.IsNullOrEmpty(snap.entryId))
                return;

            var instance = new ItemInstance(snap.entryId, snap.definition, snap.quantity);
            if (!_piles.TryGetValue(snap.tile, out List<FloorItemEntry> list))
            {
                list = new List<FloorItemEntry>();
                _piles[snap.tile] = list;
            }

            var entry = new FloorItemEntry
            {
                entryId = snap.entryId,
                instance = instance,
                phasesRemaining = snap.phasesRemaining,
            };
            list.Add(entry);
            SpawnView(snap.tile, entry);
            Changed?.Invoke();
        }

        public void TickFloorItemLifetimes()
        {
            if (_piles.Count == 0)
                return;

            var expiredIds = new List<string>();
            foreach (KeyValuePair<Vector3Int, List<FloorItemEntry>> kv in _piles)
            {
                List<FloorItemEntry> list = kv.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    FloorItemEntry entry = list[i];
                    if (entry.phasesRemaining <= 0)
                        continue;

                    entry.phasesRemaining--;
                    if (entry.phasesRemaining <= 0)
                        expiredIds.Add(entry.entryId);
                }
            }

            for (int i = 0; i < expiredIds.Count; i++)
                RemoveEntry(expiredIds[i]);
        }

        public void ApplyVisibility(VisibilityManager visibility)
        {
            if (visibility == null)
                return;

            foreach (KeyValuePair<string, FloorItemWorldView> kv in _views)
            {
                FloorItemWorldView view = kv.Value;
                if (view == null)
                    continue;
                view.SetVisible(visibility.IsLitVisible(view.GridCell));
            }
        }
    }
}
