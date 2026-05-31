using System;
using JRogue.Item;
using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Data.Enemy
{
    [Serializable]
    public sealed class LootTableEntry
    {
        [Range(0f, 1f)]
        public float dropChance = 1f;

        public LootTablePayload payload = LootTablePayload.ManaStone;

        [Range(1, 9)]
        public int manaStoneTier = 9;

        public ItemData itemData;

        public EssenceData essenceData;

        [Min(1)]
        public int quantity = 1;
    }
}
