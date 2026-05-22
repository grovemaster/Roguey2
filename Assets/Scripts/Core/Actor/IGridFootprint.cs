using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Core.Actor
{
    /// <summary>
    /// Multi-cell grid occupancy for a single <see cref="IBattleTarget"/> owner.
    /// </summary>
    public interface IGridFootprint
    {
        Vector3Int GridPosition { get; }
        FootprintLayout Layout { get; }
        int FootprintWidth { get; }
        int FootprintHeight { get; }
        FacingDirection Facing { get; }

        void GetOccupiedCells(List<Vector3Int> buffer);
        bool Occupies(Vector3Int cell);
    }
}
