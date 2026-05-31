namespace JRogue.World.Altar
{
    public static class AltarSlotFilters
    {
        public static bool TryGetManaStoneTier(AltarSlotAcceptFilter filter, out int tier)
        {
            tier = 0;
            if (filter is ManaStoneTierAcceptFilter tierFilter)
            {
                tier = tierFilter.tier;
                return true;
            }

            return false;
        }
    }
}
