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

            List<Vector3Int> candidates = CollectCandidates(map, context);
            if (candidates.Count == 0)
                return;

            Shuffle(candidates, context.Rng);
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

        static List<Vector3Int> CollectCandidates(MapManager map, DungeonGenerationContext context)
        {
            DungeonLayoutStamp stamp = context.Definition.LayoutStamp;
            var candidates = new List<Vector3Int>();
            if (stamp == null)
                return candidates;

            for (int y = 0; y < stamp.Height; y++)
            {
                for (int x = 0; x < stamp.Width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!stamp.IsFloor(x, y))
                        continue;
                    if (!map.IsWalkable(cell))
                        continue;
                    if (context.IsInSafeZone(cell))
                        continue;
                    if (context.ReservedCells.Contains(cell))
                        continue;

                    candidates.Add(cell);
                }
            }

            return candidates;
        }

        static void Shuffle(List<Vector3Int> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swap = rng.Next(i + 1);
                (list[i], list[swap]) = (list[swap], list[i]);
            }
        }
    }
}
