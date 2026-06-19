using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>Authoring constants for the TownTest demo stone building (exterior + interior).</summary>
    public static class TownDemoBuildingLayout
    {
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

        public static readonly Vector3Int InteriorArrivalCell = new Vector3Int(2, 1, 0);
        public static readonly Vector3Int InteriorExitCell = new Vector3Int(2, 3, 0);
        public static readonly Vector3Int InteriorNpcCell = new Vector3Int(2, 2, 0);
    }
}
