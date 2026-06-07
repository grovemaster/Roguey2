using System.Collections.Generic;
using JRogue.Hazards;
using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class HazardPopulationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def?.HazardPopulation == null || def.HazardPopulation.Count == 0)
                return;

            HazardService hazards = HazardService.Instance;
            MapManager map = MapManager.Instance;
            if (hazards == null || map == null)
            {
                DungeonGenerationLog.Warn($"{nameof(HazardPopulationPhase)}: HazardService or MapManager missing.");
                return;
            }

            hazards.SetOverlayMap(context.Instance.Tilemaps.HazardOverlayMap);

            List<Vector3Int> candidates = PopulationPlacementUtility.CollectFloorCandidates(map, context);
            if (candidates.Count == 0)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(HazardPopulationPhase)}: no population candidates; " +
                    PopulationPlacementUtility.DescribePopulationFailure(map, context));
                return;
            }

            PopulationPlacementUtility.Shuffle(candidates, context.Rng);
            int candidateIndex = 0;
            int placed = 0;

            for (int entryIndex = 0; entryIndex < def.HazardPopulation.Count; entryIndex++)
            {
                HazardPopulationEntry entry = def.HazardPopulation[entryIndex];
                if (entry.definition == null)
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

                        if (hazards.HasHazardAt(cell))
                            continue;

                        hazards.Register(cell, entry.definition, entry.startHidden);
                        placed++;
                        break;
                    }
                }
            }

            DungeonGenerationLog.Phase(nameof(HazardPopulationPhase), $"placed={placed}");
        }
    }
}
