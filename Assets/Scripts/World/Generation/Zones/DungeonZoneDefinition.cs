using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Zones
{
    [CreateAssetMenu(fileName = "ZoneDefinition", menuName = "JRogue/World/Dungeon Zone Definition")]
    public sealed class DungeonZoneDefinition : ScriptableObject
    {
        [SerializeField] string zoneId = "dungeon";
        [SerializeField] string displayName = "Dungeon";
        [SerializeField] TileBase floorTile;
        [SerializeField] TileBase wallTile;
        [SerializeField] int ambientRegionId = -1;
        [SerializeField] int defaultAmbientLight = 0;
        [Min(1)] [SerializeField] int minWidth = 8;
        [Min(1)] [SerializeField] int minHeight = 8;
        [Min(1)] [SerializeField] int maxWidth = 24;
        [Min(1)] [SerializeField] int maxHeight = 24;
        [SerializeField] ZoneFillProfile fillProfile = new ZoneFillProfile { mode = ZoneFillMode.SolidRect };
        [SerializeField] DungeonZonePopulationProfile populationProfile;
        [SerializeField] MonsterSpawn.MonsterPopulationMode monsterPopulationMode =
            MonsterSpawn.MonsterPopulationMode.Scatter;
        [SerializeField] MonsterSpawn.MonsterSpawnScheduleProfile monsterSpawnSchedule;
        [SerializeField] string[] tags;

        public string ZoneId => zoneId;
        public string DisplayName => displayName;
        public TileBase FloorTile => floorTile;
        public TileBase WallTile => wallTile;
        public int AmbientRegionId => ambientRegionId;
        public int DefaultAmbientLight => defaultAmbientLight;
        public int MinWidth => minWidth;
        public int MinHeight => minHeight;
        public int MaxWidth => maxWidth;
        public int MaxHeight => maxHeight;
        public ZoneFillProfile FillProfile => fillProfile;
        public DungeonZonePopulationProfile PopulationProfile => populationProfile;
        public MonsterSpawn.MonsterPopulationMode MonsterPopulationMode => monsterPopulationMode;
        public MonsterSpawn.MonsterSpawnScheduleProfile MonsterSpawnSchedule => monsterSpawnSchedule;
        public string[] Tags => tags;
    }
}
