using System.Collections.Generic;
using JRogue.Spawn;

namespace JRogue.World.Generation.MonsterSpawn
{
    public static class MonsterSpawnScheduleLogic
    {
        public static bool TryGetDaySchedule(
            MonsterSpawnGroupDefinition group,
            int dungeonDay,
            out MonsterSpawnDaySchedule schedule)
        {
            schedule = default;
            if (group.daySchedules == null || group.daySchedules.Length == 0)
                return false;

            for (int i = 0; i < group.daySchedules.Length; i++)
            {
                MonsterSpawnDaySchedule candidate = group.daySchedules[i];
                if (candidate.dungeonDay != dungeonDay)
                    continue;

                schedule = candidate;
                return true;
            }

            return false;
        }

        public static string ResolveRowId(
            string scopedGroupId,
            MonsterSpawnCompositionRow row,
            int compositionIndex)
        {
            if (!string.IsNullOrEmpty(row.rowId))
                return row.rowId;

            return $"{scopedGroupId}:{compositionIndex}";
        }

        public static int CountNeededRefill(int targetCount, int aliveCount) =>
            targetCount > aliveCount ? targetCount - aliveCount : 0;

        public static bool ShouldSkipOnceRow(
            HashSet<string> onceLedger,
            string rowKey,
            int aliveCount,
            out bool markLedgerWithoutSpawn)
        {
            markLedgerWithoutSpawn = false;
            if (onceLedger != null && onceLedger.Contains(rowKey))
                return true;

            if (aliveCount > 0)
            {
                markLedgerWithoutSpawn = true;
                return true;
            }

            return false;
        }

        public static bool MatchesSpeciesFilter(string speciesFilter, string speciesId)
        {
            if (string.IsNullOrEmpty(speciesFilter))
                return true;

            return speciesId == speciesFilter;
        }

        public static int ComputeSpawnCount(
            MonsterSpawnCompositionRow row,
            int aliveCount,
            HashSet<string> onceLedger,
            string rowKey,
            out bool skipped)
        {
            skipped = false;
            switch (row.fillPolicy)
            {
                case MonsterSpawnFillPolicy.OncePerDungeonIfAbsent:
                    if (ShouldSkipOnceRow(onceLedger, rowKey, aliveCount, out bool markWithoutSpawn))
                    {
                        skipped = true;
                        if (markWithoutSpawn && onceLedger != null)
                            onceLedger.Add(rowKey);
                        return 0;
                    }

                    return row.spawnDefinition != null ? 1 : 0;

                case MonsterSpawnFillPolicy.SpawnExactly:
                    return row.targetCount;

                default:
                    return CountNeededRefill(row.targetCount, aliveCount);
            }
        }
    }
}
