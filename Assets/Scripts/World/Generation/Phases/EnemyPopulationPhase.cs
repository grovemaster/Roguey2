using System.Collections.Generic;
using JRogue.Controller.Enemy;
using JRogue.Manager.Map;
using JRogue.Spawn;
using JRogue.World.Generation;
using JRogue.World.Generation.MonsterSpawn;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class EnemyPopulationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null)
                return;

            MapManager map = MapManager.Instance;
            if (map == null)
                return;

            if (context.UsesZoneComposite)
            {
                ExecuteZoneComposite(context, def, map);
                return;
            }

            ExecuteFloorWide(context, def, map);
        }

        static void ExecuteZoneComposite(
            DungeonGenerationContext context,
            DungeonFloorDefinition def,
            MapManager map)
        {
            ZoneGenerationDiagnostics.LogPopulationByZone(nameof(EnemyPopulationPhase), map, context);
            ZoneGenerationDiagnostics.LogZoneInstancePopulationCandidates(context, "before EnemyPopulationPhase");

            IReadOnlyList<ResolvedZonePiece> instances = ZonePopulationUtility.GetHabitatInstances(context);
            if (instances.Count == 0)
            {
                DungeonGenerationLog.Warn($"{nameof(EnemyPopulationPhase)}: no habitat zone instances.");
                return;
            }

            int spawnedTotal = 0;
            int spawnAttempts = 0;
            int spawnFailures = 0;
            int candidateTotal = 0;
            DungeonFloorZoneLayout layout = def.ZoneLayout;

            for (int i = 0; i < instances.Count; i++)
            {
                ResolvedZonePiece instance = instances[i];
                if (MonsterSpawnScheduleService.UsesScheduledEnemyGroups(def, layout, instance.ZoneId))
                    continue;

                IReadOnlyList<ZoneEnemyPopulationEntry> entries =
                    ZonePopulationUtility.ResolveEnemyEntries(def, layout, instance.ZoneId);
                if (entries == null || entries.Count == 0)
                    continue;

                List<Vector3Int> candidates = PopulationPlacementUtility.CollectZoneInstanceCandidates(
                    map,
                    context,
                    instance.ZoneInstanceId);
                if (candidates.Count == 0)
                {
                    DungeonGenerationLog.Warn(
                        $"{nameof(EnemyPopulationPhase)}: no candidates for {instance.ZoneInstanceId}.");
                    continue;
                }

                candidateTotal += candidates.Count;
                System.Random rng = ZoneGenerationRng.CreateZonePopulationRng(
                    context.RunSeed,
                    def.FloorId,
                    instance.ZoneInstanceId,
                    "Enemy");

                int zoneSpawned = ZonePopulationUtility.ScatterEnemies(
                    context,
                    map,
                    entries,
                    candidates,
                    rng,
                    instance,
                    out int zoneAttempts,
                    out int zoneFailures);
                spawnedTotal += zoneSpawned;
                spawnAttempts += zoneAttempts;
                spawnFailures += zoneFailures;
                ZonePopulationUtility.RecordZoneScatter(
                    context,
                    instance.ZoneInstanceId,
                    counts => counts.Enemies += zoneSpawned);
            }

            ZoneGenerationDiagnostics.LogZonePopulationScatterSummary(context);

            DungeonGenerationLog.Phase(nameof(EnemyPopulationPhase),
                $"zoneComposite spawned={spawnedTotal} instances={instances.Count} " +
                $"candidates={candidateTotal} attempts={spawnAttempts} failures={spawnFailures}");
        }

        static void ExecuteFloorWide(
            DungeonGenerationContext context,
            DungeonFloorDefinition def,
            MapManager map)
        {
            if (def.MonsterPopulationMode == MonsterSpawn.MonsterPopulationMode.ScheduledGroups)
                return;

            if (def.EnemyPopulation == null || def.EnemyPopulation.Count == 0)
                return;

            ZoneGenerationDiagnostics.LogPopulationByZone(nameof(EnemyPopulationPhase), map, context);

            List<Vector3Int> candidates = PopulationPlacementUtility.CollectFloorCandidates(map, context);
            if (candidates.Count == 0)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(EnemyPopulationPhase)}: no population candidates; " +
                    PopulationPlacementUtility.DescribePopulationFailure(map, context));
                return;
            }

            PopulationPlacementUtility.Shuffle(candidates, context.Rng);
            int candidateIndex = 0;

            int spawnedTotal = 0;
            int spawnAttempts = 0;
            int spawnFailures = 0;
            for (int entryIndex = 0; entryIndex < def.EnemyPopulation.Count; entryIndex++)
            {
                EnemyPopulationEntry entry = def.EnemyPopulation[entryIndex];
                if (entry.spawnDefinition == null)
                    continue;

                int count = context.Rng.Next(entry.minCount, entry.maxCount + 1);
                for (int spawnIndex = 0; spawnIndex < count; spawnIndex++)
                {
                    bool placed = false;
                    while (candidateIndex < candidates.Count)
                    {
                        Vector3Int origin = candidates[candidateIndex++];
                        if (!PopulationPlacementUtility.IsPopulationCell(map, context, origin))
                            continue;

                        spawnAttempts++;
                        if (EnemySpawnService.TrySpawn(
                                entry.spawnDefinition,
                                origin,
                                out EnemyController _,
                                context.Instance.EnemyContainer))
                        {
                            placed = true;
                            spawnedTotal++;
                            break;
                        }

                        spawnFailures++;
                    }

                    if (!placed)
                    {
                        DungeonGenerationLog.Warn(
                            $"Could not place enemy #{spawnIndex + 1} on {def.FloorId} " +
                            $"(attempts={spawnAttempts} failures={spawnFailures}).");
                    }
                }
            }

            DungeonGenerationLog.Phase(nameof(EnemyPopulationPhase),
                $"spawned={spawnedTotal} candidates={candidates.Count} " +
                $"attempts={spawnAttempts} failures={spawnFailures}");
        }
    }
}
