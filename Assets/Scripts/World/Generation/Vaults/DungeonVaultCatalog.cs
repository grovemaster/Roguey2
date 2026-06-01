using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Vaults
{
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
    }
}
