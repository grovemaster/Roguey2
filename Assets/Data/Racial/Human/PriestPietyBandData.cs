using System;
using System.Collections.Generic;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public sealed class PriestPietyBandData
    {
        [Min(0)] public int minPietyInclusive;
        [Min(1)] public int devotionSlots = 2;
        public string starLabel = "★☆☆☆☆";
        public List<HumanPerRankStatModifier> passiveModifiers = new();
    }
}
