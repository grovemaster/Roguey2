using JRogue.Actors;
using JRogue.Quest;
using JRogue.World.Generation;

namespace JRogue.Racial
{
    public static class HumanPriestShrineQuestService
    {
        public static bool TryAcceptInitiation(
            BaseActor speaker,
            out QuestDefinition acceptedQuest,
            out string failureReason)
        {
            acceptedQuest = null;
            failureReason = null;

            if (!HumanPriestShrineQuestLogic.IsSpeakerHuman(speaker, out _, out failureReason))
                return false;

            if (!HumanPriestClassCommitService.CanBeginPriestInitiation(speaker, out failureReason))
                return false;

            if (!SafeZonePolicyService.TryAllowHumanPriestShrineQuestChange(out failureReason))
                return false;

            QuestService quests = QuestService.Instance;
            if (quests == null)
            {
                failureReason = "Quest service unavailable.";
                return false;
            }

            QuestDefinition definition =
                quests.GetDefinition(HumanPriestShrineIds.InitiationQuestId);
            if (definition == null)
            {
                failureReason = "Shrine initiation quest is unavailable.";
                return false;
            }

            if (!quests.TryAcceptForMember(definition.ResolvedQuestId, speaker, out failureReason))
                return false;

            acceptedQuest = definition;
            return true;
        }

        public static bool TryCompleteInitiation(
            BaseActor speaker,
            string npcId,
            out QuestDefinition completedQuest,
            out string failureReason)
        {
            completedQuest = null;
            failureReason = null;

            if (!HumanPriestShrineQuestLogic.IsSpeakerHuman(speaker, out _, out failureReason))
                return false;

            if (!HumanPriestClassCommitService.CanBeginPriestInitiation(speaker, out failureReason))
                return false;

            if (!SafeZonePolicyService.TryAllowHumanPriestShrineQuestChange(out failureReason))
                return false;

            QuestService quests = QuestService.Instance;
            if (quests == null)
            {
                failureReason = "Quest service unavailable.";
                return false;
            }

            QuestDefinition definition =
                quests.GetDefinition(HumanPriestShrineIds.InitiationQuestId);
            if (definition == null)
            {
                failureReason = "Shrine initiation quest is unavailable.";
                return false;
            }

            if (!quests.TryTurnInForMember(definition.ResolvedQuestId, speaker, npcId, out failureReason))
                return false;

            completedQuest = definition;
            return true;
        }
    }
}
