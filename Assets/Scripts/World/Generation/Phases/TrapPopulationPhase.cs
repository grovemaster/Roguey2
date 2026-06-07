using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.Traps;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class TrapPopulationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context?.Definition;
            if (def == null)
                return;

            TrapService traps = TrapService.Instance;
            MapManager map = MapManager.Instance;
            if (traps == null || map == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TrapPopulationPhase)}: TrapService or MapManager missing.");
                return;
            }

            traps.SetOverlayMap(context.Instance.Tilemaps.TrapOverlayMap);

            if (context.UsesZoneComposite)
            {
                ExecuteZoneComposite(context, def, map, traps);
                return;
            }

            ExecuteFloorWide(context, def, map, traps);
        }

        static void ExecuteZoneComposite(
            DungeonGenerationContext context,
            DungeonFloorDefinition def,
            MapManager map,
            TrapService traps)
        {
            ZoneGenerationDiagnostics.LogPopulationByZone(nameof(TrapPopulationPhase), map, context);

            IReadOnlyList<ResolvedZonePiece> instances = ZonePopulationUtility.GetHabitatInstances(context);
            if (instances.Count == 0)
                return;

            int placedTotal = 0;
            DungeonFloorZoneLayout layout = def.ZoneLayout;

            for (int i = 0; i < instances.Count; i++)
            {
                ResolvedZonePiece instance = instances[i];
                IReadOnlyList<ZoneTrapPopulationEntry> entries =
                    ZonePopulationUtility.ResolveTrapEntries(def, layout, instance.ZoneId);
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
                    "Trap");

                int placed = ZonePopulationUtility.ScatterTraps(
                    traps,
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
                    counts => counts.Traps += placed);
            }

            DungeonGenerationLog.Phase(nameof(TrapPopulationPhase),
                $"zoneComposite placed={placedTotal} instances={instances.Count}");
        }

        static void ExecuteFloorWide(
            DungeonGenerationContext context,
            DungeonFloorDefinition def,
            MapManager map,
            TrapService traps)
        {
            if (def.TrapPopulation == null || def.TrapPopulation.Count == 0)
                return;

            List<Vector3Int> candidates = PopulationPlacementUtility.CollectFloorCandidates(map, context);
            if (candidates.Count == 0)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(TrapPopulationPhase)}: no population candidates; " +
                    PopulationPlacementUtility.DescribePopulationFailure(map, context));
                return;
            }

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
