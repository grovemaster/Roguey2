using JRogue.Stats;
using UnityEngine;

namespace JRogue.Stats.Racial
{
    /// <summary>
    /// Human class commitment rules. See Docs/RacialSystem/Human-Class-Powers-Requirements.md.
    /// </summary>
    public static class HumanClassRules
    {
        public const int DefaultEssenceSlotCount = 3;

        public static int GetMaxEssenceSlots(HumanClass humanClass) =>
            humanClass is HumanClass.Mage or HumanClass.Priest ? 0 : DefaultEssenceSlotCount;

        public static bool UsesSoulPower(HumanClass humanClass) =>
            humanClass is HumanClass.None or HumanClass.Knight;

        public static bool CanGainEssences(HumanClass humanClass) =>
            GetMaxEssenceSlots(humanClass) > 0;

        /// <summary>
        /// Placeholder class base HP added into Max HP derivation (Knight tanky, Mage fragile).
        /// </summary>
        public static int GetClassBaseHp(HumanClass humanClass) =>
            humanClass switch
            {
                HumanClass.Knight => 4,
                HumanClass.Priest => 2,
                HumanClass.Mage => 0,
                _ => 0
            };

        public static int ComputeMaxSoulPower(CharacterStats stats)
        {
            if (stats == null || !UsesSoulPower(stats.humanClass))
                return 0;

            return (stats.Intelligence.GetValue() * 5)
                   + (stats.Wisdom.GetValue() * 5)
                   + stats.levelSoulPowerBonus;
        }

        public static int ComputeMaxMagicPower(CharacterStats stats)
        {
            if (stats == null || stats.humanClass != HumanClass.Mage)
                return 0;

            return (stats.Intelligence.GetValue() * 5) + stats.levelMagicPowerBonus;
        }

        public static int ComputeMaxDivinePower(CharacterStats stats)
        {
            if (stats == null || stats.humanClass != HumanClass.Priest)
                return 0;

            return (stats.Wisdom.GetValue() * 5) + stats.levelDivinePowerBonus;
        }

        public static int GetSpellEquipCost(int spellTier, int extraEquipCost = 0) =>
            Mathf.Max(1, 10 - spellTier) + Mathf.Max(0, extraEquipCost);

        public static bool CanCommitToClass(HumanClass from, HumanClass to, out string error)
        {
            if (to == HumanClass.None)
            {
                error = "Cannot commit to HumanClass.None.";
                return false;
            }

            if (from != HumanClass.None)
            {
                error = $"Cannot change Human class from {from} to {to}; commitment is permanent.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool CanApplyHumanClassFromSnapshot(HumanClass liveClass, HumanClass snapshotClass, out string error)
        {
            if (liveClass != HumanClass.None && liveClass != snapshotClass)
            {
                error = "Human class commitment cannot be changed after initial commit.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
