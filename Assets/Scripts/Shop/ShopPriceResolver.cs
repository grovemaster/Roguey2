using JRogue.Item;
using UnityEngine;

namespace JRogue.Shop
{
    public static class ShopPriceResolver
    {
        public const int DefaultBuyValue = 2;
        public const int DefaultSellValue = 1;

        public static int GetBuyPrice(ItemData item)
        {
            if (item == null)
                return 0;

            if (item is ManaStoneItemData)
                return 0;

            if (item.buyValue > 0)
                return item.buyValue;

            return DefaultBuyValue;
        }

        public static int GetSellPrice(ItemData item)
        {
            if (item == null)
                return 0;

            if (item is ManaStoneItemData manaStone)
                return GetManaStoneSellPrice(manaStone.tier, ShopManaStoneSellPricing.Default);

            if (item.sellValue > 0)
                return item.sellValue;

            int buy = GetBuyPrice(item);
            if (buy <= 0)
                return 0;

            return Mathf.Max(1, buy / 2);
        }

        public static int GetManaStoneSellPrice(int tier) =>
            GetManaStoneSellPrice(tier, ShopManaStoneSellPricing.Default);

        public static int GetManaStoneSellPrice(int tier, ShopManaStoneSellPricing pricing)
        {
            tier = Mathf.Clamp(tier, 1, 9);
            return pricing switch
            {
                ShopManaStoneSellPricing.GuildExchange => (10 - tier) * 2,
                _ => (9 - tier) + 1,
            };
        }
    }
}
