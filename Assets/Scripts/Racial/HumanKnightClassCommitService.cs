using JRogue.Actors;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    /// <summary>
    /// Gates for Human None → Knight drill apprenticeship (tutor quest).
    /// </summary>
    public static class HumanKnightClassCommitService
    {
        public const int DrillGoldCost = 5;

        public const string RaceDenyMessage = "Only humans may train as knights here.";
        public const string ClassDenyMessage = "You have already committed to another path.";
        public const string GoldDenyMessage = "The drill master requires 5 gold for initiation.";

        public static bool CanBeginKnightTraining(BaseActor human, out string denyReason)
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

            return true;
        }

        public static bool HasDrillGold() =>
            ShopGoldUtility.GetPartyGoldTotal() >= DrillGoldCost;
    }
}
