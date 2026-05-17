using System;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public class ElementalSpiritContractPreset
    {
        public ElementalSpiritDefinition spirit;
        [Min(1)] public int contractLevel = 1;
    }
}
