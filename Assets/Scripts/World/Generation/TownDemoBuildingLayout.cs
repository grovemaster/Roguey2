using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>Authoring constants for the TownTest demo stone building (exterior + interior).</summary>
    public static class TownDemoBuildingLayout
    {
        public const int InteriorSize = 8;

        public const int ExteriorOriginX = 11;
        public const int ExteriorOriginY = 8;
        public const int ExteriorWidth = 7;
        public const int ExteriorDepth = 4;
        public const int ExteriorDoorLocalX = 3;
        public const int ExteriorDoorLocalY = 0;

        public static readonly Vector3Int ExteriorDoorCell = new Vector3Int(
            ExteriorOriginX + ExteriorDoorLocalX,
            ExteriorOriginY + ExteriorDoorLocalY,
            0);

        /// <summary>Center room — room for default 4-member south stack (y-1 per member).</summary>
        public static readonly Vector3Int InteriorArrivalCell = new Vector3Int(4, 4, 0);

        /// <summary>South perimeter doorway back to the plaza.</summary>
        public static readonly Vector3Int InteriorExitCell = new Vector3Int(4, 0, 0);

        public static readonly Vector3Int InteriorNpcCell = new Vector3Int(4, 6, 0);
    }
}
