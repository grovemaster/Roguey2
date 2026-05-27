using System;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Hazards
{
    [Serializable]
    public class HazardDetectionSettings
    {
        [Tooltip("Passive detection while hidden. None uses revealOnEnter only.")]
        public HazardDetectionMethod method = HazardDetectionMethod.None;

        [Tooltip("Reveal when any actor steps onto the cell.")]
        public bool revealOnEnter = true;

        public StatType statType = StatType.Sight;
        public SkillType skillType = SkillType.Perception;

        [Min(0)]
        public int minimumValue = 100;

        public bool requireLineOfSight = true;

        [Tooltip("When true, the observer's stat value is also used as max LOS range.")]
        public bool useStatValueAsRange = true;

        [Min(1)]
        public int fixedRange = 8;
    }
}
