namespace JRogue.Hazards
{
    public enum EnvironmentalHazardId
    {
        None = 0,
        Lava = 1,
        PoisonGas = 2,
    }

    public enum EnvironmentalHazardKind
    {
        Passage = 0,
        Persistent = 1,
    }

    public enum PassageCondition
    {
        None = 0,
        MinimumStrength = 1,
        Fly = 2,
        Swim = 3,
        AlwaysAllow = 4,
    }
}
