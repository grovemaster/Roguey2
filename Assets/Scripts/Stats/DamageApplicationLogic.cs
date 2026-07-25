using UnityEngine;

namespace JRogue.Stats
{
    /// <summary>
    /// Pure resist → armor mitigation pipeline for a single hit.
    /// </summary>
    public static class DamageApplicationLogic
    {
        public const int FullArmorDivisor = 5;
        public const int PartialArmorDivisor = 10;

        /// <summary>Max fraction of raw damage that resist + armor may remove (O3 default).</summary>
        public const float MaxMitigationFraction = 0.8f;

        public static int ArmorMitigation(int armorClass, ArmorInteraction interaction)
        {
            if (armorClass <= 0)
                return 0;

            return interaction switch
            {
                ArmorInteraction.Full => armorClass / FullArmorDivisor,
                ArmorInteraction.Partial => armorClass / PartialArmorDivisor,
                _ => 0
            };
        }

        /// <summary>
        /// Applies typed resistance then armor interaction. Always returns at least 1 when raw &gt; 0.
        /// </summary>
        public static int ComputeFinalDamage(
            int rawDamage,
            int resistance,
            int armorClass,
            ArmorInteraction armorInteraction)
        {
            if (rawDamage <= 0)
                return 0;

            int afterResist = Mathf.Max(1, rawDamage - resistance);
            int armorMit = ArmorMitigation(armorClass, armorInteraction);

            int maxMitigation = Mathf.FloorToInt(rawDamage * MaxMitigationFraction);
            int resistTaken = rawDamage - afterResist;
            int allowedArmor = Mathf.Max(0, maxMitigation - resistTaken);
            armorMit = Mathf.Min(armorMit, allowedArmor);

            return Mathf.Max(1, afterResist - armorMit);
        }
    }
}
