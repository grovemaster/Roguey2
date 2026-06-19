using JRogue.Dialog;
using JRogue.UI.Gameplay;
using JRogue.World.Generation;

namespace JRogue.World.Town
{
    public static class DungeonPortalReminderService
    {
        public static void TryShowPortalDayReminder()
        {
            GameCalendarService calendar = GameCalendarService.Instance;
            if (calendar == null || !calendar.IsEnabled || !calendar.IsDungeonPortalOpen())
                return;

            if (!calendar.TryMarkPortalReminderShown())
                return;

            var step = new DialogLineStep
            {
                SpeakerName = string.Empty,
                ResolvedText =
                    "The dungeon portal is open today in Dimension Square.\n\n" +
                    "<size=14>Enter when you are ready.</size>",
                Portrait = null,
            };

            NpcDialogBoxUI.EnsureInstance().ShowLine(
                step,
                () => NpcDialogBoxUI.EnsureInstance().Close());
        }
    }
}
