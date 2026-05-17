using System;
using System.Collections.Generic;
using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public class ElementalSpiritLevelData
    {
        public List<AttributeModifier> statModifiers;
        public List<DamageResistanceModifier> resistanceModifiers;
        public List<PassiveEffect> passiveEffects;
        public List<ElementalSpiritActiveEntry> activeEntries;
    }
}
