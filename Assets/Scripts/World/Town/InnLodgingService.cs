using JRogue.Shop;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Town
{
    public enum InnLodgingPaymentResult
    {
        Success = 0,
        AlreadyPaid = 1,
        InsufficientGold = 2,
        CalendarUnavailable = 3,
        InvalidShop = 4,
    }

    /// <summary>Run-scoped inn bed access paid through the next dungeon portal day.</summary>
    public sealed class InnLodgingService : MonoBehaviour
    {
        public const string LogPrefix = "[InnLodging]";
        public const int DefaultLodgingCostGold = 5;

        public static InnLodgingService Instance { get; private set; }

        [SerializeField] int bedAccessUntilAbsoluteDay = -1;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static InnLodgingService EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject(nameof(InnLodgingService));
            return go.AddComponent<InnLodgingService>();
        }

        public static void EnsureRunService() => EnsureInstance();

        public static bool HasBedAccess()
        {
            GameCalendarService calendar = GameCalendarService.Instance;
            if (calendar == null || !calendar.IsEnabled)
                return false;

            InnLodgingService service = Instance;
            if (service == null)
                return false;

            int currentAbsoluteDay = GameCalendarLogic.ToAbsoluteDayIndex(calendar.CurrentDate);
            return currentAbsoluteDay < service.bedAccessUntilAbsoluteDay;
        }

        public static int GetRemainingBedAccessDays()
        {
            if (!HasBedAccess())
                return 0;

            GameCalendarService calendar = GameCalendarService.Instance;
            InnLodgingService service = Instance;
            if (calendar == null || service == null)
                return 0;

            int currentAbsoluteDay = GameCalendarLogic.ToAbsoluteDayIndex(calendar.CurrentDate);
            return Mathf.Max(0, service.bedAccessUntilAbsoluteDay - currentAbsoluteDay);
        }

        public static InnLodgingPaymentResult TryPayForLodging(
            ShopNpcDefinition shopDefinition,
            int lodgingCostGold,
            out string message)
        {
            message = string.Empty;
            if (shopDefinition == null || string.IsNullOrWhiteSpace(shopDefinition.shopNpcId))
            {
                message = "The innkeeper cannot take payment right now.";
                return InnLodgingPaymentResult.InvalidShop;
            }

            GameCalendarService calendar = GameCalendarService.Instance;
            if (calendar == null || !calendar.IsEnabled)
            {
                message = "The inn is not ready for guests yet.";
                return InnLodgingPaymentResult.CalendarUnavailable;
            }

            EnsureRunService();

            if (HasBedAccess())
            {
                message = "Your room is already paid through the next dungeon day.";
                return InnLodgingPaymentResult.AlreadyPaid;
            }

            if (lodgingCostGold > 0 && ShopGoldUtility.GetPartyGoldTotal() < lodgingCostGold)
            {
                message = $"You need {lodgingCostGold} gold for a room.";
                return InnLodgingPaymentResult.InsufficientGold;
            }

            if (lodgingCostGold > 0 && !ShopGoldUtility.TrySpendPartyGold(lodgingCostGold))
            {
                message = $"You need {lodgingCostGold} gold for a room.";
                return InnLodgingPaymentResult.InsufficientGold;
            }

            int currentAbsoluteDay = GameCalendarLogic.ToAbsoluteDayIndex(calendar.CurrentDate);
            int nextPortalAbsoluteDay = GameCalendarLogic.GetNextPortalAbsoluteDayExclusive(
                currentAbsoluteDay,
                calendar.DungeonPortalIntervalDays,
                calendar.DungeonPortalStartDay);

            Instance.bedAccessUntilAbsoluteDay = nextPortalAbsoluteDay;

            TownShopStateService shopState = TownShopStateService.EnsureInstance();
            ShopStateSnapshot snapshot = shopState.GetOrCreateSnapshot(shopDefinition);
            if (snapshot != null)
            {
                snapshot.goldOnHand += lodgingCostGold;
                shopState.SaveSnapshot(snapshot);
            }

            message =
                lodgingCostGold > 0
                    ? $"Paid {lodgingCostGold} gold. The beds are yours until the next dungeon day."
                    : "The beds are yours until the next dungeon day.";

            Debug.Log(
                $"{LogPrefix} Lodging paid — access until absolute day {nextPortalAbsoluteDay} " +
                $"(innkeeper gold +{lodgingCostGold}).");
            return InnLodgingPaymentResult.Success;
        }
    }
}
