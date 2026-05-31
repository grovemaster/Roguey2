using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;

namespace JRogue.Manager.Essence
{
    public static class EssencePickupEligibility
    {
        public static bool CanGain(BaseActor mover, EssenceData essence, out string reason)
        {
            reason = string.Empty;
            if (mover == null || essence == null)
            {
                reason = "invalid mover or essence";
                return false;
            }

            EssenceSlotManager slots = mover.GetComponent<EssenceSlotManager>();
            if (slots == null)
            {
                reason = "no essence slots";
                return false;
            }

            if (slots.HasEssence(essence))
            {
                reason = "you already have this essence";
                return false;
            }

            if (!slots.HasFreeSlot())
            {
                reason = "you already have the maximum number of essences";
                return false;
            }

            return true;
        }

        public static string BuildMoveDialogBody(BaseActor mover, EssenceData essence, bool canGain, string reason)
        {
            string moverName = mover != null ? mover.DisplayName : "Party member";
            string essenceName = !string.IsNullOrEmpty(essence?.essenceName)
                ? essence.essenceName
                : "essence";

            if (canGain)
            {
                return $"{moverName} is about to enter a tile with {essenceName}. "
                    + $"Entering the tile will immediately grant {essenceName}.";
            }

            return $"{moverName} is about to enter a tile with {essenceName}. "
                + $"Entering the tile will not grant {essenceName} because {reason}.";
        }
    }
}
