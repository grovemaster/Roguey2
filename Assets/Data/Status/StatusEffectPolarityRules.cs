namespace JRogue.Status
{
    /// <summary>Default polarity per <see cref="StatusEffectId"/>; assets may override via <see cref="StatusEffectDefinition.polarity"/>.</summary>
    public static class StatusEffectPolarityRules
    {
        public static StatusPolarity GetDefaultPolarity(StatusEffectId id) =>
            id switch
            {
                StatusEffectId.Poisoned => StatusPolarity.Negative,
                StatusEffectId.Drained => StatusPolarity.Negative,
                StatusEffectId.Slowed => StatusPolarity.Negative,
                StatusEffectId.Might => StatusPolarity.Positive,
                StatusEffectId.Hasted => StatusPolarity.Positive,
                StatusEffectId.Stunned => StatusPolarity.Negative,
                _ => StatusPolarity.Neutral
            };

        public static bool IsNegative(StatusPolarity polarity) =>
            polarity == StatusPolarity.Negative;

        public static bool IsPositive(StatusPolarity polarity) =>
            polarity == StatusPolarity.Positive;
    }
}
