using System.Collections.Generic;
using JRogue.Dialog;
using JRogue.UI.Gameplay;

namespace JRogue.World.Town
{
    /// <summary>Yes/no sleep prompt when bumping an inn bed (yes action deferred).</summary>
    public static class InnBedSleepPromptService
    {
        public static void ShowSleepPrompt()
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = string.Empty,
                PromptText = "Do you want to sleep?",
                Portrait = null,
                Options = new List<DialogChoiceOptionData>
                {
                    new DialogChoiceOptionData { label = "Yes", responseNodeIndex = DialogGraph.NoNode },
                    new DialogChoiceOptionData { label = "No", responseNodeIndex = DialogGraph.NoNode },
                },
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(
                step,
                _ => NpcDialogBoxUI.EnsureInstance().Close());
        }
    }
}
