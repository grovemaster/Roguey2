using System;

namespace JRogue.World.LotF
{
    public enum LordOfTheFloorRunSlot
    {
        Available = 0,
        Summoned = 1,
        Consumed = 2,
    }

    /// <summary>Per-run LotF slot state. Pure data — no Unity dependencies.</summary>
    public sealed class LordOfTheFloorRunLedger
    {
        readonly System.Collections.Generic.Dictionary<string, LordOfTheFloorRunSlot> _slots =
            new System.Collections.Generic.Dictionary<string, LordOfTheFloorRunSlot>(StringComparer.Ordinal);

        public void Reset() => _slots.Clear();

        public LordOfTheFloorRunSlot Get(string lotfId)
        {
            if (string.IsNullOrEmpty(lotfId))
                return LordOfTheFloorRunSlot.Consumed;

            return _slots.TryGetValue(lotfId, out LordOfTheFloorRunSlot slot)
                ? slot
                : LordOfTheFloorRunSlot.Available;
        }

        public bool TryMarkSummoned(string lotfId)
        {
            if (string.IsNullOrEmpty(lotfId))
                return false;

            if (Get(lotfId) != LordOfTheFloorRunSlot.Available)
                return false;

            _slots[lotfId] = LordOfTheFloorRunSlot.Summoned;
            return true;
        }

        public void MarkConsumed(string lotfId)
        {
            if (string.IsNullOrEmpty(lotfId))
                return;

            _slots[lotfId] = LordOfTheFloorRunSlot.Consumed;
        }
    }
}
