using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Beastman den interior — same 8×8 shell as the barbarian shaman tent.</summary>
    public static class BeastmanHolyLandDenLayout
    {
        public const string BeastBloodMerchantMarkerId = "beastman_den_beast_blood_merchant";

        public static readonly Vector3Int InteriorArrivalCell = BarbarianShamanTentLayout.InteriorArrivalCell;
        public static readonly Vector3Int InteriorExitCell = BarbarianShamanTentLayout.InteriorExitCell;
        public static readonly Vector3Int BeastBloodMerchantNpcCell = BarbarianShamanTentLayout.ShamanNpcCell;
        public static readonly Vector3Int ExteriorReturnCell = BeastmanHolyLandLayout.DenDoorCell;
    }
}
