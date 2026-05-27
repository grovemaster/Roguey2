using JRogue.Actors;
using JRogue.Manager.Progression;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "GrantPartyExperience",
        menuName = "JRogue/Interactables/Effects/Grant Party Experience")]
    public sealed class GrantPartyExperienceEffect : InteractableEffect
    {
        [Min(1)]
        public int experienceAmount = 25;

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            PartyExperienceService xp = PartyExperienceService.Instance;
            if (xp == null)
            {
                Debug.LogWarning("[Interactable] No PartyExperienceService to grant XP.");
                return;
            }

            string leverName = instance?.Definition != null
                ? instance.Definition.displayName
                : "Interactable";

            xp.AwardPartyExperience(experienceAmount, $"Lever:{leverName}");
        }
    }
}
