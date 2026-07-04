namespace JRogue.World.Generation
{
    /// <summary>
    /// Portal sprite render gating for fog of war. District transition portals respect sight;
    /// only the calendar-gated town dungeon portal may show through fog on hub floors.
    /// </summary>
    public static class PortalFogVisibilityPolicy
    {
        public static bool ShouldRenderPortal(
            bool cellInFog,
            bool requiresTownTimeOpen,
            bool isHubFloor,
            bool portalOpen)
        {
            if (!portalOpen)
                return false;

            return cellInFog || (requiresTownTimeOpen && isHubFloor);
        }
    }
}
