using System;
using System.Collections.Generic;
using JRogue.Controller.Enemy;
using JRogue.Item;
using JRogue.Manager.Floor;
using JRogue.Manager.Map;
using JRogue.Spawn;
using JRogue.World.Generation;
using JRogue.World.Generation.Phases;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZonePopulationUtility
    {
        public static IReadOnlyList<ResolvedZonePiece> GetHabitatInstances(DungeonGenerationContext context)
        {
            if (context?.ResolvedZonePieces == null || context.ResolvedZonePieces.Length == 0)
                return Array.Empty<ResolvedZonePiece>();

            var habitat = new List<ResolvedZonePiece>();
            for (int i = 0; i < context.ResolvedZonePieces.Length; i++)
            {
                ResolvedZonePiece piece = context.ResolvedZonePieces[i];
                if (IsHabitatZone(piece.ZoneId))
                    habitat.Add(piece);
            }

            return habitat;
        }

        public static bool TryGetZoneInstanceBounds(
            DungeonGenerationContext context,
            string zoneInstanceId,
            out RectInt bounds)
        {
            bounds = default;
            if (context == null || string.IsNullOrEmpty(zoneInstanceId))
                return false;

            if (context.ZoneBoundsByInstanceId != null
                && context.ZoneBoundsByInstanceId.TryGetValue(zoneInstanceId, out bounds))
            {
                return true;
            }

            if (context.ResolvedZonePieces == null)
                return false;

            for (int i = 0; i < context.ResolvedZonePieces.Length; i++)
            {
                ResolvedZonePiece piece = context.ResolvedZonePieces[i];
                if (piece.ZoneInstanceId != zoneInstanceId)
                    continue;

                bounds = piece.Bounds;
                return true;
            }

            return false;
        }

        public static IReadOnlyList<ZoneEnemyPopulationEntry> ResolveEnemyEntries(
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            string zoneId)
        {
            if (layout != null
                && layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef)
                && zoneDef.PopulationProfile != null)
            {
                return zoneDef.PopulationProfile.EnemyPopulation
                    ?? Array.Empty<ZoneEnemyPopulationEntry>();
            }

            if (floorDef == null || !floorDef.UseFloorPopulationAsFallback)
                return Array.Empty<ZoneEnemyPopulationEntry>();

            return ConvertFloorEnemies(floorDef.EnemyPopulation);
        }

        public static IReadOnlyList<ZoneFloorItemPopulationEntry> ResolveFloorItemEntries(
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            string zoneId)
        {
            if (layout != null
                && layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef)
                && zoneDef.PopulationProfile != null)
            {
                return zoneDef.PopulationProfile.FloorItemPopulation
                    ?? Array.Empty<ZoneFloorItemPopulationEntry>();
            }

            if (floorDef == null || !floorDef.UseFloorPopulationAsFallback)
                return Array.Empty<ZoneFloorItemPopulationEntry>();

            return ConvertFloorItems(floorDef.FloorItemPopulation);
        }

        public static int ScatterEnemies(
            DungeonGenerationContext context,
            MapManager map,
            IReadOnlyList<ZoneEnemyPopulationEntry> entries,
            List<Vector3Int> candidates,
            System.Random rng,
            ResolvedZonePiece zoneInstance,
            out int spawnAttempts,
            out int spawnFailures)
        {
            spawnAttempts = 0;
            spawnFailures = 0;
            if (context == null || map == null || entries == null || entries.Count == 0 || candidates == null)
                return 0;

            PopulationPlacementUtility.Shuffle(candidates, rng);
            int candidateIndex = 0;
            int spawnedTotal = 0;

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                ZoneEnemyPopulationEntry entry = entries[entryIndex];
                if (entry.spawnDefinition == null)
                    continue;

                int count = rng.Next(entry.minCount, entry.maxCount + 1);
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
                            $"Could not place {zoneInstance.ZoneId} enemy #{spawnIndex + 1} " +
                            $"in {zoneInstance.ZoneInstanceId} (attempts={spawnAttempts} failures={spawnFailures}).");
                    }
                }
            }

            return spawnedTotal;
        }

        public static int ScatterFloorItems(
            DungeonGenerationContext context,
            MapManager map,
            FloorItemPileService piles,
            IReadOnlyList<ZoneFloorItemPopulationEntry> entries,
            List<Vector3Int> candidates,
            System.Random rng,
            ResolvedZonePiece zoneInstance)
        {
            if (context == null || map == null || piles == null || entries == null || entries.Count == 0
                || candidates == null)
            {
                return 0;
            }

            PopulationPlacementUtility.Shuffle(candidates, rng);
            int candidateIndex = 0;
            int placed = 0;

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                ZoneFloorItemPopulationEntry entry = entries[entryIndex];
                if (entry.itemData == null)
                    continue;

                int count = rng.Next(entry.minCount, entry.maxCount + 1);
                for (int i = 0; i < count; i++)
                {
                    while (candidateIndex < candidates.Count)
                    {
                        Vector3Int cell = candidates[candidateIndex++];
                        if (!PopulationPlacementUtility.IsPopulationCell(map, context, cell))
                            continue;

                        if (piles.GetEntries(cell).Count > 0)
                            continue;

                        int minQty = entry.minQuantity > 0 ? entry.minQuantity : 1;
                        int maxQty = entry.maxQuantity > 0 ? entry.maxQuantity : minQty;
                        int qty = rng.Next(minQty, maxQty + 1);
                        piles.AddEntry(cell, new ItemInstance(entry.itemData, qty));
                        placed++;
                        break;
                    }
                }
            }

            if (placed == 0 && entries.Count > 0)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(FloorItemPopulationPhase)}: placed no items in {zoneInstance.ZoneInstanceId}.");
            }

            return placed;
        }

        static ZoneEnemyPopulationEntry[] ConvertFloorEnemies(IReadOnlyList<EnemyPopulationEntry> floorEntries)
        {
            if (floorEntries == null || floorEntries.Count == 0)
                return Array.Empty<ZoneEnemyPopulationEntry>();

            var converted = new ZoneEnemyPopulationEntry[floorEntries.Count];
            for (int i = 0; i < floorEntries.Count; i++)
            {
                EnemyPopulationEntry entry = floorEntries[i];
                converted[i] = new ZoneEnemyPopulationEntry
                {
                    spawnDefinition = entry.spawnDefinition,
                    minCount = entry.minCount,
                    maxCount = entry.maxCount,
                };
            }

            return converted;
        }

        static ZoneFloorItemPopulationEntry[] ConvertFloorItems(IReadOnlyList<FloorItemPopulationEntry> floorEntries)
        {
            if (floorEntries == null || floorEntries.Count == 0)
                return Array.Empty<ZoneFloorItemPopulationEntry>();

            var converted = new ZoneFloorItemPopulationEntry[floorEntries.Count];
            for (int i = 0; i < floorEntries.Count; i++)
            {
                FloorItemPopulationEntry entry = floorEntries[i];
                converted[i] = new ZoneFloorItemPopulationEntry
                {
                    itemData = entry.itemData,
                    minCount = entry.minCount,
                    maxCount = entry.maxCount,
                    minQuantity = entry.minQuantity,
                    maxQuantity = entry.maxQuantity,
                };
            }

            return converted;
        }

        static bool IsHabitatZone(string zoneId) =>
            !string.IsNullOrEmpty(zoneId)
            && zoneId != ZoneIds.Empty
            && zoneId != ZoneIds.Rock;
    }
}
