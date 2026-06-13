using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public sealed class SoulBeastIdWeightBonus
    {
        public string soulBeastId;
        [Min(0)] public int bonusWeight = 1;
    }

    [CreateAssetMenu(fileName = "SoulBeastRitualOffering", menuName = "JRogue/Racial/Soul Beast Ritual Offering")]
    public sealed class SoulBeastRitualOfferingDefinition : ScriptableObject
    {
        public List<string> requiredRitualTypeIds = new List<string>();
        public List<string> poolFilterTags = new List<string>();
        public List<SoulBeastIdWeightBonus> soulBeastWeightBonuses = new List<SoulBeastIdWeightBonus>();
        public List<SoulBeastTagWeightBonus> tagWeightBonuses = new List<SoulBeastTagWeightBonus>();
        public List<string> poolExcludeSoulBeastIds = new List<string>();
    }
}
