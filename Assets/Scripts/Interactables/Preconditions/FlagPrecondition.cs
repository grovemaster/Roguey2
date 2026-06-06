using JRogue.Actors;
using JRogue.Dialog;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(fileName = "Flag", menuName = "JRogue/Interactables/Preconditions/Flag")]
    public sealed class FlagPrecondition : InteractablePrecondition
    {
        public string flagId;
        public bool expectedValue = true;

        public override bool Evaluate(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source,
            out string failureReason)
        {
            failureReason = null;
            if (string.IsNullOrWhiteSpace(flagId))
            {
                failureReason = "Flag id is empty.";
                return false;
            }

            GameStoryFlagService.EnsureInstance();
            bool actual = GameStoryFlagService.Instance.IsSet(flagId);
            if (actual == expectedValue)
                return true;

            failureReason = expectedValue
                ? $"Requires story flag '{flagId}'."
                : $"Story flag '{flagId}' must not be set.";
            return false;
        }
    }
}
