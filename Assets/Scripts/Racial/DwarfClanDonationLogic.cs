using JRogue.World.Generation;

namespace JRogue.Racial
{
    public static class DwarfClanDonationLogic
    {
        public const int SmallDonationGold = 10;
        public const int MediumDonationGold = 25;
        public const int LargeDonationGold = 50;

        public const int SmallDonationPrestige = 1;
        public const int MediumDonationPrestige = 2;
        public const int LargeDonationPrestige = 3;

        public const string InsufficientGoldMessage = "Your party does not have enough gold.";
        public const string InvalidDonationMessage =
            "Offer at least 10 gold to the clan treasury (10, 25, or 50 gold tiers).";

        public static bool CanBeginDonation(out string denyReason) =>
            SafeZonePolicyService.TryAllowDwarfClanCeremony(out denyReason);

        public static bool TryResolveDonationTier(int goldAmount, out int prestigeGained, out string failureReason)
        {
            prestigeGained = 0;
            failureReason = null;

            switch (goldAmount)
            {
                case SmallDonationGold:
                    prestigeGained = SmallDonationPrestige;
                    return true;
                case MediumDonationGold:
                    prestigeGained = MediumDonationPrestige;
                    return true;
                case LargeDonationGold:
                    prestigeGained = LargeDonationPrestige;
                    return true;
                default:
                    failureReason = InvalidDonationMessage;
                    return false;
            }
        }

        public static string BuildDonationPrompt(DwarfClanDefinition clan, int currentPrestige)
        {
            string name = clan?.shortName ?? clan?.displayName ?? "clan";
            return
                $"Offer gold to the {name} treasury.\n\n"
                + $"Clan prestige: {currentPrestige}\n"
                + $"{SmallDonationGold} gold → +{SmallDonationPrestige} prestige\n"
                + $"{MediumDonationGold} gold → +{MediumDonationPrestige} prestige\n"
                + $"{LargeDonationGold} gold → +{LargeDonationPrestige} prestige";
        }

        public static string BuildDonationSuccessLine(int prestigeGained, int totalPrestige) =>
            $"The clan accepts your offering (+{prestigeGained} prestige). "
            + $"Clan prestige is now {totalPrestige}.";
    }
}
