using System;
using System.Collections.Generic;
using JRogue.Spawn;
using JRogue.World.Generation.Vaults;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation
{
    [Serializable]
    public struct ZoneCellMapEntry
    {
        public int x;
        public int y;
        public string zoneId;
    }

    public sealed class DungeonGenerationContext
    {
        public DungeonFloorDefinition Definition { get; }
        public DungeonFloorInstance Instance { get; }
        public int RunSeed { get; }
        public System.Random Rng { get; }
        public Vector3Int PlayerStart { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public HashSet<Vector3Int> ReservedCells { get; } = new HashSet<Vector3Int>();
        public HashSet<Vector3Int> SafeZoneCells { get; } = new HashSet<Vector3Int>();
        public Dictionary<string, PortalArrivalBinding> PortalArrivals { get; } =
            new Dictionary<string, PortalArrivalBinding>();
        public List<PortalInteractable> Portals { get; } = new List<PortalInteractable>();
        public List<ResolvedEdgePortal> ResolvedEdgePortals { get; } = new List<ResolvedEdgePortal>();
        public List<ResolvedPortalPlacement> ResolvedPortals { get; } = new List<ResolvedPortalPlacement>();
        public Dictionary<Vector3Int, string> ZoneCellMap { get; set; }
        public Dictionary<string, RectInt> ZoneBoundsByInstanceId { get; set; }
        public Dictionary<string, RectInt> ZoneBoundsByZoneId { get; set; }
        public ResolvedZonePiece[] ResolvedZonePieces { get; set; }
        public List<ResolvedZoneBoundary> ResolvedZoneBoundaries { get; set; }
        public int ZoneFillAttempt { get; set; }
        public Dictionary<string, ZonePopulationScatterCounts> ZoneScatterCountsByInstance { get; } =
            new Dictionary<string, ZonePopulationScatterCounts>();

        /// <summary>Populated by <see cref="VaultStamper"/> for post-generation diagnostics.</summary>
        public List<VaultPlacementRecord> PlacedVaultRecords { get; } = new List<VaultPlacementRecord>();

        public DungeonGenerationContext(
            DungeonFloorDefinition definition,
            DungeonFloorInstance instance,
            int runSeed,
            int floorSalt)
        {
            Definition = definition;
            Instance = instance;
            RunSeed = runSeed;
            Rng = new System.Random(unchecked(runSeed * 397 ^ floorSalt));
        }

        public bool UsesZoneComposite =>
            Definition != null && Definition.LayoutMode == FloorLayoutMode.ZoneComposite;

        /// <summary>
        /// True when walkable space comes from zone fill (not the legacy layout stamp grid).
        /// </summary>
        public bool UsesPaintedZoneMap =>
            UsesZoneComposite
            || (ZoneCellMap != null && ZoneCellMap.Count > 0);

        public void BuildSafeZoneForFloor(DungeonFloorDefinition def)
        {
            SafeZoneCells.Clear();
            if (def == null)
                return;

            PartyFormationSpawnProfile profile = def.FormationProfile;
            if (profile != null && profile.TryGetOffsetsForCount(1, out Vector3Int[] offsets))
            {
                BuildSafeZone(new[] { PlayerStart + offsets[0] }, def.PlayerSafeRadius);
                return;
            }

            BuildSafeZone(new[] { PlayerStart }, def.PlayerSafeRadius);
        }

        public void BuildSafeZone(IReadOnlyList<Vector3Int> formationCells, int chebyshevRadius)
        {
            SafeZoneCells.Clear();
            if (formationCells == null)
                return;

            for (int i = 0; i < formationCells.Count; i++)
                AddChebyshevDisk(formationCells[i], chebyshevRadius);
        }

        public void AddChebyshevDisk(Vector3Int center, int radius)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) > radius)
                        continue;

                    SafeZoneCells.Add(new Vector3Int(center.x + dx, center.y + dy, 0));
                }
            }
        }

        public bool IsInSafeZone(Vector3Int cell) => SafeZoneCells.Contains(cell);

        public bool TryGetZoneId(Vector3Int cell, out string zoneId)
        {
            zoneId = null;
            if (ZoneCellMap == null)
                return false;

            return ZoneCellMap.TryGetValue(cell, out zoneId);
        }
    }
}
