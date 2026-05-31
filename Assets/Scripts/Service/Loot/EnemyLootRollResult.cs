using System.Collections.Generic;
using JRogue.Item;
using JRogue.Item.Essence;

namespace JRogue.Service.Loot
{
    public sealed class EnemyLootRollResult
    {
        public readonly List<ItemInstance> Items = new List<ItemInstance>();
        public readonly List<EssenceData> Essences = new List<EssenceData>();
    }
}
