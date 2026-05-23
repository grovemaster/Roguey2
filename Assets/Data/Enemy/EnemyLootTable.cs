using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Data.Enemy
{
    [CreateAssetMenu(fileName = "EnemyLootTable", menuName = "JRogue/Enemy/Loot Table")]
    public class EnemyLootTable : ScriptableObject
    {
        public string displayName;

        public List<LootTableEntry> entries = new List<LootTableEntry>();
    }
}
