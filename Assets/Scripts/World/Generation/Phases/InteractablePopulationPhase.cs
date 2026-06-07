using System.Collections.Generic;
using JRogue.Interactables;
using JRogue.Manager.Map;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class InteractablePopulationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context?.Definition;
            if (def == null)
                return;

            InteractableTileService interactables = InteractableTileService.Instance;
            MapManager map = MapManager.Instance;
            if (interactables == null || map == null)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(InteractablePopulationPhase)}: InteractableTileService or MapManager missing.");
                return;
            }

            interactables.SetOverlayMap(context.Instance.Tilemaps.InteractableOverlayMap);

            if (context.UsesZoneComposite)
            {
                ExecuteZoneComposite(context, def, map, interactables);
                return;
            }

            ExecuteFloorWide(context, def, map, interactables);
        }

        static void ExecuteZoneComposite(
            DungeonGenerationContext context,
            DungeonFloorDefinition def,
            MapManager map,
            InteractableTileService interactables)
        {
            ZoneGenerationDiagnostics.LogPopulationByZone(nameof(InteractablePopulationPhase), map, context);

            IReadOnlyList<ResolvedZonePiece> instances = ZonePopulationUtility.GetHabitatInstances(context);
            if (instances.Count == 0)
                return;

            int placedTotal = 0;
            DungeonFloorZoneLayout layout = def.ZoneLayout;

            for (int i = 0; i < instances.Count; i++)
            {
                ResolvedZonePiece instance = instances[i];
                IReadOnlyList<ZoneInteractablePopulationEntry> entries =
                    ZonePopulationUtility.ResolveInteractableEntries(def, layout, instance.ZoneId);
                if (entries == null || entries.Count == 0)
                    continue;

                List<Vector3Int> candidates = PopulationPlacementUtility.CollectZoneInstanceCandidates(
                    map,
                    context,
                    instance.ZoneInstanceId);
                if (candidates.Count == 0)
                    continue;

                System.Random rng = ZoneGenerationRng.CreateZonePopulationRng(
                    context.RunSeed,
                    def.FloorId,
                    instance.ZoneInstanceId,
                    "Interactable");

                int placed = ZonePopulationUtility.ScatterInteractables(
                    interactables,
                    context,
                    map,
                    entries,
                    candidates,
                    rng,
                    instance);
                placedTotal += placed;
                ZonePopulationUtility.RecordZoneScatter(
                    context,
                    instance.ZoneInstanceId,
                    counts => counts.Interactables += placed);
            }

            DungeonGenerationLog.Phase(nameof(InteractablePopulationPhase),
                $"zoneComposite placed={placedTotal} instances={instances.Count}");
        }

        static void ExecuteFloorWide(
            DungeonGenerationContext context,
            DungeonFloorDefinition def,
            MapManager map,
            InteractableTileService interactables)
        {
            if (def.InteractablePopulation == null || def.InteractablePopulation.Count == 0)
                return;

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
                        if (!PopulationPlacementUtility.IsPopulationCell(map, context, cell))
                            continue;

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
