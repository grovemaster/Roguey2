using System;
using System.Collections.Generic;
using JRogue.Spawn;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation
{
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
        [SerializeField] List<DungeonPortalSpec> portals = new List<DungeonPortalSpec>();
        [SerializeField] List<PortalArrivalBinding> arrivalBindings = new List<PortalArrivalBinding>();

        public string FloorId => floorId;
        public DungeonLayoutStamp LayoutStamp => layoutStamp;
        public TileBase FloorTile => floorTile;
        public TileBase WallTile => wallTile;
        public int PlayerSafeRadius => playerSafeRadius;
        public PartyFormationSpawnProfile FormationProfile => formationProfile;
        public IReadOnlyList<EnemyPopulationEntry> EnemyPopulation => enemyPopulation;
        public IReadOnlyList<DungeonPortalSpec> Portals => portals;
        public IReadOnlyList<PortalArrivalBinding> ArrivalBindings => arrivalBindings;

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
    }

    [Serializable]
    public struct EnemyPopulationEntry
    {
        public EnemySpawnDefinition spawnDefinition;
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
    public struct PortalArrivalBinding
    {
        public string portalLinkId;
        public Vector3Int arrivalAnchor;
    }
}
