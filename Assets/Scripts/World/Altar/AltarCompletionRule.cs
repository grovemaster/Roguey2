using System;
using UnityEngine;

namespace JRogue.World.Altar
{
    [Serializable]
    public sealed class AltarCompletionRule
    {
        public string ruleId = "default";
        [Tooltip("Empty = all altar slots must be filled.")]
        public string[] requiredSlotIds = Array.Empty<string>();
        public AltarCompletionEffect[] effects = Array.Empty<AltarCompletionEffect>();
    }
}
