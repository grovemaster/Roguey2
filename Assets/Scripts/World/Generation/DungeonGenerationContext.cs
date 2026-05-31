using System;
using System.Collections.Generic;
using JRogue.Spawn;
using UnityEngine;

namespace JRogue.World.Generation
{
    public sealed class DungeonGenerationContext
    {
        public DungeonFloorDefinition Definition { get; }
        public DungeonFloorInstance Instance { get; }
        public System.Random Rng { get; }
        public Vector3Int PlayerStart { get; set; }
        public HashSet<Vector3Int> ReservedCells { get; } = new HashSet<Vector3Int>();
        public HashSet<Vector3Int> SafeZoneCells { get; } = new HashSet<Vector3Int>();
        public Dictionary<string, PortalArrivalBinding> PortalArrivals { get; } =
            new Dictionary<string, PortalArrivalBinding>();
        public List<PortalInteractable> Portals { get; } = new List<PortalInteractable>();

        public DungeonGenerationContext(
            DungeonFloorDefinition definition,
            DungeonFloorInstance instance,
            int runSeed,
            int floorSalt)
        {
            Definition = definition;
            Instance = instance;
            Rng = new System.Random(unchecked(runSeed * 397 ^ floorSalt));
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
    }
}
