using JRogue.Actors;
using JRogue.Quest;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    public static class DwarfClanQuestLogic
    {
        public static string ResolveDevotionQuestId(DwarfClanDefinition clan)
        {
            if (clan == null || string.IsNullOrWhiteSpace(clan.clanId))
                return string.Empty;

            if (string.Equals(clan.clanId, DwarfClanIds.ForgeBrothersClanId, System.StringComparison.Ordinal))
                return DwarfClanIds.ForgeBrothersDevotionQuestId;

            if (string.Equals(clan.clanId, DwarfClanIds.StoneWardensClanId, System.StringComparison.Ordinal))
                return DwarfClanIds.StoneWardensDevotionQuestId;

            return string.Empty;
        }

        public static bool IsMemberOfClan(BaseActor speaker, DwarfClanDefinition clan, out string failureReason)
        {
            failureReason = null;
            if (!DwarfClanJoinLogic.IsSpeakerDwarf(speaker, out _, out failureReason))
                return false;

            DwarfClanMembershipRuntime membership = speaker.GetComponent<DwarfClanMembershipRuntime>();
            if (membership == null || !membership.IsAffiliated)
            {
                failureReason = DwarfAncestorLearnLogic.NotMemberMessage;
                return false;
            }

            if (!membership.MatchesClan(clan))
            {
                failureReason = DwarfClanJoinLogic.WrongClanMessage;
                return false;
            }

            return true;
        }

        public static bool IsDevotionQuestCompleted(QuestService quests, string questId)
        {
            if (quests == null || string.IsNullOrWhiteSpace(questId))
                return false;

            return quests.TryGetInstance(questId, null, out QuestInstance instance)
                   && instance.state == QuestRuntimeState.Completed;
        }

        public static bool TryGetActiveDevotionQuest(
            QuestService quests,
            string questId,
            out QuestInstance instance)
        {
            instance = null;
            if (quests == null || string.IsNullOrWhiteSpace(questId))
                return false;

            if (!quests.TryGetInstance(questId, null, out instance))
                return false;

            return instance.state == QuestRuntimeState.Active
                   || instance.state == QuestRuntimeState.ReadyToTurnIn;
        }

        public static bool IsReadyToTurnIn(QuestService quests, string questId)
        {
            if (quests == null || string.IsNullOrWhiteSpace(questId))
                return false;

            return quests.TryGetInstance(questId, null, out QuestInstance instance)
                   && instance.state == QuestRuntimeState.ReadyToTurnIn;
        }
    }
}
