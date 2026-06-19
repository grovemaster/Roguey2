using JRogue.World.Generation;

namespace JRogue.World.Town
{
    /// <summary>Backward-compatible entry point — delegates to <see cref="DistrictCalendarShopResetService"/>.</summary>
    public static class MarketItemShopResetService
    {
        public const string LogPrefix = DistrictCalendarShopResetService.LogPrefix;
        public const string ShopDefinitionResourcePath = MarketItemShopLayout.ShopDefinitionResourcePath;

        public static void TryResetForCurrentPostPortalDay() =>
            DistrictCalendarShopResetService.TryResetForCurrentPostPortalDay();

        public static void TryResetForPostPortalDay(
            GameCalendarDate date,
            int portalIntervalDays,
            int portalStartDay) =>
            DistrictCalendarShopResetService.TryResetForPostPortalDay(
                date,
                portalIntervalDays,
                portalStartDay);
    }
}
