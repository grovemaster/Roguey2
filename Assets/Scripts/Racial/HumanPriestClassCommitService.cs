using JRogue.Actors;
using JRogue.Manager.Essence;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    public static class HumanPriestClassCommitService
    {
        public const int InitiationGoldCost = 5;

        public const string RaceDenyMessage = "Only humans may swear covenant oaths here.";
        public const string ClassDenyMessage = "You have already committed to another path.";
        public const string EssenceDenyMessage =
            "You must relinquish all consumed essences before you can swear a divine covenant.";
        public const string GoldDenyMessage = "The shrine requires 5 gold for initiation.";

        public static bool CanBeginPriestInitiation(BaseActor human, out string denyReason)
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

        public static bool HasInitiationGold() =>
            ShopGoldUtility.GetPartyGoldTotal() >= InitiationGoldCost;
    }
}
