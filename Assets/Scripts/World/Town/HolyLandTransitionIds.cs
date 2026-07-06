using UnityEngine;

using JRogue.Stats;

namespace JRogue.World.Town
{
    public static class HolyLandTransitionIds
    {
        public const string SquareToNexus = "district_square_to_holy_nexus";
        public const string NexusToSquare = "district_holy_nexus_to_square";
        public const string NexusToHolyLand = "holy_nexus_to_barbarian_holy_land";
        public const string HolyLandToNexus = "barbarian_holy_land_to_nexus";
        public const string NexusToElfHolyLand = "holy_nexus_to_elf_holy_land";
        public const string ElfHolyLandToNexus = "elf_holy_land_to_nexus";
        public const string TentEnter = "building_barbarian_tent_enter";
        public const string TentExit = "building_barbarian_tent_exit";
        public const string ElfHouseEnter = "building_elf_holy_land_house_enter";
        public const string ElfHouseExit = "building_elf_holy_land_house_exit";

        public static bool IsHolyLandAdmission(string portalLinkId) =>
            TryGetHolyLandAdmissionRace(portalLinkId, out _);

        public static bool IsHolyLandExit(string portalLinkId) =>
            TryGetHolyLandExitRace(portalLinkId, out _);

        public static bool IsHolyLandBuildingPortal(string portalLinkId) =>
            portalLinkId == TentEnter
            || portalLinkId == TentExit
            || portalLinkId == ElfHouseEnter
            || portalLinkId == ElfHouseExit;

        public static bool TryGetHolyLandAdmissionRace(string portalLinkId, out Race race)
        {
            if (portalLinkId == NexusToHolyLand)
            {
                race = Race.Barbarian;
                return true;
            }

            if (portalLinkId == NexusToElfHolyLand)
            {
                race = Race.Elf;
                return true;
            }

            race = default;
            return false;
        }

        public static bool TryGetHolyLandExitRace(string portalLinkId, out Race race)
        {
            if (portalLinkId == HolyLandToNexus)
            {
                race = Race.Barbarian;
                return true;
            }

            if (portalLinkId == ElfHolyLandToNexus)
            {
                race = Race.Elf;
                return true;
            }

            race = default;
            return false;
        }

        public static Vector3Int GetNexusParkAnchorForAdmission(string portalLinkId)
        {
            if (portalLinkId == NexusToElfHolyLand)
                return HolyLandNexusLayout.ElfHolyLandReturnAnchor;

            return HolyLandNexusLayout.HolyLandReturnAnchor;
        }

        public static Vector3Int GetNexusReturnAnchorForExit(string portalLinkId)
        {
            if (portalLinkId == ElfHolyLandToNexus)
                return HolyLandNexusLayout.ElfHolyLandNexusArrivalCell;

            if (portalLinkId == HolyLandToNexus)
                return HolyLandNexusLayout.BarbarianHolyLandNexusArrivalCell;

            return GetNexusParkAnchorForAdmission(portalLinkId);
        }

        public static bool TryGetHolyLandBuildingRace(string portalLinkId, out Race race)
        {
            if (portalLinkId == TentEnter || portalLinkId == TentExit)
            {
                race = Race.Barbarian;
                return true;
            }

            if (portalLinkId == ElfHouseEnter || portalLinkId == ElfHouseExit)
            {
                race = Race.Elf;
                return true;
            }

            race = default;
            return false;
        }

        public static Vector3Int GetNexusParkAnchorForRace(Race race) =>
            race == Race.Elf
                ? HolyLandNexusLayout.ElfHolyLandReturnAnchor
                : HolyLandNexusLayout.HolyLandReturnAnchor;
    }
}
