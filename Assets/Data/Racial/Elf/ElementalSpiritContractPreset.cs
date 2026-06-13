using System;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public class ElementalSpiritContractPreset
    {
        public string contractInstanceId;
        public ElementalSpiritDefinition spirit;
        [Min(1)] public int contractLevel = 1;
        [Min(0)] public int contractExperience;
        [Tooltip("Optional player label for hotbar summon/dismiss entries.")]
        public string nickname;

        public void EnsureInstanceId()
        {
            if (string.IsNullOrEmpty(contractInstanceId))
                contractInstanceId = Guid.NewGuid().ToString("N");
        }
    }
}
