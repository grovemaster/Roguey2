using System.Collections.Generic;
using JRogue.Interactables;
using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class InteractablePopulationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def?.InteractablePopulation == null || def.InteractablePopulation.Count == 0)
                return;

            InteractableTileService interactables = InteractableTileService.Instance;
            MapManager map = MapManager.Instance;
            if (interactables == null || map == null)
            {
                DungeonGenerationLog.Warn($"{nameof(InteractablePopulationPhase)}: InteractableTileService or MapManager missing.");
                return;
            }

            interactables.SetOverlayMap(context.Instance.Tilemaps.InteractableOverlayMap);

            List<Vector3Int> candidates = PopulationPlacementUtility.CollectFloorCandidates(map, context);
            if (candidates.Count == 0)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(InteractablePopulationPhase)}: no population candidates; " +
                    PopulationPlacementUtility.DescribePopulationFailure(map, context));
                return;
            }

            PopulationPlacementUtility.Shuffle(candidates, context.Rng);
            int candidateIndex = 0;
            int placed = 0;

            for (int entryIndex = 0; entryIndex < def.InteractablePopulation.Count; entryIndex++)
            {
                InteractablePopulationEntry entry = def.InteractablePopulation[entryIndex];
                if (entry.definition == null)
                    continue;

                int count = context.Rng.Next(entry.minCount, entry.maxCount + 1);
                for (int i = 0; i < count; i++)
                {
                    while (candidateIndex < candidates.Count)
                    {
                        Vector3Int cell = candidates[candidateIndex++];
                        if (interactables.TryGetInstance(cell, out _))
                            continue;

                        interactables.Register(cell, entry.definition);
                        placed++;
                        break;
                    }
                }
            }

            DungeonGenerationLog.Phase(nameof(InteractablePopulationPhase), $"placed={placed}");
        }
    }
}
