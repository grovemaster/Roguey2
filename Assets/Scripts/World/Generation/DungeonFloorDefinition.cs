using System;
using System.Collections.Generic;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Item;
using JRogue.Spawn;
using JRogue.Traps;
using JRogue.World.Generation.Vaults;
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

    /// <summary>Runtime combat rules for a floor instance (distinct from spawn exclusion radius).</summary>
    public enum FloorCombatPolicy
    {
        Normal = 0,
        SafeZone = 1,
    }

    [Serializable]
    public struct SafeZoneRegion
    {
        public string regionId;
        public Vector2Int minInclusive;
        public Vector2Int maxInclusive;
        public FloorCombatPolicy policy;

        public bool Contains(Vector3Int cell) =>
            cell.x >= minInclusive.x && cell.x <= maxInclusive.x
            && cell.y >= minInclusive.y && cell.y <= maxInclusive.y;

        public int Area =>
            Mathf.Max(0, maxInclusive.x - minInclusive.x + 1)
            * Mathf.Max(0, maxInclusive.y - minInclusive.y + 1);
    }

    [CreateAssetMenu(fileName = "DungeonFloorDefinition", menuName = "JRogue/World/Dungeon Floor Definition")]
    public sealed class DungeonFloorDefinition : ScriptableObject
    {
        [SerializeField] string floorId = "dungeon_floor_01";
        [SerializeField] FloorLayoutMode layoutMode = FloorLayoutMode.PreBakedStamp;
        [SerializeField] DungeonLayoutStamp layoutStamp;
        [SerializeField] Zones.DungeonFloorZoneLayout zoneLayout;
        [SerializeField] TileBase floorTile;
        [SerializeField] TileBase wallTile;
        [SerializeField] int playerSafeRadius = 5;
        [SerializeField] PartyFormationSpawnProfile formationProfile;
        [SerializeField] EnemyPopulationEntry[] enemyPopulation = Array.Empty<EnemyPopulationEntry>();
        [SerializeField] HazardPopulationEntry[] hazardPopulation = Array.Empty<HazardPopulationEntry>();
        [SerializeField] TrapPopulationEntry[] trapPopulation = Array.Empty<TrapPopulationEntry>();
        [SerializeField] InteractablePopulationEntry[] interactablePopulation = Array.Empty<InteractablePopulationEntry>();
        [SerializeField] FloorItemPopulationEntry[] floorItemPopulation = Array.Empty<FloorItemPopulationEntry>();
        [SerializeField] bool useFloorPopulationAsFallback = true;
        [SerializeField] List<DungeonPortalSpec> portals = new List<DungeonPortalSpec>();
        [SerializeField] List<EdgePortalSpec> edgePortals = new List<EdgePortalSpec>();
        [SerializeField] List<PortalArrivalBinding> arrivalBindings = new List<PortalArrivalBinding>();
        [Header("Portal heuristics (v0b)")]
        [SerializeField] int orthogonalEdgePortalCount;
        [SerializeField] int orthogonalEdgeInset = 2;
        [Header("Dungeon time (StGaaB-style)")]
        [SerializeField] bool participatesInDungeonTime = true;
        [Min(1)] [SerializeField] int baseDayNightCycles = 7;
        [Min(0)] [SerializeField] int additionalDayNightCycles;
        [Min(1)] [SerializeField] int playerTurnsPerDay = 5;
        [Min(1)] [SerializeField] int playerTurnsPerNight = 5;
        [Header("Gameplay safe zone")]
        [SerializeField] FloorCombatPolicy combatPolicy = FloorCombatPolicy.Normal;
        [SerializeField] SafeZoneRegion[] safeZoneRegions = Array.Empty<SafeZoneRegion>();

        [Header("Future / v0b+")]
        [SerializeField] DungeonDoorPolicy doorPolicy = DungeonDoorPolicy.None;
        [SerializeField] List<DungeonVaultReference> vaults = new List<DungeonVaultReference>();
        [SerializeField] DungeonVaultCatalog vaultCatalog;

        public string FloorId => floorId;
        public FloorLayoutMode LayoutMode => layoutMode;
        public DungeonLayoutStamp LayoutStamp => layoutStamp;
        public Zones.DungeonFloorZoneLayout ZoneLayout => zoneLayout;
        public TileBase FloorTile => floorTile;
        public TileBase WallTile => wallTile;
        public int PlayerSafeRadius => playerSafeRadius;
        public PartyFormationSpawnProfile FormationProfile => formationProfile;
        public IReadOnlyList<EnemyPopulationEntry> EnemyPopulation => enemyPopulation;
        public IReadOnlyList<HazardPopulationEntry> HazardPopulation => hazardPopulation;
        public IReadOnlyList<TrapPopulationEntry> TrapPopulation => trapPopulation;
        public IReadOnlyList<InteractablePopulationEntry> InteractablePopulation => interactablePopulation;
        public IReadOnlyList<FloorItemPopulationEntry> FloorItemPopulation => floorItemPopulation;
        public bool UseFloorPopulationAsFallback => useFloorPopulationAsFallback;
        public IReadOnlyList<DungeonPortalSpec> Portals => portals;
        public IReadOnlyList<EdgePortalSpec> EdgePortals => edgePortals;
        public IReadOnlyList<PortalArrivalBinding> ArrivalBindings => arrivalBindings;
        public int OrthogonalEdgePortalCount => orthogonalEdgePortalCount;
        public int OrthogonalEdgeInset => orthogonalEdgeInset;
        public DungeonDoorPolicy DoorPolicy => doorPolicy;
        public IReadOnlyList<DungeonVaultReference> Vaults => vaults;
        public DungeonVaultCatalog VaultCatalog => vaultCatalog;
        public FloorCombatPolicy CombatPolicy => combatPolicy;
        public IReadOnlyList<SafeZoneRegion> SafeZoneRegions => safeZoneRegions;
        public bool ParticipatesInDungeonTime => participatesInDungeonTime;
        public int BaseDayNightCycles => baseDayNightCycles;
        public int AdditionalDayNightCycles => additionalDayNightCycles;
        public int PlayerTurnsPerDay => playerTurnsPerDay;
        public int PlayerTurnsPerNight => playerTurnsPerNight;

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
    public struct FloorItemPopulationEntry
    {
        public ItemData itemData;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        [Min(1)] public int minQuantity;
        [Min(1)] public int maxQuantity;
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
