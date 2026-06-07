using System;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Item;
using JRogue.Spawn;
using JRogue.Traps;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public sealed class ZonePopulationScatterCounts
    {
        public int Enemies;
        public int Hazards;
        public int Traps;
        public int FloorItems;
        public int Interactables;
    }

    [Serializable]
    public struct ZoneEnemyPopulationEntry
    {
        public EnemySpawnDefinition spawnDefinition;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        [Min(0)] public int weight;
        public ZonePopulationDensityMode densityMode;
        public string requiresTag;
        [Min(0)] public int forbiddenNearEdge;
    }

    [Serializable]
    public struct ZoneFloorItemPopulationEntry
    {
        public ItemData itemData;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        [Min(1)] public int minQuantity;
        [Min(1)] public int maxQuantity;
        public ZonePopulationDensityMode densityMode;
        public string requiresTag;
        [Min(0)] public int forbiddenNearEdge;
    }

    [Serializable]
    public struct ZoneHazardPopulationEntry
    {
        public EnvironmentalHazardDefinition definition;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        public bool startHidden;
        public ZonePopulationDensityMode densityMode;
        public string requiresTag;
        [Min(0)] public int forbiddenNearEdge;
    }

    [Serializable]
    public struct ZoneTrapPopulationEntry
    {
        public TrapDefinition definition;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        public ZonePopulationDensityMode densityMode;
        public string requiresTag;
        [Min(0)] public int forbiddenNearEdge;
    }

    [Serializable]
    public struct ZoneInteractablePopulationEntry
    {
        public InteractableTileDefinition definition;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        public ZonePopulationDensityMode densityMode;
        public string requiresTag;
        [Min(0)] public int forbiddenNearEdge;
    }

    [CreateAssetMenu(
        fileName = "ZonePopulationProfile",
        menuName = "JRogue/World/Dungeon Zone Population Profile")]
    public sealed class DungeonZonePopulationProfile : ScriptableObject
    {
        [SerializeField] ZoneEnemyPopulationEntry[] enemyPopulation = Array.Empty<ZoneEnemyPopulationEntry>();
        [SerializeField] ZoneFloorItemPopulationEntry[] floorItemPopulation =
            Array.Empty<ZoneFloorItemPopulationEntry>();
        [SerializeField] ZoneHazardPopulationEntry[] hazardPopulation = Array.Empty<ZoneHazardPopulationEntry>();
        [SerializeField] ZoneTrapPopulationEntry[] trapPopulation = Array.Empty<ZoneTrapPopulationEntry>();
        [SerializeField] ZoneInteractablePopulationEntry[] interactablePopulation =
            Array.Empty<ZoneInteractablePopulationEntry>();

        public ZoneEnemyPopulationEntry[] EnemyPopulation => enemyPopulation;
        public ZoneFloorItemPopulationEntry[] FloorItemPopulation => floorItemPopulation;
        public ZoneHazardPopulationEntry[] HazardPopulation => hazardPopulation;
        public ZoneTrapPopulationEntry[] TrapPopulation => trapPopulation;
        public ZoneInteractablePopulationEntry[] InteractablePopulation => interactablePopulation;
    }
}
