namespace JRogue.Manager.Floor
{
    /// <summary>Advances floor lifetimes at each player phase boundary.</summary>
    public static class FloorLifetimeTicker
    {
        public static void TickAllOnPlayerPhaseStart()
        {
            FloorEssenceService essence = FloorEssenceService.Instance;
            if (essence != null)
                essence.TickDespawnAll();

            FloorItemPileService piles = FloorItemPileService.Instance;
            if (piles != null)
                piles.TickFloorItemLifetimes();
        }
    }
}
