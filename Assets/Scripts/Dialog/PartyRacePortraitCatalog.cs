using JRogue.Actors;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Dialog
{
    [CreateAssetMenu(fileName = "PartyRacePortraitCatalog", menuName = "JRogue/Dialog/Party Race Portrait Catalog")]
    public sealed class PartyRacePortraitCatalog : ScriptableObject
    {
        [System.Serializable]
        public struct RacePortraitEntry
        {
            public Race race;
            public PortraitDefinition portrait;
        }

        public PortraitDefinition fallbackPortrait;
        public RacePortraitEntry[] racePortraits = System.Array.Empty<RacePortraitEntry>();

        public PortraitDefinition ResolveForActor(BaseActor actor)
        {
            if (actor == null)
                return fallbackPortrait;

            PortraitDefinition actorOverride = actor.PortraitOverride;
            if (actorOverride != null)
                return actorOverride;

            if (actor.stats == null)
                return fallbackPortrait;

            Race race = actor.stats.race;
            for (int i = 0; i < racePortraits.Length; i++)
            {
                if (racePortraits[i].race == race && racePortraits[i].portrait != null)
                    return racePortraits[i].portrait;
            }

            return fallbackPortrait;
        }
    }
}
