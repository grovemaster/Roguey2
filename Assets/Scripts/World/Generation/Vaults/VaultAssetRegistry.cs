using System;
using System.Collections.Generic;
using JRogue.Data.Door;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Item;
using JRogue.Spawn;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Vaults
{
    [CreateAssetMenu(fileName = "VaultAssetRegistry", menuName = "JRogue/World/Vault Asset Registry")]
    public sealed class VaultAssetRegistry : ScriptableObject
    {
        [Serializable]
        public struct TileEntry
        {
            public string key;
            public TileBase tile;
        }

        [Serializable]
        public struct ItemEntry
        {
            public string id;
            public ItemData item;
        }

        [Serializable]
        public struct InteractableEntry
        {
            public string id;
            public InteractableTileDefinition definition;
        }

        [Serializable]
        public struct HazardEntry
        {
            public string id;
            public EnvironmentalHazardDefinition definition;
        }

        [Serializable]
        public struct DoorEntry
        {
            public string id;
            public DoorDefinition definition;
        }

        [Serializable]
        public struct EnemyEntry
        {
            public string id;
            public EnemySpawnDefinition spawnDefinition;
        }

        [SerializeField] List<TileEntry> tiles = new List<TileEntry>();
        [SerializeField] List<ItemEntry> items = new List<ItemEntry>();
        [SerializeField] List<InteractableEntry> interactables = new List<InteractableEntry>();
        [SerializeField] List<HazardEntry> hazards = new List<HazardEntry>();
        [SerializeField] List<DoorEntry> doors = new List<DoorEntry>();
        [SerializeField] List<EnemyEntry> enemies = new List<EnemyEntry>();

        readonly Dictionary<string, TileBase> _tileLookup = new Dictionary<string, TileBase>();
        readonly Dictionary<string, ItemData> _itemLookup = new Dictionary<string, ItemData>();
        readonly Dictionary<string, InteractableTileDefinition> _interactableLookup =
            new Dictionary<string, InteractableTileDefinition>();
        readonly Dictionary<string, EnvironmentalHazardDefinition> _hazardLookup =
            new Dictionary<string, EnvironmentalHazardDefinition>();
        readonly Dictionary<string, DoorDefinition> _doorLookup = new Dictionary<string, DoorDefinition>();
        readonly Dictionary<string, EnemySpawnDefinition> _enemyLookup =
            new Dictionary<string, EnemySpawnDefinition>();

        void OnEnable() => RebuildLookups();

        public void RebuildLookups()
        {
            _tileLookup.Clear();
            _itemLookup.Clear();
            _interactableLookup.Clear();
            _hazardLookup.Clear();
            _doorLookup.Clear();
            _enemyLookup.Clear();

            for (int i = 0; i < tiles.Count; i++)
            {
                TileEntry entry = tiles[i];
                if (!string.IsNullOrEmpty(entry.key) && entry.tile != null)
                    _tileLookup[entry.key] = entry.tile;
            }

            for (int i = 0; i < items.Count; i++)
            {
                ItemEntry entry = items[i];
                if (!string.IsNullOrEmpty(entry.id) && entry.item != null)
                    _itemLookup[entry.id] = entry.item;
            }

            for (int i = 0; i < interactables.Count; i++)
            {
                InteractableEntry entry = interactables[i];
                if (!string.IsNullOrEmpty(entry.id) && entry.definition != null)
                    _interactableLookup[entry.id] = entry.definition;
            }

            for (int i = 0; i < hazards.Count; i++)
            {
                HazardEntry entry = hazards[i];
                if (!string.IsNullOrEmpty(entry.id) && entry.definition != null)
                    _hazardLookup[entry.id] = entry.definition;
            }

            for (int i = 0; i < doors.Count; i++)
            {
                DoorEntry entry = doors[i];
                if (!string.IsNullOrEmpty(entry.id) && entry.definition != null)
                    _doorLookup[entry.id] = entry.definition;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyEntry entry = enemies[i];
                if (!string.IsNullOrEmpty(entry.id) && entry.spawnDefinition != null)
                    _enemyLookup[entry.id] = entry.spawnDefinition;
            }
        }

        public bool TryResolveTile(string key, out TileBase tile) => _tileLookup.TryGetValue(key, out tile);

        public bool TryResolveItem(string id, out ItemData item) => _itemLookup.TryGetValue(id, out item);

        public bool TryResolveInteractable(string id, out InteractableTileDefinition definition) =>
            _interactableLookup.TryGetValue(id, out definition);

        public bool TryResolveHazard(string id, out EnvironmentalHazardDefinition definition) =>
            _hazardLookup.TryGetValue(id, out definition);

        public bool TryResolveDoor(string id, out DoorDefinition definition) =>
            _doorLookup.TryGetValue(id, out definition);

        public bool TryResolveEnemy(string id, out EnemySpawnDefinition spawnDefinition) =>
            _enemyLookup.TryGetValue(id, out spawnDefinition);
    }
}
