using System.Collections.Generic;
using JRogue.Dialog;
using JRogue.UI.Gameplay;

namespace JRogue.World.Town
{
    /// <summary>Yes/no sleep prompt when bumping an inn bed.</summary>
    public static class InnBedSleepPromptService
    {
        public static void ShowSleepPrompt()
        {
            if (!InnLodgingService.HasBedAccess())
            {
                ShowPaymentRequiredDialog();
                return;
            }

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
                option =>
                {
                    NpcDialogBoxUI.EnsureInstance().Close();
                    if (option != null && option.label == "Yes")
                        InnRestService.SleepAtInn();
                });
        }

        static void ShowPaymentRequiredDialog()
        {
            var step = new DialogLineStep
            {
                SpeakerName = string.Empty,
                ResolvedText = "The beds cannot be used without paying the innkeeper first.",
                Portrait = null,
            };

            NpcDialogBoxUI.EnsureInstance().ShowLine(
                step,
                () => NpcDialogBoxUI.EnsureInstance().Close());
        }
    }
}
