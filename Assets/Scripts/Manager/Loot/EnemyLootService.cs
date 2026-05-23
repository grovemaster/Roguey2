using System.Collections.Generic;
using JRogue.Controller.Enemy;
using JRogue.Data.Enemy;
using JRogue.Data.Item;
using JRogue.Item;
using JRogue.Manager.Floor;
using JRogue.Service.Loot;
using UnityEngine;

namespace JRogue.Manager.Loot
{
    public sealed class EnemyLootService : MonoBehaviour
    {
        public static EnemyLootService Instance { get; private set; }

        [SerializeField] ManaStoneTierCatalog manaStoneCatalog;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureCatalog();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void EnsureCatalog()
        {
            if (manaStoneCatalog == null)
                manaStoneCatalog = ManaStoneTierCatalog.LoadDefault();
        }

        public void SpawnDeathLoot(EnemyController enemy, ILootRandom rng = null)
        {
            if (enemy == null)
                return;

            EnemySpeciesDefinition species = enemy.Species;
            if (species == null || species.lootTable == null)
                return;

            EnsureCatalog();
            ILootRandom roll = rng ?? UnityLootRandom.Default;
            Vector3Int tile = enemy.GridPosition;
            List<ItemInstance> drops = EnemyLootRoller.Roll(species.lootTable, species.speciesId, manaStoneCatalog, roll);

            if (drops.Count == 0)
            {
                Debug.Log($"[LOOT] {species.displayName} dropped nothing at {tile}.");
                return;
            }

            FloorItemPileService pile = FloorItemPileService.Instance;
            if (pile == null)
            {
                Debug.LogWarning("[LOOT] No FloorItemPileService in scene; drops discarded.");
                return;
            }

            for (int i = 0; i < drops.Count; i++)
            {
                pile.AddEntry(tile, drops[i]);
                Debug.Log($"[LOOT] {species.displayName} dropped {DescribeDrop(drops[i])} at {tile}.");
            }
        }

        static string DescribeDrop(ItemInstance instance)
        {
            if (instance?.Definition is ManaStoneItemData ms)
                return $"Mana Stone T{ms.tier} ({instance.ManaStoneSourceSpeciesId})";
            return instance?.Definition != null ? instance.Definition.itemName : "item";
        }
    }
}
