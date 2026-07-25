using UnityEngine;

namespace JRogue.Stats
{
    /// <summary>
    /// Band-aware attribute contributions to attack damage (not <c>+Strength</c>).
    /// </summary>
    public static class AttackDamageLogic
    {
        /// <summary>Melee: +floor(Strength / 4).</summary>
        public static int MeleeStrengthBonus(int strength) =>
            Mathf.Max(0, strength / 4);

        /// <summary>Ranged: +floor(Dexterity / 5) — smaller share than melee Strength.</summary>
        public static int RangedDexterityBonus(int dexterity) =>
            Mathf.Max(0, dexterity / 5);

        public static int ApplyMeleeStrengthBonus(int baseDamage, int strength) =>
            Mathf.Max(1, baseDamage + MeleeStrengthBonus(strength));

        public static int ApplyRangedDexterityBonus(int baseDamage, int dexterity) =>
            Mathf.Max(1, baseDamage + RangedDexterityBonus(dexterity));
    }
}
