using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public sealed class SoulBeastWeightEntry
    {
        public string soulBeastId;
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public sealed class SoulBeastTagWeightBonus
    {
        public string tag;
        [Min(0)] public int bonusWeight = 1;
    }

    [CreateAssetMenu(fileName = "SoulBeastRitualType", menuName = "JRogue/Racial/Soul Beast Ritual Type")]
    public sealed class SoulBeastRitualTypeDefinition : ScriptableObject
    {
        public string ritualTypeId;
        public string displayName;
        [TextArea] public string description;

        public List<SoulBeastType> allowedSoulBeastTypes = new List<SoulBeastType>();
        public List<SoulBeastWeightEntry> baseWeights = new List<SoulBeastWeightEntry>();

        [Min(0)] public int noneOutcomeWeight = 50;
    }
}
