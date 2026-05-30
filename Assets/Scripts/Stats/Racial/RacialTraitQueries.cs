using UnityEngine;

namespace JRogue.Stats.Racial
{
    public static class RacialTraitQueries
    {
        public static bool HasTrait(GameObject actor, RacialTraitFlags trait)
        {
            if (actor == null || trait == RacialTraitFlags.None)
                return false;

            return actor.TryGetComponent(out CharacterStats stats) && HasTrait(stats, trait);
        }

        public static bool HasTrait(CharacterStats stats, RacialTraitFlags trait)
        {
            if (stats == null || trait == RacialTraitFlags.None)
                return false;

            return (stats.racialTraits & trait) == trait;
        }
    }
}
