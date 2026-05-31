using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.Traps;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class TrapPopulationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def?.TrapPopulation == null || def.TrapPopulation.Count == 0)
                return;

            TrapService traps = TrapService.Instance;
            MapManager map = MapManager.Instance;
            if (traps == null || map == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TrapPopulationPhase)}: TrapService or MapManager missing.");
                return;
            }

            traps.SetOverlayMap(context.Instance.Tilemaps.TrapOverlayMap);

            List<Vector3Int> candidates = PopulationPlacementUtility.CollectFloorCandidates(map, context);
            if (candidates.Count == 0)
                return;

            PopulationPlacementUtility.Shuffle(candidates, context.Rng);
            int candidateIndex = 0;
            int placed = 0;

            for (int entryIndex = 0; entryIndex < def.TrapPopulation.Count; entryIndex++)
            {
                TrapPopulationEntry entry = def.TrapPopulation[entryIndex];
                if (entry.definition == null || entry.definition.placement != TrapPlacement.Floor)
                    continue;

                int count = context.Rng.Next(entry.minCount, entry.maxCount + 1);
                for (int i = 0; i < count; i++)
                {
                    while (candidateIndex < candidates.Count)
                    {
                        Vector3Int cell = candidates[candidateIndex++];
                        if (!PopulationPlacementUtility.IsPopulationCell(
                                map,
                                def.LayoutStamp,
                                cell,
                                context))
                            continue;

                        if (traps.IsFloorTrapAt(cell))
                            continue;

                        traps.Register(cell, entry.definition);
                        placed++;
                        break;
                    }
                }
            }

            DungeonGenerationLog.Phase(nameof(TrapPopulationPhase), $"placed={placed}");
        }
    }
}
