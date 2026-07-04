namespace JRogue.World.Town
{
    public static class HolyLandTransitionIds
    {
        public const string SquareToNexus = "district_square_to_holy_nexus";
        public const string NexusToSquare = "district_holy_nexus_to_square";
        public const string NexusToHolyLand = "holy_nexus_to_barbarian_holy_land";
        public const string HolyLandToNexus = "barbarian_holy_land_to_nexus";
        public const string TentEnter = "building_barbarian_tent_enter";
        public const string TentExit = "building_barbarian_tent_exit";

        public static bool IsHolyLandAdmission(string portalLinkId) =>
            portalLinkId == NexusToHolyLand;

        public static bool IsHolyLandExit(string portalLinkId) =>
            portalLinkId == HolyLandToNexus;

        public static bool IsTentPortal(string portalLinkId) =>
            portalLinkId == TentEnter || portalLinkId == TentExit;
    }
}
