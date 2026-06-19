using JRogue.Shop;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Resets calendar-linked shop baselines on post-portal hub days.</summary>
    public static class MarketItemShopResetService
    {
        public const string LogPrefix = "[MarketItemShopReset]";
        public const string ShopDefinitionResourcePath = "Shop/ShopNpc_MarketItemShopClerk";

        static ShopNpcDefinition _cachedDefinition;

        public static void TryResetForCurrentPostPortalDay()
        {
            GameCalendarService calendar = GameCalendarService.Instance;
            if (calendar == null || !calendar.IsEnabled)
                return;

            TryResetForPostPortalDay(
                calendar.CurrentDate,
                calendar.DungeonPortalIntervalDays,
                calendar.DungeonPortalStartDay);
        }

        public static void TryResetForPostPortalDay(
            GameCalendarDate date,
            int portalIntervalDays,
            int portalStartDay)
        {
            if (!GameCalendarLogic.IsPostPortalDay(date, portalIntervalDays, portalStartDay))
                return;

            ShopNpcDefinition definition = LoadShopDefinition();
            if (definition == null)
            {
                Debug.LogWarning($"{LogPrefix} Missing shop definition at Resources/{ShopDefinitionResourcePath}.");
                return;
            }

            TownShopStateService shopState = TownShopStateService.EnsureInstance();
            shopState.ResetSnapshotFromDefinition(definition);
            Debug.Log($"{LogPrefix} Restocked item shop on {GameCalendarLogic.FormatDisplayDate(date)}.");
        }

        static ShopNpcDefinition LoadShopDefinition()
        {
            if (_cachedDefinition != null)
                return _cachedDefinition;

            _cachedDefinition = Resources.Load<ShopNpcDefinition>(ShopDefinitionResourcePath);
            return _cachedDefinition;
        }
    }
}
