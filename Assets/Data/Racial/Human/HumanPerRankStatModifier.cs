using System;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    [Serializable]
    public struct HumanPerRankStatModifier
    {
        public StatType attribute;
        public int valuePerRank;
    }
}
