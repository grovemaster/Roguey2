using System;
using UnityEngine;

namespace JRogue.World.Altar
{
    [Serializable]
    public sealed class AltarSlotDefinition
    {
        public string slotId = "slot";
        public string label = "Offering slot";
        public AltarSlotAcceptFilter acceptFilter;
        [Min(1)]
        public int maxCount = 1;
    }
}
