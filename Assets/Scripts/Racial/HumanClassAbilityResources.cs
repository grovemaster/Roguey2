using JRogue.Ability;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    /// <summary>Maps ability costs to Soul / Magic / Divine pools by Human class.</summary>
    public static class HumanClassAbilityResources
    {
        public static int GetCost(CharacterStats stats, AbilityAction ability)
        {
            if (stats == null || ability == null)
                return 0;

            return stats.humanClass switch
            {
                HumanClass.Priest => ability.divinePowerCost,
                HumanClass.Mage => ability.magicPowerCost,
                _ => ability.soulPowerCost
            };
        }

        public static bool CanAfford(CharacterStats stats, AbilityAction ability)
        {
            if (stats == null || ability == null)
                return false;

            int cost = GetCost(stats, ability);
            return stats.humanClass switch
            {
                HumanClass.Priest => stats.currentDivinePower >= cost,
                HumanClass.Mage => stats.currentMagicPower >= cost,
                _ => stats.currentSoulPower >= cost
            };
        }

        public static bool TrySpend(CharacterStats stats, AbilityAction ability)
        {
            if (!CanAfford(stats, ability))
                return false;

            int cost = GetCost(stats, ability);
            switch (stats.humanClass)
            {
                case HumanClass.Priest:
                    stats.currentDivinePower -= cost;
                    return true;
                case HumanClass.Mage:
                    stats.currentMagicPower -= cost;
                    return true;
                default:
                    stats.currentSoulPower -= cost;
                    return true;
            }
        }

        public static string InsufficientResourceMessage(HumanClass humanClass) =>
            humanClass switch
            {
                HumanClass.Priest => "Not enough Divine Power!",
                HumanClass.Mage => "Not enough Magic Power!",
                _ => "Not enough Soul Power!"
            };
    }
}
