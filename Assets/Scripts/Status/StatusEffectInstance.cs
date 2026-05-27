using System;
using UnityEngine;

namespace JRogue.Status
{
    [Serializable]
    public sealed class StatusEffectInstance
    {
        public StatusEffectDefinition definition;
        public int turnsRemaining;
        public GameObject source;

        public StatusEffectId StatusId =>
            definition != null ? definition.statusId : StatusEffectId.None;
    }
}
