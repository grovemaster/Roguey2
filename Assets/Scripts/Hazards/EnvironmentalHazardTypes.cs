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

    /// <summary>How a hidden hazard may be passively revealed to the player.</summary>
    public enum HazardDetectionMethod
    {
        None = 0,
        PartyStatInRange = 1,
        PartySkill = 2,
    }

    /// <summary>When an occupant may leave a hazard cell (snare traps later).</summary>
    public enum HazardExitCondition
    {
        Always = 0,
    }
}
