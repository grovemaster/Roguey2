using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.World.Altar
{
    public enum AltarOfferingResult
    {
        Failed = 0,
        Placed = 1,
        Removed = 2,
    }

    public static class AltarOfferingService
    {
        public static int FindPlaceSlotIndex(AltarInstance instance, int tier, string sourceSpeciesId)
        {
            if (instance?.Definition?.slots == null)
                return -1;

            for (int i = 0; i < instance.Definition.slots.Length; i++)
            {
                if (i >= instance.Slots.Count)
                    break;

                if (!instance.Slots[i].IsEmpty)
                    continue;

                AltarSlotDefinition slotDef = instance.Definition.slots[i];
                AltarSlotAcceptFilter filter = slotDef?.acceptFilter;
                if (filter == null || !filter.AcceptsManaStone(tier, sourceSpeciesId))
                    continue;

                return i;
            }

            return -1;
        }

        public static AltarOfferingResult TryPlaceManaStone(
            AltarInstance instance,
            int tier,
            string sourceSpeciesId)
        {
            if (instance == null || instance.IsDepleted)
                return AltarOfferingResult.Failed;

            int slotIndex = FindPlaceSlotIndex(instance, tier, sourceSpeciesId);
            if (slotIndex < 0)
                return AltarOfferingResult.Failed;

            PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
            if (ledger == null || !ledger.TrySpend(tier, sourceSpeciesId, 1))
                return AltarOfferingResult.Failed;

            instance.Slots[slotIndex].Offering = new AltarManaStoneOffering(tier, sourceSpeciesId);
            AltarCompletionRunner.TryFireCompletion(instance);
            return AltarOfferingResult.Placed;
        }

        public static AltarOfferingResult TryRemoveFromSlot(AltarInstance instance, int slotIndex)
        {
            if (instance == null || instance.IsDepleted || slotIndex < 0 || slotIndex >= instance.Slots.Count)
                return AltarOfferingResult.Failed;

            AltarSlotState slot = instance.Slots[slotIndex];
            if (slot.IsEmpty)
                return AltarOfferingResult.Failed;

            PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
            if (ledger == null)
                return AltarOfferingResult.Failed;

            ledger.Add(slot.Offering.Tier, slot.Offering.SourceSpeciesId, 1);
            slot.Offering = default;
            return AltarOfferingResult.Removed;
        }
    }
}
