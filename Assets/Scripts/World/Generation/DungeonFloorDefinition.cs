using System;
using System.Collections.Generic;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Spawn;
using JRogue.Traps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation
{
    public enum DungeonDoorPolicy
    {
        None,
        Procedural,
        VaultOnly,
        StampOnly,
    }

    [CreateAssetMenu(fileName = "DungeonFloorDefinition", menuName = "JRogue/World/Dungeon Floor Definition")]
    public sealed class DungeonFloorDefinition : ScriptableObject
    {
        [SerializeField] string floorId = "dungeon_floor_01";
        [SerializeField] DungeonLayoutStamp layoutStamp;
        [SerializeField] TileBase floorTile;
        [SerializeField] TileBase wallTile;
        [SerializeField] int playerSafeRadius = 5;
        [SerializeField] PartyFormationSpawnProfile formationProfile;
        [SerializeField] EnemyPopulationEntry[] enemyPopulation = Array.Empty<EnemyPopulationEntry>();
        [SerializeField] HazardPopulationEntry[] hazardPopulation = Array.Empty<HazardPopulationEntry>();
        [SerializeField] TrapPopulationEntry[] trapPopulation = Array.Empty<TrapPopulationEntry>();
        [SerializeField] InteractablePopulationEntry[] interactablePopulation = Array.Empty<InteractablePopulationEntry>();
        [SerializeField] List<DungeonPortalSpec> portals = new List<DungeonPortalSpec>();
        [SerializeField] List<EdgePortalSpec> edgePortals = new List<EdgePortalSpec>();
        [SerializeField] List<PortalArrivalBinding> arrivalBindings = new List<PortalArrivalBinding>();
        [Header("Portal heuristics (v0b)")]
        [SerializeField] int orthogonalEdgePortalCount;
        [SerializeField] int orthogonalEdgeInset = 2;
        [Header("Future / v0b+")]
        [SerializeField] DungeonDoorPolicy doorPolicy = DungeonDoorPolicy.None;
        [SerializeField] List<DungeonVaultReference> vaults = new List<DungeonVaultReference>();

        public string FloorId => floorId;
        public DungeonLayoutStamp LayoutStamp => layoutStamp;
        public TileBase FloorTile => floorTile;
        public TileBase WallTile => wallTile;
        public int PlayerSafeRadius => playerSafeRadius;
        public PartyFormationSpawnProfile FormationProfile => formationProfile;
        public IReadOnlyList<EnemyPopulationEntry> EnemyPopulation => enemyPopulation;
        public IReadOnlyList<HazardPopulationEntry> HazardPopulation => hazardPopulation;
        public IReadOnlyList<TrapPopulationEntry> TrapPopulation => trapPopulation;
        public IReadOnlyList<InteractablePopulationEntry> InteractablePopulation => interactablePopulation;
        public IReadOnlyList<DungeonPortalSpec> Portals => portals;
        public IReadOnlyList<EdgePortalSpec> EdgePortals => edgePortals;
        public IReadOnlyList<PortalArrivalBinding> ArrivalBindings => arrivalBindings;
        public int OrthogonalEdgePortalCount => orthogonalEdgePortalCount;
        public int OrthogonalEdgeInset => orthogonalEdgeInset;
        public DungeonDoorPolicy DoorPolicy => doorPolicy;
        public IReadOnlyList<DungeonVaultReference> Vaults => vaults;

        public bool TryGetArrivalBinding(string portalLinkId, out PortalArrivalBinding binding)
        {
            binding = default;
            if (string.IsNullOrEmpty(portalLinkId) || arrivalBindings == null)
                return false;

            for (int i = 0; i < arrivalBindings.Count; i++)
            {
                PortalArrivalBinding candidate = arrivalBindings[i];
                if (candidate.portalLinkId != portalLinkId)
                    continue;

                binding = candidate;
                return true;
            }

            return false;
        }

        public bool TryGetEdgePortalSpec(MapEdge edge, out EdgePortalSpec spec)
        {
            spec = default;
            if (edgePortals == null)
                return false;

            for (int i = 0; i < edgePortals.Count; i++)
            {
                EdgePortalSpec candidate = edgePortals[i];
                if (candidate.edge != edge)
                    continue;

                spec = candidate;
                return true;
            }

            return false;
        }
    }

    [Serializable]
    public struct EnemyPopulationEntry
    {
        public EnemySpawnDefinition spawnDefinition;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
    }

    [Serializable]
    public struct HazardPopulationEntry
    {
        public EnvironmentalHazardDefinition definition;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        public bool startHidden;
    }

    [Serializable]
    public struct TrapPopulationEntry
    {
        public TrapDefinition definition;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
    }

    [Serializable]
    public struct InteractablePopulationEntry
    {
        public InteractableTileDefinition definition;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
    }

    [Serializable]
    public struct DungeonPortalSpec
    {
        public string portalLinkId;
        public string targetFloorId;
        [Tooltip("Stamp marker id or leave empty to use portalCell.")]
        public string portalMarkerId;
        public Vector3Int portalCell;
        public string listLabel;
    }

    [Serializable]
    public struct EdgePortalSpec
    {
        public MapEdge edge;
        public string portalLinkId;
        public string targetFloorId;
        public string listLabel;
    }

    [Serializable]
    public struct PortalArrivalBinding
    {
        public string portalLinkId;
        public Vector3Int arrivalAnchor;
    }

    [Serializable]
    public struct DungeonVaultReference
    {
        public string vaultId;
        [Min(0)] public int weight;
    }
}
