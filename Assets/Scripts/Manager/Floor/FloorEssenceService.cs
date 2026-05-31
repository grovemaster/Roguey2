using System;
using System.Collections.Generic;
using JRogue.Item.Essence;
using JRogue.Item.World;
using JRogue.Actors;
using JRogue.Manager.Essence;
using JRogue.Manager.Visibility;
using UnityEngine;

namespace JRogue.Manager.Floor
{
    public sealed class FloorEssenceService : MonoBehaviour
    {
        public static FloorEssenceService Instance { get; private set; }

        [SerializeField] FloorEssenceWorldView worldViewPrefab;

        readonly Dictionary<Vector3Int, FloorEssenceEntry> _byTile =
            new Dictionary<Vector3Int, FloorEssenceEntry>();

        readonly Dictionary<string, FloorEssenceWorldView> _views =
            new Dictionary<string, FloorEssenceWorldView>();

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
            _viewRoot = new GameObject("FloorEssences").transform;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool HasEssenceAt(Vector3Int tile) => _byTile.ContainsKey(tile);

        public bool TryGetAt(Vector3Int tile, out FloorEssenceEntry entry) =>
            _byTile.TryGetValue(tile, out entry);

        public void SpawnEssence(Vector3Int tile, EssenceData data)
        {
            if (data == null)
                return;

            if (_byTile.ContainsKey(tile))
            {
                Debug.LogWarning($"[Essence] Tile {tile} already has a floor essence; replacing.");
                RemoveAtTile(tile);
            }

            int lifetime = Mathf.Max(0, data.floorLifetimePlayerPhases);
            var entry = new FloorEssenceEntry
            {
                entryId = Guid.NewGuid().ToString("N"),
                tile = tile,
                essenceData = data,
                phasesRemaining = lifetime,
            };

            _byTile[tile] = entry;
            SpawnView(entry);
            Changed?.Invoke();
            Debug.Log($"[Essence] Spawned {data.essenceName} (T{data.tier}) at {tile}.");
        }

        public void RemoveAtTile(Vector3Int tile)
        {
            if (!_byTile.TryGetValue(tile, out FloorEssenceEntry entry))
                return;

            _byTile.Remove(tile);
            DestroyView(entry.entryId);
            Changed?.Invoke();
        }

        public bool TryClaimAt(Vector3Int tile, BaseActor mover)
        {
            if (mover == null || !_byTile.TryGetValue(tile, out FloorEssenceEntry entry))
                return false;

            EssenceSlotManager slots = mover.GetComponent<EssenceSlotManager>();
            if (slots == null || !slots.TryAcquireEssence(entry.essenceData))
                return false;

            Debug.Log($"[Essence] {mover.DisplayName} gained {entry.essenceData.essenceName}.");
            RemoveAtTile(tile);
            return true;
        }

        public void TickDespawnAll()
        {
            if (_byTile.Count == 0)
                return;

            var expired = new List<Vector3Int>();
            foreach (KeyValuePair<Vector3Int, FloorEssenceEntry> kv in _byTile)
            {
                FloorEssenceEntry entry = kv.Value;
                if (entry.phasesRemaining <= 0)
                    continue;

                entry.phasesRemaining--;
                if (entry.phasesRemaining <= 0)
                    expired.Add(kv.Key);
            }

            for (int i = 0; i < expired.Count; i++)
            {
                Vector3Int tile = expired[i];
                if (_byTile.TryGetValue(tile, out FloorEssenceEntry entry))
                {
                    string name = entry.essenceData != null ? entry.essenceData.essenceName : "essence";
                    Debug.Log($"[Essence] {name} faded from {tile}.");
                }

                RemoveAtTile(tile);
            }
        }

        public void ApplyVisibility(VisibilityManager visibility)
        {
            if (visibility == null)
                return;

            foreach (KeyValuePair<string, FloorEssenceWorldView> kv in _views)
            {
                FloorEssenceWorldView view = kv.Value;
                if (view != null)
                    view.SetVisible(visibility.IsLitVisible(view.GridCell));
            }
        }

        void SpawnView(FloorEssenceEntry entry)
        {
            FloorEssenceWorldView view = worldViewPrefab != null
                ? Instantiate(worldViewPrefab, _viewRoot)
                : FloorEssenceWorldView.CreateDefault(_viewRoot);

            view.Bind(entry.entryId, entry.essenceData);
            view.SetGridCell(entry.tile);
            view.transform.position = FloorItemPileService.TileCenterWorld(entry.tile);
            _views[entry.entryId] = view;
        }

        void DestroyView(string entryId)
        {
            if (!_views.TryGetValue(entryId, out FloorEssenceWorldView view))
                return;

            _views.Remove(entryId);
            if (view != null)
                Destroy(view.gameObject);
        }
    }
}
