using System.Collections.Generic;
using JRogue.Controller.Enemy;
using JRogue.Manager.Map;
using JRogue.Spawn;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class EnemyPopulationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def?.EnemyPopulation == null || def.EnemyPopulation.Count == 0)
                return;

            MapManager map = MapManager.Instance;
            if (map == null)
                return;

            List<Vector3Int> candidates = PopulationPlacementUtility.CollectFloorCandidates(map, context);
            if (candidates.Count == 0)
                return;

            PopulationPlacementUtility.Shuffle(candidates, context.Rng);
            int candidateIndex = 0;

            int spawnedTotal = 0;
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
                    }

                    if (!placed)
                        DungeonGenerationLog.Warn($"Could not place enemy #{spawnIndex + 1} on {def.FloorId}.");
                }
            }

            DungeonGenerationLog.Phase(nameof(EnemyPopulationPhase),
                $"spawned={spawnedTotal} candidates={candidates.Count}");
        }

    }
}
