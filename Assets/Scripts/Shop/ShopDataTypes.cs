using System;
using System.Collections.Generic;
using JRogue.Item;
using UnityEngine;

namespace JRogue.Shop
{
    public enum ShopManaStoneSellPricing
    {
        Default = 0,
        GuildExchange = 1,
    }

    public static class TownShopNpcIds
    {
        public const string Npc4 = "town_npc_4";
        public const string Npc5 = "town_npc_5";
        public const string FairyMerchant = "fairy_merchant";
        public const string ElfGroveFairyMerchant = "elf_grove_fairy_merchant";
        public const string BeastmanDenBeastBloodMerchant = "beastman_den_beast_blood_merchant";
        public const string BeastBloodMerchant = "beast_blood_merchant";
        public const string ResidentialInnKeeper = "residential_inn_keeper";
        public const string MarketItemShopClerk = "market_item_shop_clerk";
        public const string MarketBlacksmith = "market_blacksmith";
        public const string AdventureGuildClerk = "adventure_guild_clerk";
    }

    [Serializable]
    public sealed class ShopStockEntry
    {
        public ItemData item;
        [Min(1)] public int quantity = 1;
    }

    [Serializable]
    public sealed class ShopStockSnapshot
    {
        public ItemData item;
        public int quantity;
    }

    [Serializable]
    public sealed class ShopStateSnapshot
    {
        public string shopNpcId;
        public int goldOnHand;
        public List<ShopStockSnapshot> stock = new List<ShopStockSnapshot>();
        public List<ShopStockSnapshot> boughtFromPlayer = new List<ShopStockSnapshot>();

        public ShopStateSnapshot Clone()
        {
            return new ShopStateSnapshot
            {
                shopNpcId = shopNpcId,
                goldOnHand = goldOnHand,
                stock = CloneList(stock),
                boughtFromPlayer = CloneList(boughtFromPlayer),
            };
        }

        static List<ShopStockSnapshot> CloneList(List<ShopStockSnapshot> source)
        {
            var copy = new List<ShopStockSnapshot>(source?.Count ?? 0);
            if (source == null)
                return copy;

            for (int i = 0; i < source.Count; i++)
            {
                ShopStockSnapshot row = source[i];
                if (row == null || row.item == null || row.quantity <= 0)
                    continue;

                copy.Add(new ShopStockSnapshot
                {
                    item = row.item,
                    quantity = row.quantity,
                });
            }

            return copy;
        }
    }
}
