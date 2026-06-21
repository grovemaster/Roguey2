using JRogue.World.Generation.Zones;

namespace JRogue.World.Lighting
{
    /// <summary>
    /// Zone-aware vision rules for pitch-dark areas (e.g. Floor 1 Northern Dark).
    /// </summary>
    public static class ZoneVisionPolicy
    {
        public const string RequiresPersonalLightTag = "requires_personal_light";
        public const string NorthernDarkZoneId = "northern_dark";

        public static bool ZoneRequiresPersonalLightForVision(string zoneId, DungeonFloorZoneLayout layout)
        {
            if (string.IsNullOrEmpty(zoneId))
                return false;

            if (zoneId == NorthernDarkZoneId)
                return true;

            if (layout == null || !layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition definition))
                return false;

            return HasTag(definition.Tags, RequiresPersonalLightTag);
        }

        public static bool MemberNavigatesBlind(
            string zoneId,
            DungeonFloorZoneLayout layout,
            bool hasPersonalVisionLight) =>
            !hasPersonalVisionLight && ZoneRequiresPersonalLightForVision(zoneId, layout);

        public static bool ShouldSuppressFogMemory(
            string zoneId,
            DungeonFloorZoneLayout layout,
            bool partyHasPersonalVisionLight) =>
            !partyHasPersonalVisionLight && ZoneRequiresPersonalLightForVision(zoneId, layout);

        public static bool IsPitchDarkForVision(
            string zoneId,
            int emitLight,
            int receivedLight,
            DungeonFloorZoneLayout layout,
            bool hasPersonalVisionLight)
        {
            if (emitLight > 0)
                return false;

            if (hasPersonalVisionLight)
                return receivedLight <= 0;

            if (ZoneRequiresPersonalLightForVision(zoneId, layout))
                return true;

            return receivedLight <= 0;
        }

        static bool HasTag(string[] tags, string required)
        {
            if (tags == null || string.IsNullOrEmpty(required))
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == required)
                    return true;
            }

            return false;
        }
    }
}
