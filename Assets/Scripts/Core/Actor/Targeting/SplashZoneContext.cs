using JRogue.Core.Actor;
using UnityEngine;

namespace JRogue.Core.Targeting
{
    public readonly struct SplashZoneContext
    {
        public Vector3Int CasterCell { get; }
        public Vector3Int PrimaryTile { get; }
        public FacingDirection CasterFacing { get; }

        public SplashZoneContext(Vector3Int casterCell, Vector3Int primaryTile, FacingDirection casterFacing)
        {
            CasterCell = casterCell;
            PrimaryTile = primaryTile;
            CasterFacing = casterFacing;
        }
    }
}
