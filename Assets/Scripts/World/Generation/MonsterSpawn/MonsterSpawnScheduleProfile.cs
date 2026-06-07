using UnityEngine;

namespace JRogue.World.Generation.MonsterSpawn
{
    [CreateAssetMenu(
        fileName = "MonsterSpawnSchedule",
        menuName = "JRogue/World/Monster Spawn Schedule Profile")]
    public sealed class MonsterSpawnScheduleProfile : ScriptableObject
    {
        [SerializeField] MonsterSpawnGroupDefinition[] groups = System.Array.Empty<MonsterSpawnGroupDefinition>();

        public MonsterSpawnGroupDefinition[] Groups => groups ?? System.Array.Empty<MonsterSpawnGroupDefinition>();
    }
}
