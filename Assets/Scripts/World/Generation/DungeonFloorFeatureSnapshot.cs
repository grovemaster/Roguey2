using System;
using System.Collections.Generic;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Traps;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Per-floor runtime feature state captured when parking so singleton services can switch floors.
    /// </summary>
    [Serializable]
    public sealed class DungeonFloorFeatureSnapshot
    {
        public List<HazardSnapshotEntry> hazards = new List<HazardSnapshotEntry>();
        public List<TrapSnapshotEntry> traps = new List<TrapSnapshotEntry>();
        public List<InteractableSnapshotEntry> interactables = new List<InteractableSnapshotEntry>();

        public void Clear()
        {
            hazards.Clear();
            traps.Clear();
            interactables.Clear();
        }
    }

    [Serializable]
    public struct HazardSnapshotEntry
    {
        public Vector3Int cell;
        public EnvironmentalHazardDefinition definition;
        public bool isRevealed;
    }

    [Serializable]
    public struct TrapSnapshotEntry
    {
        public Vector3Int hostCell;
        public TrapDefinition definition;
        public bool hasTriggered;
        public bool isDetected;
        public int chargesRemaining;
    }

    [Serializable]
    public struct InteractableSnapshotEntry
    {
        public Vector3Int cell;
        public InteractableTileDefinition definition;
        public bool isOn;
    }
}
