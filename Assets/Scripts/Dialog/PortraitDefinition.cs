using JRogue.Actors;
using UnityEngine;

namespace JRogue.Dialog
{
    [CreateAssetMenu(fileName = "Portrait", menuName = "JRogue/Dialog/Portrait Definition")]
    public sealed class PortraitDefinition : ScriptableObject
    {
        public Sprite portrait;
        public Vector2 displayOffset;
    }

    public static class PortraitResolver
    {
        static PartyRacePortraitCatalog _catalog;

        public static PartyRacePortraitCatalog Catalog
        {
            get
            {
                if (_catalog != null)
                    return _catalog;

                _catalog = Resources.Load<PartyRacePortraitCatalog>("Dialog/PartyRacePortraitCatalog");
                return _catalog;
            }
            set => _catalog = value;
        }

        public static PortraitDefinition ResolveSpeaker(BaseActor actor, PortraitDefinition explicitPortrait)
        {
            if (explicitPortrait != null)
                return explicitPortrait;

            if (actor != null && actor.PortraitOverride != null)
                return actor.PortraitOverride;

            PartyRacePortraitCatalog catalog = Catalog;
            if (catalog != null)
                return catalog.ResolveForActor(actor);

            return null;
        }
    }
}
