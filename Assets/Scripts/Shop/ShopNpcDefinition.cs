using System;
using JRogue.Dialog;
using JRogue.Item;
using UnityEngine;

namespace JRogue.Shop
{
    [CreateAssetMenu(fileName = "ShopNpcDefinition", menuName = "JRogue/Shop/Shop NPC Definition")]
    public sealed class ShopNpcDefinition : ScriptableObject
    {
        public string shopNpcId;
        public string displayName;
        public PortraitDefinition portrait;
        public bool allowPlayerBuy = true;
        public bool allowPlayerSell = true;
        public bool allowPlayerSellManaStones = true;
        [Min(0)] public int initialGold = 100;
        public ShopStockEntry[] initialStock = Array.Empty<ShopStockEntry>();
    }
}
