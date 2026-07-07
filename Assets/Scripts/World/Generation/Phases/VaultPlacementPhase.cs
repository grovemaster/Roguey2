using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.World.Generation.Vaults;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class VaultPlacementPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context?.Definition;
            DungeonVaultCatalog catalog = def?.VaultCatalog;
            if (catalog == null || catalog.Entries == null || catalog.Entries.Count == 0)
            {
                DungeonGenerationLog.Phase(
                    nameof(VaultPlacementPhase),
                    "skipped — no VaultCatalog on floor definition.");
                return;
            }

            DungeonGenerationLog.Phase(
                nameof(VaultPlacementPhase),
                $"begin entries={catalog.Entries.Count} playerStart={context.PlayerStart} " +
                $"(filter Console: {VaultStampDiagnostics.Tag})");

            int placedVaults = VaultPlacementResolver.PlaceCatalog(context, catalog);

            DungeonGenerationLog.Phase(nameof(VaultPlacementPhase), $"placedCount={placedVaults}");
            ZoneGenerationDiagnostics.LogCheckpoint(context, "after VaultPlacementPhase");
        }
    }
}

namespace JRogue.World.Generation.Vaults
{
    /// <summary>Ordered Floor 1 vault placement (§7.5–§7.7).</summary>
    internal static class VaultPlacementResolver
    {
        struct PreparedEntry
        {
            public DungeonVaultCatalogEntry CatalogEntry;
            public VaultBlueprint Blueprint;
        }

        const int ZoneCenterSearchRadius = 16;

        public static int PlaceCatalog(DungeonGenerationContext context, DungeonVaultCatalog catalog)
        {
            if (context == null || catalog?.Entries == null || catalog.Entries.Count == 0)
                return 0;

            VaultAssetRegistry registry = catalog.AssetRegistry;
            if (registry == null)
                return 0;

            registry.RebuildLookups();
            VaultStampDiagnostics.LogRegistryAudit(registry, catalog.name ?? "VaultCatalog");

            if (!TryPrepareEntries(catalog, out List<PreparedEntry> prepared))
                return 0;

            var zoneCenter = new List<PreparedEntry>();
            var mandatoryRandom = new List<PreparedEntry>();
            var nearZoneNorthEdge = new List<PreparedEntry>();
            var pondScatter = new List<PreparedEntry>();
            var random = new List<PreparedEntry>();

            for (int i = 0; i < prepared.Count; i++)
            {
                PreparedEntry entry = prepared[i];
                switch (entry.CatalogEntry.placementRule)
                {
                    case VaultPlacementRule.ZoneCenter:
                        zoneCenter.Add(entry);
                        break;
                    case VaultPlacementRule.MandatoryRandom:
                        mandatoryRandom.Add(entry);
                        break;
                    case VaultPlacementRule.NearZoneNorthEdge:
                        nearZoneNorthEdge.Add(entry);
                        break;
                    case VaultPlacementRule.PondScatter:
                        pondScatter.Add(entry);
                        break;
                    default:
                        random.Add(entry);
                        break;
                }
            }

            int placed = 0;
            placed += PlaceZoneCenterEntries(context, registry, zoneCenter);
            placed += PlaceMandatoryRandomEntries(context, registry, mandatoryRandom);
            placed += PlaceNearZoneNorthEdgeEntries(context, registry, nearZoneNorthEdge);
            placed += PlacePondScatterEntries(context, registry, pondScatter);
            placed += PlaceRandomEntries(context, registry, random);

            MapManager map = MapManager.Instance;
            VaultStampDiagnostics.LogPlacedVaultsAudit(context.PlacedVaultRecords, map, "afterVaultPlacement");
            VaultStampDiagnostics.LogFloorScanForVaultTiles(map, "afterVaultPlacement");

            return placed;
        }

        static bool TryPrepareEntries(DungeonVaultCatalog catalog, out List<PreparedEntry> prepared)
        {
            prepared = new List<PreparedEntry>(catalog.Entries.Count);
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                DungeonVaultCatalogEntry entry = catalog.Entries[i];
                if (entry == null || entry.weight <= 0)
                    continue;

                if (!VaultSourceText.TryRead(entry, out string vaultText, out string readError))
                {
                    DungeonGenerationLog.Warn(
                        $"{nameof(Phases.VaultPlacementPhase)}: cannot read vault '{entry.vaultId}': {readError}");
                    continue;
                }

                if (!VaultFileParser.TryParse(vaultText, out VaultBlueprint blueprint, out string parseError))
                {
                    DungeonGenerationLog.Warn(
                        $"{nameof(Phases.VaultPlacementPhase)}: parse failed for '{entry.vaultId}': {parseError}");
                    continue;
                }

                string vaultId = string.IsNullOrEmpty(entry.vaultId) ? blueprint.VaultId : entry.vaultId;
                if (string.IsNullOrEmpty(vaultId))
                {
                    DungeonGenerationLog.Warn(
                        $"{nameof(Phases.VaultPlacementPhase)}: missing vault id for catalog entry.");
                    continue;
                }

                blueprint.VaultId = vaultId;
                prepared.Add(new PreparedEntry { CatalogEntry = entry, Blueprint = blueprint });
            }

            return prepared.Count > 0;
        }

        static int PlaceZoneCenterEntries(
            DungeonGenerationContext context,
            VaultAssetRegistry registry,
            List<PreparedEntry> entries)
        {
            int placed = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                PreparedEntry prepared = entries[i];
                DungeonVaultCatalogEntry entry = prepared.CatalogEntry;
                VaultBlueprint blueprint = prepared.Blueprint;

                if (!VaultPlacementUtility.TryResolveZoneCenterOrigin(
                        context,
                        blueprint,
                        entry.requiredZoneId,
                        out Vector3Int preferredOrigin))
                {
                    LogMandatoryFailure(entry, blueprint, "could not resolve zone center.");
                    continue;
                }

                bool ok = VaultPlacer.TryPlaceNearPreferredOrigin(
                    blueprint,
                    registry,
                    context,
                    context.Rng,
                    preferredOrigin,
                    entry.minDistanceFromPlayerStart,
                    entry.requiredZoneId,
                    ZoneCenterSearchRadius,
                    out Vector3Int origin,
                    requireReachableFromPlayerStart: true);

                if (ok)
                {
                    placed++;
                    LogPlaced(blueprint.VaultId, origin, VaultPlacementRule.ZoneCenter);
                }
                else
                {
                    LogMandatoryFailure(entry, blueprint, $"no valid anchor near zone center {preferredOrigin}.");
                }
            }

            return placed;
        }

        static int PlaceMandatoryRandomEntries(
            DungeonGenerationContext context,
            VaultAssetRegistry registry,
            List<PreparedEntry> entries)
        {
            int placed = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                PreparedEntry prepared = entries[i];
                DungeonVaultCatalogEntry entry = prepared.CatalogEntry;
                VaultBlueprint blueprint = prepared.Blueprint;

                List<Vector3Int> candidates =
                    VaultPlacementUtility.CollectZoneOriginCandidates(context, entry.requiredZoneId);

                bool ok = VaultPlacer.TryPlaceFromCandidates(
                    blueprint,
                    registry,
                    context,
                    candidates,
                    context.Rng,
                    entry.minDistanceFromPlayerStart,
                    entry.requiredZoneId,
                    out Vector3Int origin);

                if (ok)
                {
                    placed++;
                    LogPlaced(blueprint.VaultId, origin, VaultPlacementRule.MandatoryRandom);
                }
                else
                {
                    LogMandatoryFailure(entry, blueprint, "no valid random anchor in required zone.");
                }
            }

            return placed;
        }

        static int PlaceNearZoneNorthEdgeEntries(
            DungeonGenerationContext context,
            VaultAssetRegistry registry,
            List<PreparedEntry> entries)
        {
            int placed = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                PreparedEntry prepared = entries[i];
                DungeonVaultCatalogEntry entry = prepared.CatalogEntry;
                VaultBlueprint blueprint = prepared.Blueprint;

                List<Vector3Int> candidates = VaultPlacementUtility.CollectNearZoneNorthEdgeCandidates(
                    context,
                    blueprint,
                    entry.requiredZoneId,
                    DescentPlinthPlacementLogic.Floor01NorthMapRow,
                    DescentPlinthPlacementLogic.MaxChebyshevFromNorthEdge);

                bool ok = VaultPlacer.TryPlaceFromCandidates(
                    blueprint,
                    registry,
                    context,
                    candidates,
                    context.Rng,
                    entry.minDistanceFromPlayerStart,
                    entry.requiredZoneId,
                    out Vector3Int origin);

                if (ok)
                {
                    placed++;
                    LogPlaced(blueprint.VaultId, origin, VaultPlacementRule.NearZoneNorthEdge);
                    DescentPlinthPlacementLogic.OnPlaced(context, blueprint, origin);
                }
                else
                {
                    LogMandatoryFailure(
                        entry,
                        blueprint,
                        "no valid anchor near northern zone edge (y=79, Chebyshev<=3).");
                }
            }

            return placed;
        }

        static int PlacePondScatterEntries(
            DungeonGenerationContext context,
            VaultAssetRegistry registry,
            List<PreparedEntry> pondEntries)
        {
            if (pondEntries.Count == 0)
                return 0;

            int targetCount = Floor01PondPlacementLogic.RollPondCount(
                context.RunSeed,
                context.Definition?.FloorId ?? string.Empty);

            System.Random pondRng = ZoneGenerationRng.CreatePopulationRng(
                context.RunSeed,
                (context.Definition?.FloorId ?? string.Empty) + "_pond_vault_pick");

            var shuffled = new List<PreparedEntry>(pondEntries);
            ShuffleEntries(shuffled, pondRng);

            int placed = 0;
            for (int i = 0; i < shuffled.Count && placed < targetCount; i++)
            {
                PreparedEntry prepared = shuffled[i];
                DungeonVaultCatalogEntry entry = prepared.CatalogEntry;
                VaultBlueprint blueprint = prepared.Blueprint;

                List<Vector3Int> candidates =
                    VaultPlacementUtility.CollectZoneOriginCandidates(context, entry.requiredZoneId);

                if (VaultPlacer.TryPlaceFromCandidates(
                        blueprint,
                        registry,
                        context,
                        candidates,
                        context.Rng,
                        entry.minDistanceFromPlayerStart,
                        entry.requiredZoneId,
                        out Vector3Int origin))
                {
                    placed++;
                    LogPlaced(blueprint.VaultId, origin, VaultPlacementRule.PondScatter);
                }
            }

            if (placed < Floor01PondPlacementLogic.MinimumPondCount)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(Phases.VaultPlacementPhase)}: placed {placed}/{targetCount} ponds " +
                    $"(minimum {Floor01PondPlacementLogic.MinimumPondCount}).");
            }
            else
            {
                DungeonGenerationLog.Phase(
                    nameof(Phases.VaultPlacementPhase),
                    $"ponds placed={placed} target={targetCount}");
            }

            return placed;
        }

        static int PlaceRandomEntries(
            DungeonGenerationContext context,
            VaultAssetRegistry registry,
            List<PreparedEntry> entries)
        {
            if (entries.Count == 0)
                return 0;

            var entryOrder = new List<int>();
            for (int i = 0; i < entries.Count; i++)
                entryOrder.Add(i);

            ShuffleIndices(entryOrder, context.Rng);

            var placedCounts = new Dictionary<string, int>();
            int placed = 0;

            for (int orderIndex = 0; orderIndex < entryOrder.Count; orderIndex++)
            {
                PreparedEntry prepared = entries[entryOrder[orderIndex]];
                DungeonVaultCatalogEntry entry = prepared.CatalogEntry;
                VaultBlueprint blueprint = prepared.Blueprint;
                string vaultId = blueprint.VaultId;

                if (!placedCounts.TryGetValue(vaultId, out int count))
                    count = 0;

                if (count >= entry.maxPerFloor)
                    continue;

                if (VaultPlacer.TryPlaceOnce(
                        blueprint,
                        registry,
                        context,
                        context.Rng,
                        entry.minDistanceFromPlayerStart,
                        entry.requiredZoneId,
                        out Vector3Int origin))
                {
                    placedCounts[vaultId] = count + 1;
                    placed++;
                    LogPlaced(vaultId, origin, VaultPlacementRule.Random);
                }
            }

            return placed;
        }

        static void LogPlaced(string vaultId, Vector3Int origin, VaultPlacementRule rule)
        {
            DungeonGenerationLog.Phase(
                nameof(Phases.VaultPlacementPhase),
                $"placed {vaultId} at {origin} ({rule})");
        }

        static void LogMandatoryFailure(
            DungeonVaultCatalogEntry entry,
            VaultBlueprint blueprint,
            string detail)
        {
            if (entry.mandatory)
            {
                DungeonGenerationLog.Error(
                    $"{nameof(Phases.VaultPlacementPhase)}: mandatory vault '{blueprint.VaultId}' failed — {detail}");
            }
            else
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(Phases.VaultPlacementPhase)}: vault '{blueprint.VaultId}' not placed — {detail}");
            }
        }

        static void ShuffleEntries(List<PreparedEntry> entries, System.Random rng)
        {
            for (int i = entries.Count - 1; i > 0; i--)
            {
                int swap = rng.Next(i + 1);
                (entries[i], entries[swap]) = (entries[swap], entries[i]);
            }
        }

        static void ShuffleIndices(List<int> indices, System.Random rng)
        {
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int swap = rng.Next(i + 1);
                (indices[i], indices[swap]) = (indices[swap], indices[i]);
            }
        }
    }
}
