using System;
using JRogue.Item;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public struct CyborgImplantInstallCost
    {
        [Min(0)] public int gold;
        public CyborgImplantItemCost[] items;
        public CyborgImplantFlagCost[] storyFlags;
    }

    [Serializable]
    public struct CyborgImplantRemoveCost
    {
        [Min(0)] public int gold;
        public CyborgImplantItemCost[] items;
        public CyborgImplantFlagCost[] storyFlags;
    }

    [Serializable]
    public struct CyborgImplantItemCost
    {
        public ItemData item;
        [Min(1)] public int quantity;
    }

    [Serializable]
    public struct CyborgImplantFlagCost
    {
        public string flagId;
        public bool expectedValue;
    }
}
