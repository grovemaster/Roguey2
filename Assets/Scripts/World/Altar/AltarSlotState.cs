namespace JRogue.World.Altar
{
    public sealed class AltarSlotState
    {
        public readonly string SlotId;
        public AltarManaStoneOffering Offering;

        public AltarSlotState(string slotId)
        {
            SlotId = slotId ?? string.Empty;
            Offering = default;
        }

        public bool IsEmpty => Offering.IsEmpty;
    }
}
