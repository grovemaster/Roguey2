using System.Collections.Generic;
using JRogue.Controller.Enemy;
using JRogue.Manager.Map;
using JRogue.Spawn;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.MonsterSpawn
{
    public static class MonsterSpawnScheduleService
    {
        public const string LogPrefix = "[MonsterSpawn]";

        public static int GetCurrentDungeonDay()
        {
            DungeonTimeService time = DungeonTimeService.Instance;
            if (time == null || !time.DungeonRunActive)
                return 1;

            return time.ElapsedCycles + 1;
        }

        public static bool AreScheduledSpawnsSuppressed()
        {
            // MS3: wire DisableMonsterSpawnsWhileAliveEffect ref-count on MonsterMapPresenceService.
            return false;
        }

        public static MonsterSpawnApplyResult ApplyForDungeonDay(
            DungeonFloorInstance instance,
            int dungeonDay,
            int runSeed)
        {
            if (instance == null || !instance.IsGenerated || instance.Definition == null)
                return new MonsterSpawnApplyResult(0, 0, 0);

            if (instance.GetLastAppliedMonsterSpawnDay() >= dungeonDay)
            {
                Debug.Log(
                    $"{LogPrefix} Day {dungeonDay} floor={instance.FloorId} skipped (already applied).");
                return new MonsterSpawnApplyResult(0, 0, 0);
            }

            if (AreScheduledSpawnsSuppressed())
            {
                Debug.Log($"{LogPrefix} Day {dungeonDay} suppressed (map presence).");
                return new MonsterSpawnApplyResult(0, 0, 0);
            }

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                Debug.LogWarning($"{LogPrefix} MapManager missing — cannot spawn.");
                return new MonsterSpawnApplyResult(0, 0, 0);
            }

            List<ResolvedMonsterSpawnGroup> groups = CollectGroups(instance);
            if (groups.Count == 0)
            {
                instance.SetLastAppliedMonsterSpawnDay(dungeonDay);
                return new MonsterSpawnApplyResult(0, 0, 0);
            }

            HashSet<string> onceLedger = instance.GetMonsterSpawnOnceLedger();
            int spawnedTotal = 0;
            int skippedRows = 0;
            int failedSpawns = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                ResolvedMonsterSpawnGroup group = groups[i];
                if (!MonsterSpawnScheduleLogic.TryGetDaySchedule(group.Definition, dungeonDay, out MonsterSpawnDaySchedule daySchedule))
                    continue;

                MonsterSpawnCompositionRow[] composition = daySchedule.composition;
                if (composition == null || composition.Length == 0)
                    continue;

                List<Vector3Int> areaCandidates = MonsterSpawnAreaResolver.CollectAreaCandidates(
                    instance,
                    group.Definition.areaBinding,
                    group.ZoneInstanceId,
                    map);
                PopulationPlacementUtility.Shuffle(areaCandidates, CreateGroupRng(runSeed, instance.FloorId, group.ScopedGroupId, dungeonDay));

                for (int rowIndex = 0; rowIndex < composition.Length; rowIndex++)
                {
                    MonsterSpawnCompositionRow row = composition[rowIndex];
                    if (row.spawnDefinition == null)
                        continue;

                    string rowKey = MonsterSpawnScheduleLogic.ResolveRowId(
                        group.ScopedGroupId,
                        row,
                        rowIndex);
                    int alive = CountAlive(instance, group.ScopedGroupId, row.speciesFilter);
                    int spawnCount = MonsterSpawnScheduleLogic.ComputeSpawnCount(
                        row,
                        alive,
                        onceLedger,
                        rowKey,
                        out bool skipped);

                    if (skipped)
                    {
                        skippedRows++;
                        Debug.Log(
                            $"{LogPrefix} Day {dungeonDay} floor={instance.FloorId} group={group.ScopedGroupId} " +
                            $"row={rowKey} skipped (once/ledger alive={alive}).");
                        continue;
                    }

                    if (spawnCount <= 0)
                    {
                        Debug.Log(
                            $"{LogPrefix} Day {dungeonDay} floor={instance.FloorId} group={group.ScopedGroupId} " +
                            $"row={rowKey} target={row.targetCount} alive={alive} spawned=0");
                        continue;
                    }

                    int spawnedForRow = 0;
                    int candidateIndex = 0;
                    for (int spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
                    {
                        if (!TrySpawnOne(
                                instance,
                                group,
                                row,
                                rowKey,
                                dungeonDay,
                                areaCandidates,
                                ref candidateIndex,
                                out _))
                        {
                            failedSpawns++;
                            break;
                        }

                        spawnedForRow++;
                        if (row.fillPolicy == MonsterSpawnFillPolicy.OncePerDungeonIfAbsent)
                            onceLedger.Add(rowKey);
                    }

                    spawnedTotal += spawnedForRow;
                    Debug.Log(
                        $"{LogPrefix} Day {dungeonDay} floor={instance.FloorId} group={group.ScopedGroupId} " +
                        $"row={rowKey} target={row.targetCount} alive={alive} spawned={spawnedForRow}");
                }
            }

            instance.SetLastAppliedMonsterSpawnDay(dungeonDay);
            Debug.Log(
                $"{LogPrefix} Day {dungeonDay} floor={instance.FloorId} complete spawned={spawnedTotal} " +
                $"skippedRows={skippedRows} failures={failedSpawns}");
            return new MonsterSpawnApplyResult(spawnedTotal, skippedRows, failedSpawns);
        }

        public static void ApplyForActiveFloorOnDayStarted(int dungeonDay, int runSeed)
        {
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            DungeonFloorInstance instance = manager?.GetActiveFloorInstance();
            if (instance == null)
                return;

            ApplyForDungeonDay(instance, dungeonDay, runSeed);
        }

        public static bool UsesScheduledEnemyGroups(
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            string zoneId)
        {
            if (floorDef == null || string.IsNullOrEmpty(zoneId))
                return false;

            if (floorDef.MonsterPopulationMode == MonsterPopulationMode.ScheduledGroups)
                return true;

            if (layout == null || !layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef))
                return false;

            return zoneDef.MonsterPopulationMode == MonsterPopulationMode.ScheduledGroups;
        }

        public static List<ResolvedMonsterSpawnGroup> CollectGroups(DungeonFloorInstance instance)
        {
            var resolved = new List<ResolvedMonsterSpawnGroup>();
            if (instance?.Definition == null)
                return resolved;

            DungeonFloorDefinition floorDef = instance.Definition;
            if (floorDef.MonsterSpawnSchedule != null)
                AddGroupsFromProfile(resolved, floorDef.MonsterSpawnSchedule, null, instance);

            DungeonFloorZoneLayout layout = floorDef.ZoneLayout;
            if (layout?.ZoneDefinitions == null)
                return resolved;

            for (int i = 0; i < layout.ZoneDefinitions.Length; i++)
            {
                DungeonZoneDefinition zoneDef = layout.ZoneDefinitions[i];
                if (zoneDef == null || zoneDef.MonsterSpawnSchedule == null)
                    continue;

                AddGroupsFromProfile(resolved, zoneDef.MonsterSpawnSchedule, zoneDef.ZoneId, instance);
            }

            return resolved;
        }

        public static List<ResolvedMonsterSpawnGroup> CollectGroups(DungeonFloorDefinition floorDef)
        {
            var resolved = new List<ResolvedMonsterSpawnGroup>();
            if (floorDef?.MonsterSpawnSchedule == null)
                return resolved;

            AddGroupsFromProfile(resolved, floorDef.MonsterSpawnSchedule, null, null);
            return resolved;
        }

        static void AddGroupsFromProfile(
            List<ResolvedMonsterSpawnGroup> resolved,
            MonsterSpawnScheduleProfile profile,
            string profileZoneId,
            DungeonFloorInstance instance)
        {
            MonsterSpawnGroupDefinition[] groups = profile.Groups;
            for (int i = 0; i < groups.Length; i++)
            {
                MonsterSpawnGroupDefinition group = groups[i];
                if (string.IsNullOrEmpty(group.groupId))
                    continue;

                ExpandGroup(resolved, group, instance, profileZoneId);
            }
        }

        static void ExpandGroup(
            List<ResolvedMonsterSpawnGroup> resolved,
            MonsterSpawnGroupDefinition group,
            DungeonFloorInstance instance,
            string profileZoneId)
        {
            MonsterSpawnAreaBinding binding = group.areaBinding;
            if (binding.kind == MonsterSpawnAreaBindingKind.ZoneInstance
                && !string.IsNullOrEmpty(binding.zoneInstanceId))
            {
                resolved.Add(new ResolvedMonsterSpawnGroup(group, group.groupId, binding.zoneInstanceId));
                return;
            }

            if (binding.kind == MonsterSpawnAreaBindingKind.ZoneId
                && !string.IsNullOrEmpty(binding.zoneId))
            {
                ExpandForZoneId(resolved, group, instance, binding.zoneId);
                return;
            }

            if (!string.IsNullOrEmpty(profileZoneId))
            {
                ExpandForZoneId(resolved, group, instance, profileZoneId);
                return;
            }

            if (binding.kind == MonsterSpawnAreaBindingKind.StampMarkers)
            {
                resolved.Add(new ResolvedMonsterSpawnGroup(group, group.groupId, string.Empty));
                return;
            }

            resolved.Add(new ResolvedMonsterSpawnGroup(group, group.groupId, string.Empty));
        }

        static void ExpandForZoneId(
            List<ResolvedMonsterSpawnGroup> resolved,
            MonsterSpawnGroupDefinition group,
            DungeonFloorInstance instance,
            string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || instance == null)
            {
                resolved.Add(new ResolvedMonsterSpawnGroup(group, group.groupId, string.Empty));
                return;
            }

            IReadOnlyList<ResolvedZonePiece> pieces = instance.ResolvedZonePieces;
            int matchCount = 0;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].ZoneId == zoneId)
                    matchCount++;
            }

            if (matchCount == 0)
            {
                resolved.Add(new ResolvedMonsterSpawnGroup(group, group.groupId, string.Empty));
                return;
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                ResolvedZonePiece piece = pieces[i];
                if (piece.ZoneId != zoneId)
                    continue;

                string scopedGroupId = matchCount > 1
                    ? $"{group.groupId}@{piece.ZoneInstanceId}"
                    : group.groupId;
                resolved.Add(new ResolvedMonsterSpawnGroup(group, scopedGroupId, piece.ZoneInstanceId));
            }
        }

        static int CountAlive(DungeonFloorInstance instance, string scopedGroupId, string speciesFilter)
        {
            if (instance.EnemyContainer == null)
                return 0;

            int count = 0;
            MonsterSpawnGroupMembership[] memberships =
                instance.EnemyContainer.GetComponentsInChildren<MonsterSpawnGroupMembership>(true);
            for (int i = 0; i < memberships.Length; i++)
            {
                MonsterSpawnGroupMembership membership = memberships[i];
                if (membership == null || membership.GroupId != scopedGroupId)
                    continue;

                EnemyController enemy = membership.GetComponent<EnemyController>();
                if (enemy == null)
                    continue;

                string speciesId = enemy.Species != null ? enemy.Species.speciesId : null;
                if (!MonsterSpawnScheduleLogic.MatchesSpeciesFilter(speciesFilter, speciesId))
                    continue;

                count++;
            }

            return count;
        }

        static bool TrySpawnOne(
            DungeonFloorInstance instance,
            ResolvedMonsterSpawnGroup group,
            MonsterSpawnCompositionRow row,
            string rowKey,
            int dungeonDay,
            List<Vector3Int> areaCandidates,
            ref int candidateIndex,
            out EnemyController spawned)
        {
            spawned = null;
            Vector3Int origin = ResolveOrigin(group.Definition, areaCandidates, ref candidateIndex);
            if (origin == InvalidCell())
                return false;

            if (!EnemySpawnService.TrySpawn(
                    row.spawnDefinition,
                    origin,
                    out spawned,
                    instance.EnemyContainer))
            {
                return false;
            }

            MonsterSpawnGroupMembership membership =
                spawned.GetComponent<MonsterSpawnGroupMembership>()
                ?? spawned.gameObject.AddComponent<MonsterSpawnGroupMembership>();
            membership.Initialize(group.ScopedGroupId, rowKey, dungeonDay);
            return true;
        }

        static Vector3Int ResolveOrigin(
            MonsterSpawnGroupDefinition group,
            List<Vector3Int> areaCandidates,
            ref int candidateIndex)
        {
            if (group.anchors != null && group.anchors.Length > 0)
                return group.anchors[0];

            if (areaCandidates == null || areaCandidates.Count == 0)
                return InvalidCell();

            if (group.anchorPolicy == MonsterSpawnAnchorPolicy.RandomInArea)
            {
                int index = candidateIndex < areaCandidates.Count
                    ? candidateIndex++
                    : areaCandidates.Count - 1;
                return areaCandidates[index];
            }

            while (candidateIndex < areaCandidates.Count)
            {
                Vector3Int cell = areaCandidates[candidateIndex++];
                return cell;
            }

            return InvalidCell();
        }

        static System.Random CreateGroupRng(int runSeed, string floorId, string groupId, int dungeonDay)
        {
            int salt = unchecked(runSeed * 397
                ^ (floorId?.GetHashCode() ?? 0)
                ^ (groupId?.GetHashCode() ?? 0)
                ^ dungeonDay);
            return new System.Random(salt);
        }

        static Vector3Int InvalidCell() => new Vector3Int(int.MinValue, int.MinValue, 0);
    }
}
