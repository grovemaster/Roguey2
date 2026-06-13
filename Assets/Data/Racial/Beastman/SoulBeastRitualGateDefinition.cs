using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "SoulBeastRitualGate", menuName = "JRogue/Racial/Soul Beast Ritual Gate")]
    public sealed class SoulBeastRitualGateDefinition : ScriptableObject
    {
        public string gateId;
        public string displayName;
        public List<SoulBeastRitualTypeDefinition> ritualTypes = new List<SoulBeastRitualTypeDefinition>();
    }
}
