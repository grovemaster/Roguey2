using System;
using JRogue.Item;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public struct SpiritImprintUnlockCost
    {
        [Min(0)] public int gold;
        public SpiritImprintItemCost[] items;
        public SpiritImprintFlagCost[] storyFlags;
    }

    [Serializable]
    public struct SpiritImprintItemCost
    {
        public ItemData item;
        [Min(1)] public int quantity;
    }

    [Serializable]
    public struct SpiritImprintFlagCost
    {
        public string flagId;
        public bool expectedValue;
    }
}
