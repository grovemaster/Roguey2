using JRogue.Actors;
using JRogue.Quest;
using JRogue.World.Generation;

namespace JRogue.Racial
{
    public static class DwarfClanQuestService
    {
        public static bool TryAcceptDevotionQuest(
            BaseActor speaker,
            DwarfClanDefinition clan,
            out QuestDefinition acceptedQuest,
            out string failureReason)
        {
            acceptedQuest = null;
            failureReason = null;

            if (!DwarfClanQuestLogic.IsMemberOfClan(speaker, clan, out failureReason))
                return false;

            if (!SafeZonePolicyService.TryAllowDwarfClanCeremony(out failureReason))
                return false;

            QuestService quests = QuestService.Instance;
            if (quests == null)
            {
                failureReason = "Quest service unavailable.";
                return false;
            }

            string questId = DwarfClanQuestLogic.ResolveDevotionQuestId(clan);
            if (string.IsNullOrWhiteSpace(questId))
            {
                failureReason = "This clan has no devotion errand.";
                return false;
            }

            if (DwarfClanQuestLogic.IsDevotionQuestCompleted(quests, questId))
            {
                failureReason = "You have already completed this clan's devotion errand.";
                return false;
            }

            if (DwarfClanQuestLogic.TryGetActiveDevotionQuest(quests, questId, out _))
            {
                failureReason = "Your clan devotion errand is already underway.";
                return false;
            }

            QuestDefinition definition = quests.GetDefinition(questId);
            if (definition == null)
            {
                failureReason = "Clan devotion quest is unavailable.";
                return false;
            }

            if (!quests.TryAcceptForMember(definition.ResolvedQuestId, speaker, out failureReason))
                return false;

            acceptedQuest = definition;
            return true;
        }

        public static bool TryTurnInDevotionQuest(
            BaseActor speaker,
            DwarfClanDefinition clan,
            string npcId,
            out QuestDefinition completedQuest,
            out string failureReason)
        {
            completedQuest = null;
            failureReason = null;

            if (!DwarfClanQuestLogic.IsMemberOfClan(speaker, clan, out failureReason))
                return false;

            if (!SafeZonePolicyService.TryAllowDwarfClanCeremony(out failureReason))
                return false;

            QuestService quests = QuestService.Instance;
            if (quests == null)
            {
                failureReason = "Quest service unavailable.";
                return false;
            }

            string questId = DwarfClanQuestLogic.ResolveDevotionQuestId(clan);
            QuestDefinition definition = quests.GetDefinition(questId);
            if (definition == null)
            {
                failureReason = "Clan devotion quest is unavailable.";
                return false;
            }

            if (!DwarfClanQuestLogic.IsReadyToTurnIn(quests, questId))
            {
                failureReason = "You have no clan devotion errand ready to report.";
                return false;
            }

            if (!quests.TryTurnInForMember(definition.ResolvedQuestId, speaker, npcId, out failureReason))
                return false;

            completedQuest = definition;
            return true;
        }
    }
}
