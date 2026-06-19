using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation
{
    public enum TownFacadePaintLayer
    {
        Wall = 0,
        Floor = 1,
        /// <summary>Opaque floor-tile fill inside the facade footprint; blocks movement.</summary>
        InteriorMass = 2,
    }

    [Serializable]
    public struct TownFacadePaintCell
    {
        public Vector3Int cell;
        public TileBase tile;
        public TownFacadePaintLayer layer;
    }

    /// <summary>
    /// Per-cell tile overrides for town building facades (stone walls, roofs, doors).</summary>
    [CreateAssetMenu(fileName = "TownBuildingFacadeOverlay", menuName = "JRogue/World/Town Building Facade Overlay")]
    public sealed class TownBuildingFacadeOverlay : ScriptableObject
    {
        [SerializeField] string floorId;
        [SerializeField] TownFacadePaintCell[] cells = Array.Empty<TownFacadePaintCell>();

        public string FloorId => floorId;
        public TownFacadePaintCell[] Cells => cells;

        public void Configure(string id, TownFacadePaintCell[] paintCells)
        {
            floorId = id;
            cells = paintCells ?? Array.Empty<TownFacadePaintCell>();
        }
    }
}
