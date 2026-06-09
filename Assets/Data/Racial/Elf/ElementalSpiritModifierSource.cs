using System;

namespace JRogue.Racial
{
    sealed class ElementalSpiritModifierSource : IEquatable<ElementalSpiritModifierSource>
    {
        public ElementalSpiritDefinition Spirit { get; }
        public string SpiritId { get; }
        public string ContractInstanceId { get; }

        public ElementalSpiritModifierSource(ElementalSpiritDefinition spirit, string contractInstanceId)
        {
            Spirit = spirit;
            SpiritId = spirit != null ? spirit.spiritId : string.Empty;
            ContractInstanceId = contractInstanceId ?? string.Empty;
        }

        public bool Equals(ElementalSpiritModifierSource other) =>
            other != null && ContractInstanceId == other.ContractInstanceId;

        public override bool Equals(object obj) => obj is ElementalSpiritModifierSource o && Equals(o);

        public override int GetHashCode() =>
            ContractInstanceId != null ? ContractInstanceId.GetHashCode() : 0;
    }
}
