using System;
using JRogue.Ability;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public class ElementalSpiritActiveEntry
    {
        public AbilityAction ability;
        [Tooltip("Uses AbilityAction.soulPowerCost when > 0.")]
        public bool consumesTurn;
        public bool repeatableSameTurn;
        public ElementalSpiritActiveKind kind = ElementalSpiritActiveKind.Instant;
    }
}
