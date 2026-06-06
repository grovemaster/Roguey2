using UnityEngine;

namespace JRogue.Interactables
{
    /// <summary>Runtime town time lever definitions when authored assets are unavailable.</summary>
    public static class TownTimeLeverContent
    {
        public struct LeverPair
        {
            public InteractableTileDefinition LeverA;
            public InteractableTileDefinition LeverB;
        }

        public static LeverPair CreateRuntimeDefinitions()
        {
            var alwaysTrue = ScriptableObject.CreateInstance<AlwaysTruePrecondition>();
            var advancePhase = ScriptableObject.CreateInstance<TownTimeLeverEffect>();

            return new LeverPair
            {
                LeverA = CreateLever(
                    InteractableTileId.TownTimeLeverA,
                    "Time lever A",
                    new InteractablePrecondition[] { alwaysTrue },
                    new InteractableEffect[] { advancePhase }),
                LeverB = CreateLever(
                    InteractableTileId.TownTimeLeverB,
                    "Time lever B",
                    new InteractablePrecondition[] { alwaysTrue },
                    new InteractableEffect[] { advancePhase }),
            };
        }

        static InteractableTileDefinition CreateLever(
            InteractableTileId id,
            string displayName,
            InteractablePrecondition[] preconditions,
            InteractableEffect[] effects)
        {
            var def = ScriptableObject.CreateInstance<InteractableTileDefinition>();
            def.interactableId = id;
            def.displayName = displayName;
            def.kind = InteractableTileKind.Lever;
            def.blocksOccupancy = true;
            def.bumpEnabled = true;
            def.preconditions = preconditions;
            def.onActivateEffects = effects;
            def.spriteOff = InteractablePlaceholderSprites.OffRight;
            def.spriteOn = InteractablePlaceholderSprites.OnLeft;
            return def;
        }
    }
}
