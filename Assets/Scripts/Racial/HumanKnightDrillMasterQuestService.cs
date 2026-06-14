using JRogue.Actors;
using JRogue.Quest;
using JRogue.World.Generation;

namespace JRogue.Racial
{
    public static class HumanKnightDrillMasterQuestService
    {
        public static bool TryAcceptApprenticeship(
            BaseActor speaker,
            out QuestDefinition acceptedQuest,
            out string failureReason)
        {
            acceptedQuest = null;
            failureReason = null;

            if (!HumanKnightDrillMasterQuestLogic.IsSpeakerHuman(speaker, out _, out failureReason))
                return false;

            if (!HumanKnightClassCommitService.CanBeginKnightTraining(speaker, out failureReason))
                return false;

            if (!SafeZonePolicyService.TryAllowHumanKnightDrillQuestChange(out failureReason))
                return false;

            QuestService quests = QuestService.Instance;
            if (quests == null)
            {
                failureReason = "Quest service unavailable.";
                return false;
            }

            if (HumanKnightDrillMasterQuestLogic.IsApprenticeshipCompleted(speaker, quests))
            {
                failureReason = "You already walk the Knight's path.";
                return false;
            }

            if (HumanKnightDrillMasterQuestLogic.TryGetActiveApprenticeship(speaker, quests, out _, out _))
            {
                failureReason = "Your drill apprenticeship is already underway.";
                return false;
            }

            QuestDefinition definition = quests.GetDefinition(HumanKnightDrillMasterIds.ApprenticeshipQuestId);
            if (definition == null)
            {
                failureReason = "Drill apprenticeship quest is unavailable.";
                return false;
            }

            if (!quests.TryAcceptForMember(definition.ResolvedQuestId, speaker, out failureReason))
                return false;

            acceptedQuest = definition;
            return true;
        }

        public static bool TryCompleteApprenticeship(
            BaseActor speaker,
            string npcId,
            out QuestDefinition completedQuest,
            out string failureReason)
        {
            completedQuest = null;
            failureReason = null;

            if (!HumanKnightDrillMasterQuestLogic.IsSpeakerHuman(speaker, out _, out failureReason))
                return false;

            if (!HumanKnightClassCommitService.CanBeginKnightTraining(speaker, out failureReason))
                return false;

            if (!SafeZonePolicyService.TryAllowHumanKnightDrillQuestChange(out failureReason))
                return false;

            QuestService quests = QuestService.Instance;
            if (quests == null)
            {
                failureReason = "Quest service unavailable.";
                return false;
            }

            QuestDefinition definition = quests.GetDefinition(HumanKnightDrillMasterIds.ApprenticeshipQuestId);
            if (definition == null)
            {
                failureReason = "Drill apprenticeship quest is unavailable.";
                return false;
            }

            if (!HumanKnightDrillMasterQuestLogic.TryGetActiveApprenticeship(speaker, quests, out _, out _))
            {
                failureReason = "You have not begun drill apprenticeship.";
                return false;
            }

            if (!quests.TryTurnInForMember(definition.ResolvedQuestId, speaker, npcId, out failureReason))
                return false;

            completedQuest = definition;
            return true;
        }
    }
}
