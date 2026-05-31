using JRogue.Data.Enemy;
using JRogue.Data.Item;
using JRogue.Item;
using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Service.Loot
{
    public static class EnemyLootRoller
    {
        public static EnemyLootRollResult Roll(
            EnemyLootTable table,
            string sourceSpeciesId,
            ManaStoneTierCatalog catalog,
            ILootRandom rng)
        {
            var result = new EnemyLootRollResult();
            if (table == null || table.entries == null || table.entries.Count == 0 || rng == null)
                return result;

            string species = string.IsNullOrEmpty(sourceSpeciesId) ? "unknown" : sourceSpeciesId;

            foreach (LootTableEntry entry in table.entries)
            {
                if (entry == null || rng.NextFloat() > entry.dropChance)
                    continue;

                int qty = Mathf.Max(1, entry.quantity);
                switch (entry.payload)
                {
                    case LootTablePayload.ManaStone:
                        if (catalog == null)
                        {
                            Debug.LogWarning("[LOOT] Mana stone drop skipped: no ManaStoneTierCatalog.");
                            break;
                        }

                        ManaStoneItemData manaDef = catalog.GetByTier(entry.manaStoneTier);
                        if (manaDef == null)
                        {
                            Debug.LogWarning($"[LOOT] No mana stone definition for tier {entry.manaStoneTier}.");
                            break;
                        }

                        for (int i = 0; i < qty; i++)
                            result.Items.Add(ItemInstance.CreateManaStone(manaDef, species));
                        break;

                    case LootTablePayload.ItemData:
                        if (entry.itemData == null)
                            break;

                        for (int i = 0; i < qty; i++)
                        {
                            var inst = new ItemInstance(entry.itemData);
                            inst.StorageLocation = ItemStorageLocation.OnGround;
                            result.Items.Add(inst);
                        }

                        break;

                    case LootTablePayload.Essence:
                        if (entry.essenceData == null)
                            break;

                        for (int i = 0; i < qty; i++)
                            result.Essences.Add(entry.essenceData);
                        break;
                }
            }

            return result;
        }
    }
}
