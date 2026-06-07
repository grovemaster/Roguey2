using System;
using UnityEngine;

namespace JRogue.World.Generation
{
    public enum PortalPlacementRuleKind
    {
        OrthogonalMapEdge = 0,
        FixedStampMarker = 1,
        TaggedRegionEdge = 2,
    }

    public enum TaggedRegionPortalMetric
    {
        MaxManhattanFromStart = 0,
        MaxY = 1,
        MaxX = 2,
        MinY = 3,
        MinX = 4,
    }

    [Serializable]
    public struct PortalPlacementRule
    {
        public PortalPlacementRuleKind kind;
        [Tooltip("Shared: stable id pairing exits across floors.")]
        public string portalLinkId;
        public string targetFloorId;
        public string listLabel;

        [Header("OrthogonalMapEdge")]
        public MapEdge edge;
        [Min(1)] public int insetFromEdge;

        [Header("FixedStampMarker")]
        [Tooltip("Reads exact cell from layoutStamp; falls back to portalCell when empty.")]
        public string portalMarkerId;
        public Vector3Int portalCell;

        [Header("TaggedRegionEdge")]
        [Tooltip("Zone-composite floors: filter walkable cells by ZoneCellMap zone id.")]
        public string zoneId;
        [Tooltip("Reserved for stamp region tags when region authoring exists.")]
        public string regionTag;
        public TaggedRegionPortalMetric metric;
        [Min(0)] public int minChebyshevFromStart;
    }

    public struct ResolvedPortalPlacement
    {
        public Vector3Int cell;
        public string portalLinkId;
        public string targetFloorId;
        public string listLabel;
        public PortalPlacementRuleKind sourceKind;
        public MapEdge edge;
    }

}
