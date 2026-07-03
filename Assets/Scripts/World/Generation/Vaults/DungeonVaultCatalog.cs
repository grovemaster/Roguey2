using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Vaults
{
    /// <summary>How a catalog entry is anchored during <see cref="Phases.VaultPlacementPhase"/>.</summary>
    public enum VaultPlacementRule
    {
        Random = 0,
        ZoneCenter = 1,
        MandatoryRandom = 2,
        PondScatter = 3,
    }

    [CreateAssetMenu(fileName = "Floor1_VaultCatalog", menuName = "JRogue/World/Dungeon Vault Catalog")]
    public sealed class DungeonVaultCatalog : ScriptableObject
    {
        [SerializeField] VaultAssetRegistry assetRegistry;
        [SerializeField] List<DungeonVaultCatalogEntry> entries = new List<DungeonVaultCatalogEntry>();

        public VaultAssetRegistry AssetRegistry => assetRegistry;
        public IReadOnlyList<DungeonVaultCatalogEntry> Entries => entries;
    }

    [Serializable]
    public sealed class DungeonVaultCatalogEntry
    {
        [Tooltip("Optional override; when empty, parsed from the .vault file VAULT header.")]
        public string vaultId;

        public TextAsset sourceFile;

        [Tooltip("Fallback under Assets/ when sourceFile is missing (e.g. Data/Vaults/Floor1/vault_shrine_5x5.vault).")]
        public string sourceAssetPath;

        [Min(0)] public int weight = 1;
        [Min(1)] public int maxPerFloor = 1;

        [Tooltip("0 = use MIN_DISTANCE_FROM_PLAYER_START from the .vault file.")]
        [Min(0)] public int minDistanceFromPlayerStart;

        [Tooltip("When set, every vault footprint cell must lie in this habitat zone id.")]
        public string requiredZoneId;

        public VaultPlacementRule placementRule = VaultPlacementRule.Random;

        [Tooltip("When true, placement failure emits an error (monument, altar).")]
        public bool mandatory;
    }

    /// <summary>Floor 1 production pond count rules (§7.7).</summary>
    public static class Floor01PondPlacementLogic
    {
        public const int MinimumPondCount = 2;
        public const int TypicalMaxPondCount = 5;
        public const int HardCapPondCount = 8;
        public const float OverflowChance = 0.15f;

        public static int RollPondCount(int runSeed, string floorId)
        {
            System.Random rng = Zones.ZoneGenerationRng.CreatePopulationRng(runSeed, floorId + "_pond_vaults");
            return RollPondCount(rng);
        }

        public static int RollPondCount(System.Random rng)
        {
            if (rng == null)
                return MinimumPondCount;

            if (rng.NextDouble() < OverflowChance)
                return rng.Next(6, HardCapPondCount + 1);

            return rng.Next(MinimumPondCount, TypicalMaxPondCount + 1);
        }
    }
}
