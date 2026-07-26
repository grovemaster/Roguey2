using System;
using JRogue.Spawn;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Rift
{
    [Serializable]
    public struct RiftEnemySpawnSpec
    {
        public EnemySpawnDefinition spawnDefinition;
        public Vector3Int cell;
        public bool isBoss;
    }

    [Serializable]
    public struct RiftConditionalSummonSpec
    {
        public string conditionId;
        public Vector3Int roomMinInclusive;
        public Vector3Int roomMaxInclusive;
        public RiftEnemySpawnSpec[] spawns;
    }

    /// <summary>Data contract for a hand-authored rift (Docs/World/Rift-Requirements.md §3.1).</summary>
    [CreateAssetMenu(fileName = "RiftDefinition", menuName = "JRogue/World/Rift Definition")]
    public sealed class RiftDefinition : ScriptableObject
    {
        public string riftId = "rift_test";
        public string displayName = "Rift Test";
        public string[] hostFloorIds = { DungeonFloorTransitionIds.Floor01Id };

        [Tooltip("Floor definition used as the rift instance (PreBakedStamp).")]
        public DungeonFloorDefinition riftFloorDefinition;

        public Vector3Int entryAnchor = new Vector3Int(9, 2, 0);
        public Vector3Int exitPortalCell = new Vector3Int(9, 69, 0);

        public RiftEnemySpawnSpec[] initialSpawns = Array.Empty<RiftEnemySpawnSpec>();
        public RiftConditionalSummonSpec[] conditionalSummons = Array.Empty<RiftConditionalSummonSpec>();

        public string EnterCombatLogLine => $"You have entered {displayName}";

        public bool IsHostedBy(string floorId)
        {
            if (string.IsNullOrEmpty(floorId) || hostFloorIds == null)
                return false;
            for (int i = 0; i < hostFloorIds.Length; i++)
            {
                if (hostFloorIds[i] == floorId)
                    return true;
            }

            return false;
        }
    }
}
