using System;
using JRogue.Spawn;
using UnityEngine;

namespace JRogue.World.Generation.MonsterSpawn
{
    public enum MonsterPopulationMode
    {
        Scatter = 0,
        ScheduledGroups = 1,
    }

    public enum MonsterSpawnAreaBindingKind
    {
        ZoneInstance = 0,
        ZoneId = 1,
        StampMarkers = 2,
    }

    public enum MonsterSpawnAnchorPolicy
    {
        NearestWalkableInArea = 0,
        RandomInArea = 1,
        AtAnchor = 2,
    }

    public enum MonsterSpawnFillPolicy
    {
        RefillToTarget = 0,
        OncePerDungeonIfAbsent = 1,
        SpawnExactly = 2,
    }

    [Serializable]
    public struct MonsterSpawnAreaBinding
    {
        public MonsterSpawnAreaBindingKind kind;
        public string zoneInstanceId;
        public string zoneId;
        public string[] markerIds;
    }

    [Serializable]
    public struct MonsterSpawnCompositionRow
    {
        public string rowId;
        public EnemySpawnDefinition spawnDefinition;
        [Min(0)] public int targetCount;
        public MonsterSpawnFillPolicy fillPolicy;
        public string speciesFilter;
    }

    [Serializable]
    public struct MonsterSpawnDaySchedule
    {
        [Min(1)] public int dungeonDay;
        public MonsterSpawnCompositionRow[] composition;
    }

    [Serializable]
    public struct MonsterSpawnGroupDefinition
    {
        public string groupId;
        public string displayName;
        public MonsterSpawnAreaBinding areaBinding;
        public Vector3Int[] anchors;
        public MonsterSpawnAnchorPolicy anchorPolicy;
        public MonsterSpawnDaySchedule[] daySchedules;
    }

    public readonly struct ResolvedMonsterSpawnGroup
    {
        public ResolvedMonsterSpawnGroup(
            MonsterSpawnGroupDefinition definition,
            string scopedGroupId,
            string zoneInstanceId)
        {
            Definition = definition;
            ScopedGroupId = scopedGroupId;
            ZoneInstanceId = zoneInstanceId;
        }

        public MonsterSpawnGroupDefinition Definition { get; }
        public string ScopedGroupId { get; }
        public string ZoneInstanceId { get; }
    }

    public readonly struct MonsterSpawnApplyResult
    {
        public MonsterSpawnApplyResult(int spawned, int skippedRows, int failedSpawns)
        {
            Spawned = spawned;
            SkippedRows = skippedRows;
            FailedSpawns = failedSpawns;
        }

        public int Spawned { get; }
        public int SkippedRows { get; }
        public int FailedSpawns { get; }
    }

}
