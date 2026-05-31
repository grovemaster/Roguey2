namespace JRogue.World.Altar
{
    public static class AltarCompletionEvaluator
    {
        public static bool IsRuleSatisfied(AltarInstance instance, AltarCompletionRule rule)
        {
            if (instance == null || rule == null)
                return false;

            if (rule.requiredSlotIds == null || rule.requiredSlotIds.Length == 0)
                return AllSlotsFilled(instance);

            for (int i = 0; i < rule.requiredSlotIds.Length; i++)
            {
                string slotId = rule.requiredSlotIds[i];
                AltarSlotState slot = instance.FindSlotById(slotId);
                if (slot == null || slot.IsEmpty)
                    return false;
            }

            return true;
        }

        public static bool AllSlotsFilled(AltarInstance instance)
        {
            if (instance == null)
                return false;

            for (int i = 0; i < instance.Slots.Count; i++)
            {
                if (instance.Slots[i].IsEmpty)
                    return false;
            }

            return instance.Slots.Count > 0;
        }
    }
}
