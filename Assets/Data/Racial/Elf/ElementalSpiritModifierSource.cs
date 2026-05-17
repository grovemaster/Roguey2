using System;

namespace JRogue.Racial
{
    sealed class ElementalSpiritModifierSource : IEquatable<ElementalSpiritModifierSource>
    {
        public ElementalSpiritDefinition Spirit { get; }
        public string SpiritId { get; }

        public ElementalSpiritModifierSource(ElementalSpiritDefinition spirit)
        {
            Spirit = spirit;
            SpiritId = spirit != null ? spirit.spiritId : string.Empty;
        }

        public bool Equals(ElementalSpiritModifierSource other) =>
            other != null && SpiritId == other.SpiritId;

        public override bool Equals(object obj) => obj is ElementalSpiritModifierSource o && Equals(o);

        public override int GetHashCode() => SpiritId != null ? SpiritId.GetHashCode() : 0;
    }
}
