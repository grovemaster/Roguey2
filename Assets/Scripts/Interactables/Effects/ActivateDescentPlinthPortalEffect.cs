using JRogue.Actors;
using JRogue.Manager.Progression;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "ActivateDescentPlinthPortal",
        menuName = "JRogue/Interactables/Effects/Activate Descent Plinth Portal")]
    public sealed class ActivateDescentPlinthPortalEffect : InteractableEffect
    {
        [Min(1)]
        public int experienceAmount = 2;

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            DungeonFloorInstance floor = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            if (floor == null)
            {
                Debug.LogWarning("[Interactable] No active dungeon floor for descent plinth activation.");
                return;
            }

            if (floor.IsDescentPlinthActivated)
                return;

            PartyExperienceService xp = PartyExperienceService.Instance;
            if (xp != null)
                xp.AwardPartyExperience(experienceAmount, "Descent plinth");

            floor.ActivateDescentPlinthPortal(service);
        }
    }
}
