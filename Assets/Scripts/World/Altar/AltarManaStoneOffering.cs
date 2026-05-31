namespace JRogue.World.Altar
{
    public readonly struct AltarManaStoneOffering
    {
        public readonly int Tier;
        public readonly string SourceSpeciesId;

        public AltarManaStoneOffering(int tier, string sourceSpeciesId)
        {
            Tier = tier;
            SourceSpeciesId = sourceSpeciesId ?? string.Empty;
        }

        public bool IsEmpty => Tier <= 0 || string.IsNullOrEmpty(SourceSpeciesId);
    }
}
