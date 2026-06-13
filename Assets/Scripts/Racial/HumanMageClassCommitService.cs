using JRogue.Actors;
using JRogue.Manager.Essence;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    /// <summary>
    /// Gates and execution for Human None → Mage apprenticeship (tutor quest).
    /// </summary>
    public static class HumanMageClassCommitService
    {
        public const int ApprenticeshipGoldCost = 5;

        public const string RaceDenyMessage = "Only humans may train as mages here.";
        public const string ClassDenyMessage = "You have already committed to another path.";
        public const string EssenceDenyMessage =
            "You must relinquish all consumed essences before you can study the arcana.";
        public const string GoldDenyMessage = "The tutor requires 5 gold for initiation.";

        public static bool CanBeginMageTraining(BaseActor human, out string denyReason)
        {
            denyReason = null;
            if (human == null)
            {
                denyReason = "No speaker.";
                return false;
            }

            CharacterStats stats = human.GetComponent<CharacterStats>();
            if (stats == null || stats.race != Race.Human)
            {
                denyReason = RaceDenyMessage;
                return false;
            }

            if (stats.humanClass != HumanClass.None)
            {
                denyReason = ClassDenyMessage;
                return false;
            }

            EssenceSlotManager essence = human.GetComponent<EssenceSlotManager>();
            if (essence != null && essence.CountOccupiedSlots() > 0)
            {
                denyReason = EssenceDenyMessage;
                return false;
            }

            return true;
        }

        public static bool HasApprenticeshipGold() =>
            ShopGoldUtility.GetPartyGoldTotal() >= ApprenticeshipGoldCost;

        public static bool TryCompleteMageApprenticeship(BaseActor human, out string failureReason)
        {
            failureReason = null;
            if (!CanBeginMageTraining(human, out failureReason))
                return false;

            if (!HasApprenticeshipGold())
            {
                failureReason = GoldDenyMessage;
                return false;
            }

            if (!ShopGoldUtility.TrySpendPartyGold(ApprenticeshipGoldCost))
            {
                failureReason = GoldDenyMessage;
                return false;
            }

            if (!HumanClassCommitment.TryCommit(human.gameObject, HumanClass.Mage, out failureReason))
            {
                ShopGoldUtility.AddPartyGold(ApprenticeshipGoldCost);
                return false;
            }

            return true;
        }
    }
}
