using UnityEngine;

namespace JRogue.World.Generation.MonsterSpawn
{
    public sealed class MonsterSpawnGroupMembership : MonoBehaviour
    {
        [SerializeField] string groupId;
        [SerializeField] string compositionRowId;
        [SerializeField] int spawnedOnDungeonDay;

        public string GroupId => groupId;
        public string CompositionRowId => compositionRowId;
        public int SpawnedOnDungeonDay => spawnedOnDungeonDay;

        public void Initialize(string scopedGroupId, string rowId, int dungeonDay)
        {
            groupId = scopedGroupId ?? string.Empty;
            compositionRowId = rowId ?? string.Empty;
            spawnedOnDungeonDay = dungeonDay;
        }
    }
}
