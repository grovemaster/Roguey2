using JRogue.Actors;
using JRogue.Quest;
using JRogue.World.Generation;

namespace JRogue.Racial
{
    public static class HumanMageTutorQuestService
    {
        public static bool TryAcceptApprenticeship(
            BaseActor speaker,
            out QuestDefinition acceptedQuest,
            out string failureReason)
        {
            acceptedQuest = null;
            failureReason = null;

            if (!HumanMageTutorQuestLogic.IsSpeakerHuman(speaker, out _, out failureReason))
                return false;

            if (!HumanMageClassCommitService.CanBeginMageTraining(speaker, out failureReason))
                return false;

            if (!SafeZonePolicyService.TryAllowHumanMageTutorQuestChange(out failureReason))
                return false;

            QuestService quests = QuestService.Instance;
            if (quests == null)
            {
                failureReason = "Quest service unavailable.";
                return false;
            }

            if (HumanMageTutorQuestLogic.IsApprenticeshipCompleted(speaker, quests))
            {
                failureReason = "You already walk the arcane path.";
                return false;
            }

            if (HumanMageTutorQuestLogic.TryGetActiveApprenticeship(speaker, quests, out _, out _))
            {
                failureReason = "Your apprenticeship is already underway.";
                return false;
            }

            QuestDefinition definition = quests.GetDefinition(HumanMageTutorIds.ApprenticeshipQuestId);
            if (definition == null)
            {
                failureReason = "Apprenticeship quest is unavailable.";
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

            if (!HumanMageTutorQuestLogic.IsSpeakerHuman(speaker, out _, out failureReason))
                return false;

            if (!HumanMageClassCommitService.CanBeginMageTraining(speaker, out failureReason))
                return false;

            if (!SafeZonePolicyService.TryAllowHumanMageTutorQuestChange(out failureReason))
                return false;

            QuestService quests = QuestService.Instance;
            if (quests == null)
            {
                failureReason = "Quest service unavailable.";
                return false;
            }

            QuestDefinition definition = quests.GetDefinition(HumanMageTutorIds.ApprenticeshipQuestId);
            if (definition == null)
            {
                failureReason = "Apprenticeship quest is unavailable.";
                return false;
            }

            if (!HumanMageTutorQuestLogic.TryGetActiveApprenticeship(speaker, quests, out _, out _))
            {
                failureReason = "You have not begun arcane apprenticeship.";
                return false;
            }

            if (!quests.TryTurnInForMember(definition.ResolvedQuestId, speaker, npcId, out failureReason))
                return false;

            completedQuest = definition;
            return true;
        }
    }
}
