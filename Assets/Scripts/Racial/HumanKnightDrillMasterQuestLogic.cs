using System.Text;
using JRogue.Actors;
using JRogue.Quest;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    public static class HumanKnightDrillMasterQuestLogic
    {
        public const string LogPrefix = "[KnightDrill]";

        public static bool IsSpeakerHuman(BaseActor speaker, out CharacterStats stats, out string rejectLine)
        {
            stats = null;
            rejectLine = null;

            if (speaker == null)
            {
                rejectLine = "No speaker.";
                return false;
            }

            stats = speaker.GetComponent<CharacterStats>();
            if (stats == null || stats.race != Race.Human)
            {
                rejectLine = HumanKnightClassCommitService.RaceDenyMessage;
                return false;
            }

            return true;
        }

        public static bool IsAlreadyKnight(CharacterStats stats) =>
            stats != null && stats.humanClass == HumanClass.Knight;

        public static bool HasCommittedElsewhere(CharacterStats stats) =>
            stats != null
            && stats.humanClass != HumanClass.None
            && stats.humanClass != HumanClass.Knight;

        public static bool TryGetActiveApprenticeship(
            BaseActor speaker,
            QuestService quests,
            out QuestInstance instance,
            out QuestDefinition definition)
        {
            instance = null;
            definition = null;
            if (speaker == null || quests == null)
                return false;

            definition = quests.GetDefinition(HumanKnightDrillMasterIds.ApprenticeshipQuestId);
            if (definition == null)
                return false;

            string memberId = QuestLogic.ResolveMemberId(speaker);
            string storageKey = QuestInstanceKey.StorageKey(definition.ResolvedQuestId, memberId);
            if (!quests.ActiveInstances.TryGetValue(storageKey, out instance))
                return false;

            return instance.state != QuestRuntimeState.Completed
                && instance.state != QuestRuntimeState.Failed;
        }

        public static bool IsApprenticeshipCompleted(BaseActor speaker, QuestService quests)
        {
            if (speaker == null || quests == null)
                return false;

            QuestDefinition definition = quests.GetDefinition(HumanKnightDrillMasterIds.ApprenticeshipQuestId);
            if (definition == null)
                return false;

            string memberId = QuestLogic.ResolveMemberId(speaker);
            return QuestLogic.IsQuestCompletedForMember(quests.ActiveInstances, definition.ResolvedQuestId, memberId);
        }

        public static string BuildOfferBodyText()
        {
            var sb = new StringBuilder();
            sb.Append("The drill yard teaches form — the battlefield teaches mastery. ");
            sb.Append("Pay ");
            sb.Append(HumanKnightClassCommitService.DrillGoldCost);
            sb.Append(" gold when you are ready to commit to the Knight path.");
            return sb.ToString();
        }

        public static string BuildTurnInBodyText(BaseActor speaker)
        {
            string name = speaker != null ? speaker.DisplayName : "You";
            return
                $"{name}, swear the oath of the shield-wall and accept drill instruction for "
                + $"{HumanKnightClassCommitService.DrillGoldCost} gold?";
        }

        public static string BuildCompletionLine(BaseActor speaker)
        {
            string name = speaker != null ? speaker.DisplayName : "You";
            return
                $"{name} swears the oath of the shield-wall. Soul Power and essences remain your tools — "
                + "seek the drill master to spend skill points, and refine your techniques in combat.";
        }
    }
}
