using System;
using JRogue.Item;
using JRogue.Spawn;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    [Serializable]
    public struct ZoneEnemyPopulationEntry
    {
        public EnemySpawnDefinition spawnDefinition;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        [Min(0)] public int weight;
    }

    [Serializable]
    public struct ZoneFloorItemPopulationEntry
    {
        public ItemData itemData;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        [Min(1)] public int minQuantity;
        [Min(1)] public int maxQuantity;
    }

    [CreateAssetMenu(
        fileName = "ZonePopulationProfile",
        menuName = "JRogue/World/Dungeon Zone Population Profile")]
    public sealed class DungeonZonePopulationProfile : ScriptableObject
    {
        [SerializeField] ZoneEnemyPopulationEntry[] enemyPopulation = Array.Empty<ZoneEnemyPopulationEntry>();
        [SerializeField] ZoneFloorItemPopulationEntry[] floorItemPopulation =
            Array.Empty<ZoneFloorItemPopulationEntry>();

        public ZoneEnemyPopulationEntry[] EnemyPopulation => enemyPopulation;
        public ZoneFloorItemPopulationEntry[] FloorItemPopulation => floorItemPopulation;
    }
}
