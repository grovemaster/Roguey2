using JRogue.Actors;
using JRogue.Quest;
using JRogue.World.Generation;

namespace JRogue.Racial
{
    public static class DragonianElderQuestService
    {
        public static bool TryAcceptNextQuest(
            DragonianElderDefinition elder,
            BaseActor speaker,
            out QuestDefinition acceptedQuest,
            out string failureReason)
        {
            acceptedQuest = null;
            failureReason = null;

            if (!DragonianElderQuestLogic.IsSpeakerEligible(speaker, out _, out failureReason))
                return false;

            if (!SafeZonePolicyService.TryAllowDragonianElderQuestChange(out failureReason))
                return false;

            QuestService quests = QuestService.Instance;
            if (quests == null)
            {
                failureReason = "Quest service unavailable.";
                return false;
            }

            if (!DragonianElderQuestLogic.TryResolveNextOffer(elder, speaker, quests, out QuestDefinition next, out failureReason))
                return false;

            if (!quests.TryAcceptForMember(next.ResolvedQuestId, speaker, out failureReason))
                return false;

            acceptedQuest = next;
            return true;
        }

        public static bool TryTurnInReadyQuest(
            DragonianElderDefinition elder,
            BaseActor speaker,
            string npcId,
            out QuestDefinition completedQuest,
            out string failureReason)
        {
            completedQuest = null;
            failureReason = null;

            if (!DragonianElderQuestLogic.IsSpeakerEligible(speaker, out _, out failureReason))
                return false;

            if (!SafeZonePolicyService.TryAllowDragonianElderQuestChange(out failureReason))
                return false;

            QuestService quests = QuestService.Instance;
            if (quests == null)
            {
                failureReason = "Quest service unavailable.";
                return false;
            }

            if (!DragonianElderQuestLogic.TryGetReadyTurnInQuest(elder, speaker, quests, out QuestDefinition quest, out _))
            {
                failureReason = "No lesson is ready to seal.";
                return false;
            }

            if (!quests.TryTurnInForMember(quest.ResolvedQuestId, speaker, npcId, out failureReason))
                return false;

            completedQuest = quest;
            return true;
        }
    }
}
