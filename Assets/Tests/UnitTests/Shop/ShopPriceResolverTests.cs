using JRogue.Item;
using JRogue.Shop;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Shop
{
    [TestFixture]
    public sealed class ShopPriceResolverTests
    {
        [Test]
        public void GetBuyPrice_UsesDefaultWhenUnset()
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.buyValue = 0;
            Assert.AreEqual(ShopPriceResolver.DefaultBuyValue, ShopPriceResolver.GetBuyPrice(item));
            Object.DestroyImmediate(item);
        }

        [Test]
        public void GetSellPrice_DerivesHalfOfBuyWhenUnset()
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.buyValue = 2;
            item.sellValue = 0;
            Assert.AreEqual(1, ShopPriceResolver.GetSellPrice(item));
            Object.DestroyImmediate(item);
        }

        [Test]
        public void GetSellPrice_UsesExplicitOverride()
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.buyValue = 80;
            item.sellValue = 40;
            Assert.AreEqual(40, ShopPriceResolver.GetSellPrice(item));
            Object.DestroyImmediate(item);
        }

        [Test]
        public void GetManaStoneSellPrice_FollowsTierFormula()
        {
            Assert.AreEqual(1, ShopPriceResolver.GetManaStoneSellPrice(9));
            Assert.AreEqual(2, ShopPriceResolver.GetManaStoneSellPrice(8));
            Assert.AreEqual(9, ShopPriceResolver.GetManaStoneSellPrice(1));
        }

        [Test]
        public void GetManaStoneSellPrice_GuildExchangeFormula()
        {
            Assert.AreEqual(2, ShopPriceResolver.GetManaStoneSellPrice(9, ShopManaStoneSellPricing.GuildExchange));
            Assert.AreEqual(4, ShopPriceResolver.GetManaStoneSellPrice(8, ShopManaStoneSellPricing.GuildExchange));
            Assert.AreEqual(18, ShopPriceResolver.GetManaStoneSellPrice(1, ShopManaStoneSellPricing.GuildExchange));
        }

        [Test]
        public void GetBuyPrice_ManaStoneIsZero()
        {
            var stone = ScriptableObject.CreateInstance<ManaStoneItemData>();
            stone.tier = 5;
            Assert.AreEqual(0, ShopPriceResolver.GetBuyPrice(stone));
            Object.DestroyImmediate(stone);
        }
    }
}
