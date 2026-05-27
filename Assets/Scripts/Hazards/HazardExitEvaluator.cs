using JRogue.Actors;

namespace JRogue.Hazards
{
    /// <summary>Whether an occupant may leave a hazard cell (snare traps later).</summary>
    public static class HazardExitEvaluator
    {
        public static bool CanExit(EnvironmentalHazardDefinition definition, BaseActor actor)
        {
            if (definition == null || actor == null)
                return true;

            return definition.exitCondition switch
            {
                HazardExitCondition.Always => true,
                _ => true,
            };
        }
    }
}
