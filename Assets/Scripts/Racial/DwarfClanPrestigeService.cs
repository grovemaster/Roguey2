using JRogue.Quest;
using JRogue.Shop;
using UnityEngine;

namespace JRogue.Racial
{
    public static class DwarfClanPrestigeService
    {
        public static int AddPrestige(DwarfClanDefinition clan, int amount, string sourceLabel = null)
        {
            if (clan == null || string.IsNullOrWhiteSpace(clan.clanId) || amount <= 0)
                return 0;

            DwarfClanWorldState world = DwarfClanWorldState.EnsureInstance();
            world.EnsurePrestige(clan.clanId, clan.startingPrestige);
            int total = world.AddPrestige(clan.clanId, amount);

            if (!string.IsNullOrWhiteSpace(sourceLabel))
            {
                Debug.Log(
                    $"[DwarfClan] Clan '{clan.clanId}' prestige +{amount} ({sourceLabel}). Total {total}.");
            }

            return total;
        }

        public static bool TryApplyQuestReward(QuestRewardBundle rewards, out string failureReason)
        {
            failureReason = null;
            if (rewards.clanPrestige <= 0 || string.IsNullOrWhiteSpace(rewards.clanPrestigeClanId))
                return true;

            DwarfClanDefinition clan = DwarfClanRegistry.TryLoadByClanId(rewards.clanPrestigeClanId);
            if (clan == null)
            {
                failureReason = $"Unknown clan '{rewards.clanPrestigeClanId}'.";
                return false;
            }

            AddPrestige(clan, rewards.clanPrestige, "quest reward");
            return true;
        }

        public static bool TryDonateGold(
            DwarfClanDefinition clan,
            int goldAmount,
            out int prestigeGained,
            out int totalPrestige,
            out string failureReason)
        {
            prestigeGained = 0;
            totalPrestige = 0;
            failureReason = null;

            if (!DwarfClanDonationLogic.CanBeginDonation(out failureReason))
                return false;

            if (clan == null || string.IsNullOrWhiteSpace(clan.clanId))
            {
                failureReason = "This clan cannot receive offerings.";
                return false;
            }

            if (!DwarfClanDonationLogic.TryResolveDonationTier(goldAmount, out prestigeGained, out failureReason))
                return false;

            if (ShopGoldUtility.GetPartyGoldTotal() < goldAmount)
            {
                failureReason = DwarfClanDonationLogic.InsufficientGoldMessage;
                return false;
            }

            if (!ShopGoldUtility.TrySpendPartyGold(goldAmount))
            {
                failureReason = DwarfClanDonationLogic.InsufficientGoldMessage;
                return false;
            }

            totalPrestige = AddPrestige(clan, prestigeGained, $"treasury donation ({goldAmount} gold)");
            return true;
        }
    }
}
