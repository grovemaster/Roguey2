using UnityEngine;

namespace JRogue.World.Town
{
    public static class HolyLandFloorIds
    {
        public const string Nexus = "holy_land_nexus";
        public const string HolyLandProper = "barbarian_holy_land";
        public const string ShamanTentInterior = "barbarian_shaman_tent_interior";
        public const string ElfHolyLandProper = "elf_holy_land";
        public const string ElfHouseInterior = "elf_holy_land_house_interior";

        public static bool IsRacialHolyLandProper(string floorId) =>
            floorId == HolyLandProper || floorId == ElfHolyLandProper;

        public static bool IsRacialHolyLandFloor(string floorId) =>
            IsRacialHolyLandProper(floorId)
            || floorId == ShamanTentInterior
            || floorId == ElfHouseInterior;

        public static bool TryGetNexusParkAnchorForRacialFloor(string floorId, out Vector3Int anchor)
        {
            if (floorId == ElfHolyLandProper || floorId == ElfHouseInterior)
            {
                anchor = HolyLandNexusLayout.ElfHolyLandReturnAnchor;
                return true;
            }

            if (floorId == HolyLandProper || floorId == ShamanTentInterior)
            {
                anchor = HolyLandNexusLayout.HolyLandReturnAnchor;
                return true;
            }

            anchor = default;
            return false;
        }
    }
}
