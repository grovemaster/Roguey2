using JRogue.Actors;
using JRogue.World.Town;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "InnBedSleepPrompt",
        menuName = "JRogue/Interactables/Effects/Inn Bed Sleep Prompt")]
    public sealed class InnBedSleepPromptEffect : InteractableEffect
    {
        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            InnBedSleepPromptService.ShowSleepPrompt();
        }
    }
}
