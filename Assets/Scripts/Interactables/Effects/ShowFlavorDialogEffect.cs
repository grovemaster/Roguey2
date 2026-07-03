using JRogue.Actors;
using JRogue.Dialog;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "ShowFlavorDialog",
        menuName = "JRogue/Interactables/Effects/Show Flavor Dialog")]
    public sealed class ShowFlavorDialogEffect : InteractableEffect
    {
        [TextArea(2, 5)] public string dialogLine;

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            if (string.IsNullOrEmpty(dialogLine))
                return;

            var step = new DialogLineStep
            {
                SpeakerName = string.Empty,
                ResolvedText = dialogLine,
                Portrait = null,
            };

            NpcDialogBoxUI.EnsureInstance().ShowLine(
                step,
                () => NpcDialogBoxUI.EnsureInstance().Close());
        }
    }
}
