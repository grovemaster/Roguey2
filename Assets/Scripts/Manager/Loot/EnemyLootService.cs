using JRogue.Controller.Enemy;
using JRogue.Data.Enemy;
using JRogue.Data.Item;
using JRogue.Item;
using JRogue.Item.Essence;
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
            EnemyLootRollResult drops = EnemyLootRoller.Roll(
                species.lootTable,
                species.speciesId,
                manaStoneCatalog,
                roll);

            if (drops.Items.Count == 0 && drops.Essences.Count == 0)
            {
                Debug.Log($"[LOOT] {species.displayName} dropped nothing at {tile}.");
                return;
            }

            FloorItemPileService pile = FloorItemPileService.Instance;
            if (pile == null && drops.Items.Count > 0)
                Debug.LogWarning("[LOOT] No FloorItemPileService in scene; item drops discarded.");

            for (int i = 0; i < drops.Items.Count; i++)
            {
                if (pile == null)
                    break;

                ItemInstance item = drops.Items[i];
                pile.AddEntry(tile, item);
                Debug.Log($"[LOOT] {species.displayName} dropped {DescribeDrop(item)} at {tile}.");
            }

            FloorEssenceService essenceService = FloorEssenceService.Instance;
            if (essenceService == null && drops.Essences.Count > 0)
                Debug.LogWarning("[LOOT] No FloorEssenceService in scene; essence drops discarded.");

            for (int i = 0; i < drops.Essences.Count; i++)
            {
                if (essenceService == null)
                    break;

                EssenceData essence = drops.Essences[i];
                essenceService.SpawnEssence(tile, essence);
                Debug.Log($"[LOOT] {species.displayName} dropped essence {essence.essenceName} at {tile}.");
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
