using JRogue.Shop;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Resets calendar-linked district shop baselines on post-portal hub days.</summary>
    public static class DistrictCalendarShopResetService
    {
        public const string LogPrefix = "[DistrictShopReset]";

        public static readonly string[] ShopDefinitionResourcePaths =
        {
            MarketItemShopLayout.ShopDefinitionResourcePath,
            MarketBlacksmithLayout.ShopDefinitionResourcePath,
        };

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

            TownShopStateService shopState = TownShopStateService.EnsureInstance();
            int resetCount = 0;

            for (int i = 0; i < ShopDefinitionResourcePaths.Length; i++)
            {
                string resourcePath = ShopDefinitionResourcePaths[i];
                ShopNpcDefinition definition = Resources.Load<ShopNpcDefinition>(resourcePath);
                if (definition == null)
                {
                    Debug.LogWarning($"{LogPrefix} Missing shop definition at Resources/{resourcePath}.");
                    continue;
                }

                shopState.ResetSnapshotFromDefinition(definition);
                resetCount++;
            }

            if (resetCount > 0)
            {
                Debug.Log(
                    $"{LogPrefix} Restocked {resetCount} district shop(s) on " +
                    $"{GameCalendarLogic.FormatDisplayDate(date)}.");
            }
        }
    }
}
