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
        public const string NexusToBeastmanHolyLand = "holy_nexus_to_beastman_holy_land";
        public const string BeastmanHolyLandToNexus = "beastman_holy_land_to_nexus";
        public const string NexusToTieflingHolyLand = "holy_nexus_to_tiefling_holy_land";
        public const string TieflingHolyLandToNexus = "tiefling_holy_land_to_nexus";
        public const string TentEnter = "building_barbarian_tent_enter";
        public const string TentExit = "building_barbarian_tent_exit";
        public const string ElfHouseEnter = "building_elf_holy_land_house_enter";
        public const string ElfHouseExit = "building_elf_holy_land_house_exit";
        public const string BeastmanDenEnter = "building_beastman_den_enter";
        public const string BeastmanDenExit = "building_beastman_den_exit";
        public const string TieflingSanctumEnter = "building_tiefling_sanctum_enter";
        public const string TieflingSanctumExit = "building_tiefling_sanctum_exit";

        public static bool IsHolyLandAdmission(string portalLinkId) =>
            TryGetHolyLandAdmissionRace(portalLinkId, out _);

        public static bool IsHolyLandExit(string portalLinkId) =>
            TryGetHolyLandExitRace(portalLinkId, out _);

        public static bool IsHolyLandBuildingPortal(string portalLinkId) =>
            portalLinkId == TentEnter
            || portalLinkId == TentExit
            ||             portalLinkId == ElfHouseEnter
            || portalLinkId == ElfHouseExit
            || portalLinkId == BeastmanDenEnter
            || portalLinkId == BeastmanDenExit
            || portalLinkId == TieflingSanctumEnter
            || portalLinkId == TieflingSanctumExit;

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

            if (portalLinkId == NexusToBeastmanHolyLand)
            {
                race = Race.Beastman;
                return true;
            }

            if (portalLinkId == NexusToTieflingHolyLand)
            {
                race = Race.Tiefling;
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

            if (portalLinkId == BeastmanHolyLandToNexus)
            {
                race = Race.Beastman;
                return true;
            }

            if (portalLinkId == TieflingHolyLandToNexus)
            {
                race = Race.Tiefling;
                return true;
            }

            race = default;
            return false;
        }

        public static Vector3Int GetNexusParkAnchorForAdmission(string portalLinkId)
        {
            if (portalLinkId == NexusToBeastmanHolyLand)
                return HolyLandNexusLayout.BeastmanHolyLandReturnAnchor;

            if (portalLinkId == NexusToTieflingHolyLand)
                return HolyLandNexusLayout.TieflingHolyLandReturnAnchor;

            if (portalLinkId == NexusToElfHolyLand)
                return HolyLandNexusLayout.ElfHolyLandReturnAnchor;

            return HolyLandNexusLayout.HolyLandReturnAnchor;
        }

        public static Vector3Int GetNexusReturnAnchorForExit(string portalLinkId)
        {
            if (portalLinkId == BeastmanHolyLandToNexus)
                return HolyLandNexusLayout.BeastmanHolyLandNexusArrivalCell;

            if (portalLinkId == TieflingHolyLandToNexus)
                return HolyLandNexusLayout.TieflingHolyLandNexusArrivalCell;

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

            if (portalLinkId == BeastmanDenEnter || portalLinkId == BeastmanDenExit)
            {
                race = Race.Beastman;
                return true;
            }

            if (portalLinkId == TieflingSanctumEnter || portalLinkId == TieflingSanctumExit)
            {
                race = Race.Tiefling;
                return true;
            }

            race = default;
            return false;
        }

        public static Vector3Int GetNexusParkAnchorForRace(Race race)
        {
            if (race == Race.Beastman)
                return HolyLandNexusLayout.BeastmanHolyLandReturnAnchor;

            if (race == Race.Tiefling)
                return HolyLandNexusLayout.TieflingHolyLandReturnAnchor;

            if (race == Race.Elf)
                return HolyLandNexusLayout.ElfHolyLandReturnAnchor;

            return HolyLandNexusLayout.HolyLandReturnAnchor;
        }
    }
}
