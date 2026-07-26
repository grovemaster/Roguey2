namespace JRogue.World.Rift
{
    public static class RiftTransitionIds
    {
        public const string RiftTestFloorId = "rift_test";
        public const string HostToRiftPrefix = "link_host_to_";
        public const string RiftExitToHostPrefix = "link_rift_exit_to_host_";

        public static string HostToRift(string riftId) => HostToRiftPrefix + riftId;
        public static string RiftExitToHost(string riftId) => RiftExitToHostPrefix + riftId;

        public static bool IsHostToRift(string portalLinkId) =>
            !string.IsNullOrEmpty(portalLinkId) && portalLinkId.StartsWith(HostToRiftPrefix);

        public static bool IsRiftExit(string portalLinkId) =>
            !string.IsNullOrEmpty(portalLinkId) && portalLinkId.StartsWith(RiftExitToHostPrefix);

        public static string ParseRiftIdFromHostLink(string portalLinkId)
        {
            if (!IsHostToRift(portalLinkId))
                return null;
            return portalLinkId.Substring(HostToRiftPrefix.Length);
        }

        public static string ParseRiftIdFromExitLink(string portalLinkId)
        {
            if (!IsRiftExit(portalLinkId))
                return null;
            return portalLinkId.Substring(RiftExitToHostPrefix.Length);
        }
    }
}
