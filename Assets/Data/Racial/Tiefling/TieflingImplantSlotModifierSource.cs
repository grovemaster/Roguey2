using System;

namespace JRogue.Racial
{
    sealed class TieflingImplantSlotModifierSource : IEquatable<TieflingImplantSlotModifierSource>
    {
        public ImplantSlot Slot { get; }

        public TieflingImplantSlotModifierSource(ImplantSlot slot) => Slot = slot;

        public bool Equals(TieflingImplantSlotModifierSource other) =>
            other != null && Slot == other.Slot;

        public override bool Equals(object obj) => obj is TieflingImplantSlotModifierSource o && Equals(o);

        public override int GetHashCode() => ((int)Slot).GetHashCode();
    }
}
