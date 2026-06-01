using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.World.Generation.Vaults;
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
                $"begin entries={catalog.Entries.Count} playerStart={context.PlayerStart}");

            VaultAssetRegistry registry = catalog.AssetRegistry;
            if (registry == null)
            {
                DungeonGenerationLog.Warn($"{nameof(VaultPlacementPhase)}: missing VaultAssetRegistry on catalog.");
                return;
            }

            registry.RebuildLookups();

            var entryOrder = new List<int>();
            for (int i = 0; i < catalog.Entries.Count; i++)
                entryOrder.Add(i);

            ShuffleIndices(entryOrder, context.Rng);

            var placedCounts = new Dictionary<string, int>();
            int placedVaults = 0;

            for (int orderIndex = 0; orderIndex < entryOrder.Count; orderIndex++)
            {
                DungeonVaultCatalogEntry entry = catalog.Entries[entryOrder[orderIndex]];
                if (!VaultSourceText.TryRead(entry, out string vaultText, out string readError))
                {
                    DungeonGenerationLog.Warn(
                        $"{nameof(VaultPlacementPhase)}: cannot read vault '{entry?.vaultId}': {readError}");
                    continue;
                }

                if (!VaultFileParser.TryParse(vaultText, out VaultBlueprint blueprint, out string parseError))
                {
                    DungeonGenerationLog.Warn(
                        $"{nameof(VaultPlacementPhase)}: parse failed for '{entry.vaultId}': {parseError}");
                    continue;
                }

                string vaultId = string.IsNullOrEmpty(entry.vaultId) ? blueprint.VaultId : entry.vaultId;
                if (string.IsNullOrEmpty(vaultId))
                {
                    DungeonGenerationLog.Warn(
                        $"{nameof(VaultPlacementPhase)}: missing vault id for catalog entry.");
                    continue;
                }

                blueprint.VaultId = vaultId;

                if (!placedCounts.TryGetValue(vaultId, out int count))
                    count = 0;

                if (count >= entry.maxPerFloor)
                    continue;

                if (entry.weight <= 0)
                    continue;

                if (VaultPlacer.TryPlaceOnce(
                        blueprint,
                        registry,
                        context,
                        context.Rng,
                        entry.minDistanceFromPlayerStart,
                        out Vector3Int origin))
                {
                    placedCounts[vaultId] = count + 1;
                    placedVaults++;
                    DungeonGenerationLog.Phase(
                        nameof(VaultPlacementPhase),
                        $"placed {vaultId} at {origin}");
                }
                else
                {
                    int minDist = entry.minDistanceFromPlayerStart > 0
                        ? entry.minDistanceFromPlayerStart
                        : blueprint.MinDistanceFromPlayerStart;
                    int validAnchors = VaultPlacementUtility.CountValidAnchors(
                        blueprint,
                        context,
                        MapManager.Instance,
                        minDist);

                    DungeonGenerationLog.Warn(
                        $"{nameof(VaultPlacementPhase)}: no valid anchor for '{vaultId}' on '{def.FloorId}' " +
                        $"(validAnchors={validAnchors}, minDistance={minDist}, playerStart={context.PlayerStart}).");
                }
            }

            DungeonGenerationLog.Phase(nameof(VaultPlacementPhase), $"placedCount={placedVaults}");
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
