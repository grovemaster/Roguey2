using JRogue.Actors;
using JRogue.Stats;

namespace JRogue.Hazards
{
    public static class HazardPassageEvaluator
    {
        public static bool MeetsPassageCondition(EnvironmentalHazardDefinition definition, BaseActor actor)
        {
            if (definition == null || actor == null)
                return true;

            if (definition.kind != EnvironmentalHazardKind.Passage)
                return true;

            return definition.passageCondition switch
            {
                PassageCondition.MinimumStrength => MeetsStrength(actor, definition.requiredStrength),
                PassageCondition.AlwaysAllow => true,
                PassageCondition.Fly => false,
                PassageCondition.Swim => false,
                _ => true,
            };
        }

        public static bool CanEnter(EnvironmentalHazardDefinition definition, BaseActor actor) =>
            MeetsPassageCondition(definition, actor);

        static bool MeetsStrength(BaseActor actor, int required)
        {
            CharacterStats stats = actor.stats;
            if (stats == null)
                return false;

            return stats.Strength.GetValue() >= required;
        }
    }
}
