using System;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public class DwarfCommonSlotPreset
    {
        [Tooltip("Common ability slot index 0–2.")]
        [Range(0, 2)]
        public int slotIndex;

        public DwarfCommonAbilityDefinition ability;
    }
}
