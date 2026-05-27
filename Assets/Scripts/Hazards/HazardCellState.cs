namespace JRogue.Hazards
{
    /// <summary>Per-cell runtime state for a registered environmental hazard.</summary>
    public sealed class HazardCellState
    {
        public EnvironmentalHazardDefinition Definition { get; }
        public bool StartsHidden { get; }
        public bool IsRevealed { get; private set; }

        public HazardCellState(EnvironmentalHazardDefinition definition, bool startsHidden)
        {
            Definition = definition;
            StartsHidden = startsHidden;
            IsRevealed = !startsHidden;
        }

        public bool IsHiddenToPlayer => StartsHidden && !IsRevealed;

        public void Reveal() => IsRevealed = true;
    }
}
