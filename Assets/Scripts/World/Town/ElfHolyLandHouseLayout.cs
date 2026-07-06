using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Elf grove house interior — same 8×8 shell as the barbarian shaman tent.</summary>
    public static class ElfHolyLandHouseLayout
    {
        public const string FairyMerchantMarkerId = "elf_grove_fairy_merchant";

        public static readonly Vector3Int InteriorArrivalCell = BarbarianShamanTentLayout.InteriorArrivalCell;
        public static readonly Vector3Int InteriorExitCell = BarbarianShamanTentLayout.InteriorExitCell;
        public static readonly Vector3Int FairyMerchantNpcCell = BarbarianShamanTentLayout.ShamanNpcCell;
        public static readonly Vector3Int ExteriorReturnCell = ElfHolyLandLayout.HouseDoorCell;
    }
}
