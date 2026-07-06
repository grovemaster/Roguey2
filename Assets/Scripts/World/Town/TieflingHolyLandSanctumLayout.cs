using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Tiefling sanctum interior — same 8×8 shell as the barbarian shaman tent.</summary>
    public static class TieflingHolyLandSanctumLayout
    {
        public const string ForgemasterMarkerId = "tiefling_holy_land_forgemaster";

        public static readonly Vector3Int InteriorArrivalCell = BarbarianShamanTentLayout.InteriorArrivalCell;
        public static readonly Vector3Int InteriorExitCell = BarbarianShamanTentLayout.InteriorExitCell;
        public static readonly Vector3Int ForgemasterNpcCell = BarbarianShamanTentLayout.ShamanNpcCell;
        public static readonly Vector3Int ExteriorReturnCell = TieflingHolyLandLayout.SanctumDoorCell;
    }
}
