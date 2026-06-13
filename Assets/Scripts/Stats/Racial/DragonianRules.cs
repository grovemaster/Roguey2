using JRogue.Stats;
using UnityEngine;

namespace JRogue.Stats.Racial
{
    /// <summary>
    /// Dragonian Soul Power and spell memory rules.
    /// See Docs/RacialSystem/Dragonian-Spell-Memory-Requirements.md.
    /// </summary>
    public static class DragonianRules
    {
        public static bool UsesSoulPower(Race race) => race == Race.Dragonian;

        public static int ComputeMaxSoulPower(CharacterStats stats)
        {
            if (stats == null || !UsesSoulPower(stats.race))
                return 0;

            return (stats.Intelligence.GetValue() * 5)
                   + (stats.Wisdom.GetValue() * 5)
                   + stats.levelSoulPowerBonus;
        }
    }
}
