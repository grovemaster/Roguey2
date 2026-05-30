using JRogue.Controller.Enemy;
using UnityEngine;

namespace JRogue.Spawn
{
    [CreateAssetMenu(fileName = "EnemySpawn", menuName = "JRogue/Spawn/Enemy Spawn Definition")]
    public sealed class EnemySpawnDefinition : ScriptableObject
    {
        [Header("Enemy")]
        public EnemyController enemyPrefab;

        [Header("Placement")]
        public EnemySpawnPlacementPolicy placementPolicy =
            EnemySpawnPlacementPolicy.NorthOfOriginThenNearestUnoccupiedFloor;

        [Tooltip("Primary candidate = origin + this offset (default north +Y).")]
        public Vector3Int primaryOffset = new Vector3Int(0, 1, 0);
    }
}
