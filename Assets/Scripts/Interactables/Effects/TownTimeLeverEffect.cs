using JRogue.Actors;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "TownTimeAdvancePhase",
        menuName = "JRogue/Interactables/Effects/Town Time Advance Phase")]
    public sealed class TownTimeLeverEffect : InteractableEffect
    {
        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            if (service == null || instance?.Definition == null)
                return;

            TownTimeService townTime = TownTimeService.Instance;
            if (townTime == null)
            {
                Debug.LogWarning("[TownTime] TownTimeService missing — lever has no effect.");
                return;
            }

            townTime.OnTimeLeverActivated(instance.Definition.interactableId, service);
        }
    }
}
