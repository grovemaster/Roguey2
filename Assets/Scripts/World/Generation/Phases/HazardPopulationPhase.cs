using System.Collections.Generic;
using JRogue.Hazards;
using JRogue.Manager.Map;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class HazardPopulationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context?.Definition;
            if (def == null)
                return;

            HazardService hazards = HazardService.Instance;
            MapManager map = MapManager.Instance;
            if (hazards == null || map == null)
            {
                DungeonGenerationLog.Warn($"{nameof(HazardPopulationPhase)}: HazardService or MapManager missing.");
                return;
            }

            hazards.SetOverlayMap(context.Instance.Tilemaps.HazardOverlayMap);

            if (context.UsesZoneComposite)
            {
                ExecuteZoneComposite(context, def, map, hazards);
                return;
            }

            ExecuteFloorWide(context, def, map, hazards);
        }

        static void ExecuteZoneComposite(
            DungeonGenerationContext context,
            DungeonFloorDefinition def,
            MapManager map,
            HazardService hazards)
        {
            context.ZoneScatterCountsByInstance.Clear();
            ZoneGenerationDiagnostics.LogPopulationByZone(nameof(HazardPopulationPhase), map, context);
            ZoneGenerationDiagnostics.LogZoneInstancePopulationCandidates(context, "before HazardPopulationPhase");

            IReadOnlyList<ResolvedZonePiece> instances = ZonePopulationUtility.GetHabitatInstances(context);
            if (instances.Count == 0)
                return;

            int placedTotal = 0;
            DungeonFloorZoneLayout layout = def.ZoneLayout;

            for (int i = 0; i < instances.Count; i++)
            {
                ResolvedZonePiece instance = instances[i];
                IReadOnlyList<ZoneHazardPopulationEntry> entries =
                    ZonePopulationUtility.ResolveHazardEntries(def, layout, instance.ZoneId);
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
                    "Hazard");

                int placed = ZonePopulationUtility.ScatterHazards(
                    hazards,
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
                    counts => counts.Hazards += placed);
            }

            DungeonGenerationLog.Phase(nameof(HazardPopulationPhase),
                $"zoneComposite placed={placedTotal} instances={instances.Count}");
        }

        static void ExecuteFloorWide(
            DungeonGenerationContext context,
            DungeonFloorDefinition def,
            MapManager map,
            HazardService hazards)
        {
            if (def.HazardPopulation == null || def.HazardPopulation.Count == 0)
                return;

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
