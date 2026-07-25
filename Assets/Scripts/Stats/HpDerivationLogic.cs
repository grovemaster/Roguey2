using UnityEngine;

namespace JRogue.Stats
{
    /// <summary>
    /// Pure Max HP derivation: race + class + level + soft Constitution contribution.
    /// Replaces the former <c>Constitution × 10</c> coupling.
    /// </summary>
    public static class HpDerivationLogic
    {
        /// <summary>Default Human-like race base when none is authored (tutorial-band L1).</summary>
        public const int DefaultRaceBaseHp = 12;

        /// <summary>C1: +1 Max HP per Constitution point.</summary>
        public static int ConstitutionContribution(int constitution) =>
            Mathf.Max(0, constitution);

        public static int ComputeMaxHp(
            int raceBaseHp,
            int classBaseHp,
            int levelHpGain,
            int constitution,
            int flatBonus = 0)
        {
            int race = raceBaseHp > 0 ? raceBaseHp : DefaultRaceBaseHp;
            int total = race
                        + Mathf.Max(0, classBaseHp)
                        + Mathf.Max(0, levelHpGain)
                        + ConstitutionContribution(constitution)
                        + flatBonus;
            return Mathf.Max(1, total);
        }
    }
}
