using UnityEngine;

namespace JRogue.Interactables
{
    /// <summary>Builds the four SampleScene test levers (§9).</summary>
    public static class InteractableLeverContent
    {
        public struct LeverSet
        {
            public InteractableTileDefinition First;
            public InteractableTileDefinition Second;
            public InteractableTileDefinition Third;
            public InteractableTileDefinition Fourth;
        }

        public static LeverSet CreateRuntimeDefinitions()
        {
            var alwaysTrue = ScriptableObject.CreateInstance<AlwaysTruePrecondition>();
            var scriptOnly = ScriptableObject.CreateInstance<ScriptOnlyPrecondition>();
            var requiresFirst = ScriptableObject.CreateInstance<OtherInteractableOnPrecondition>();
            requiresFirst.requiredInteractableId = InteractableTileId.LeverSwitchFirst;
            var requiresThird = ScriptableObject.CreateInstance<OtherInteractableOnPrecondition>();
            requiresThird.requiredInteractableId = InteractableTileId.LeverSwitchThird;

            var activateThird = ScriptableObject.CreateInstance<ActivateInteractableEffect>();
            activateThird.targetInteractableId = InteractableTileId.LeverSwitchThird;
            var grantXp = ScriptableObject.CreateInstance<GrantPartyExperienceEffect>();
            grantXp.experienceAmount = 25;

            LeverSet set = new LeverSet
            {
                First = CreateLever(
                    InteractableTileId.LeverSwitchFirst,
                    "Lever 1",
                    bumpEnabled: true,
                    preconditions: new InteractablePrecondition[] { alwaysTrue },
                    effects: System.Array.Empty<InteractableEffect>()),
                Second = CreateLever(
                    InteractableTileId.LeverSwitchSecond,
                    "Lever 2",
                    bumpEnabled: true,
                    preconditions: new InteractablePrecondition[] { requiresFirst },
                    effects: new InteractableEffect[] { activateThird }),
                Third = CreateLever(
                    InteractableTileId.LeverSwitchThird,
                    "Lever 3",
                    bumpEnabled: false,
                    preconditions: new InteractablePrecondition[] { scriptOnly },
                    effects: System.Array.Empty<InteractableEffect>()),
                Fourth = CreateLever(
                    InteractableTileId.LeverSwitchFourth,
                    "Lever 4",
                    bumpEnabled: true,
                    preconditions: new InteractablePrecondition[] { requiresThird },
                    effects: new InteractableEffect[] { grantXp }),
            };

            return set;
        }

        static InteractableTileDefinition CreateLever(
            InteractableTileId id,
            string displayName,
            bool bumpEnabled,
            InteractablePrecondition[] preconditions,
            InteractableEffect[] effects)
        {
            var def = ScriptableObject.CreateInstance<InteractableTileDefinition>();
            def.interactableId = id;
            def.displayName = displayName;
            def.kind = InteractableTileKind.Lever;
            def.blocksOccupancy = true;
            def.bumpEnabled = bumpEnabled;
            def.preconditions = preconditions;
            def.onActivateEffects = effects;
            def.spriteOff = InteractablePlaceholderSprites.OffRight;
            def.spriteOn = InteractablePlaceholderSprites.OnLeft;
            return def;
        }
    }
}
