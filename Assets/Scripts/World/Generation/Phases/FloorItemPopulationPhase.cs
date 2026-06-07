using System.Collections.Generic;
using JRogue.Item;
using JRogue.Manager.Floor;
using JRogue.Manager.Map;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class FloorItemPopulationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def?.FloorItemPopulation == null || def.FloorItemPopulation.Count == 0)
                return;

            FloorItemPileService piles = FloorItemPileService.Instance;
            MapManager map = MapManager.Instance;
            if (piles == null || map == null)
            {
                DungeonGenerationLog.Warn($"{nameof(FloorItemPopulationPhase)}: FloorItemPileService or MapManager missing.");
                return;
            }

            ZoneGenerationDiagnostics.LogPopulationByZone(nameof(FloorItemPopulationPhase), map, context);

            List<Vector3Int> candidates = PopulationPlacementUtility.CollectFloorCandidates(map, context);
            if (candidates.Count == 0)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(FloorItemPopulationPhase)}: no population candidates; " +
                    PopulationPlacementUtility.DescribePopulationFailure(map, context));
                return;
            }

            PopulationPlacementUtility.Shuffle(candidates, context.Rng);
            int candidateIndex = 0;
            int placed = 0;

            for (int entryIndex = 0; entryIndex < def.FloorItemPopulation.Count; entryIndex++)
            {
                FloorItemPopulationEntry entry = def.FloorItemPopulation[entryIndex];
                if (entry.itemData == null)
                    continue;

                int count = context.Rng.Next(entry.minCount, entry.maxCount + 1);
                for (int i = 0; i < count; i++)
                {
                    while (candidateIndex < candidates.Count)
                    {
                        Vector3Int cell = candidates[candidateIndex++];
                        if (!PopulationPlacementUtility.IsPopulationCell(map, def.LayoutStamp, cell, context))
                            continue;

                        if (piles.GetEntries(cell).Count > 0)
                            continue;

                        int minQty = entry.minQuantity > 0 ? entry.minQuantity : 1;
                        int maxQty = entry.maxQuantity > 0 ? entry.maxQuantity : minQty;
                        int qty = context.Rng.Next(minQty, maxQty + 1);
                        piles.AddEntry(cell, new ItemInstance(entry.itemData, qty));
                        placed++;
                        break;
                    }
                }
            }

            DungeonGenerationLog.Phase(nameof(FloorItemPopulationPhase), $"placed={placed}");
        }
    }
}
